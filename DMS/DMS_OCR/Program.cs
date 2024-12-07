// DMS_OCR/Program.cs
using System;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using Minio;
using Minio.DataModel.Args;
using System.IO;
using System.Threading.Tasks;

namespace DMS_OCR
{
    class Program
    {
        private static EventingBasicConsumer consumer;
        private static IMinioClient minioClient;
        private static string minioEndpoint = "minio"; // MinIO-Server-Hostname
        private static int minioPort = 9000; // MinIO-Port
        private static string minioAccessKey = "your-access-key"; // MinIO-Zugangsschlüssel
        private static string minioSecretKey = "your-secret-key"; // MinIO-Geheimschlüssel
        private static bool minioUseSSL = false; // SSL verwenden oder nicht
        private static string minioBucketName = "uploads"; // MinIO-Bucket-Name

        static void Main(string[] args)
        {
            try
            {
                // RabbitMQ-Konfiguration (hardcodiert)
                var rabbitHostName = "rabbitmq"; // RabbitMQ-Server-Hostname
                var rabbitUserName = "guest"; // RabbitMQ-Benutzername
                var rabbitPassword = "guest"; // RabbitMQ-Passwort
                var rabbitPort = 5672; // RabbitMQ-Port

                // Initialisieren des MinIO-Clients
                minioClient = new MinioClient()
                    .WithEndpoint(minioEndpoint, minioPort)
                    .WithCredentials(minioAccessKey, minioSecretKey)
                    .WithSSL(minioUseSSL)
                    .Build();

                Console.WriteLine($"Verbindung zu RabbitMQ: HostName={rabbitHostName}, Port={rabbitPort}, UserName={rabbitUserName}");

                // Anzahl der Wiederholungsversuche und Verzögerung zwischen den Versuchen
                int retryCount = 5;
                int delaySeconds = 5;
                IConnection connection = null;

                // Wiederholungslogik für die Verbindung zu RabbitMQ
                for (int i = 0; i < retryCount; i++)
                {
                    try
                    {
                        var factory = new ConnectionFactory()
                        {
                            HostName = rabbitHostName,
                            Port = rabbitPort,
                            UserName = rabbitUserName,
                            Password = rabbitPassword
                        };
                        connection = factory.CreateConnection();
                        Console.WriteLine("Verbindung zu RabbitMQ erfolgreich hergestellt.");
                        break; // Erfolgreich verbunden, Schleife verlassen
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Verbindungsversuch {i + 1} fehlgeschlagen: {ex.Message}");
                        if (i == retryCount - 1)
                        {
                            Console.WriteLine("Maximale Anzahl von Verbindungsversuchen erreicht. Beende Anwendung.");
                            return; // Anwendung beenden oder Exception werfen
                        }
                        Console.WriteLine($"Warte {delaySeconds} Sekunden vor dem nächsten Versuch...");
                        Thread.Sleep(delaySeconds * 1000);
                    }
                }

                if (connection == null)
                {
                    Console.WriteLine("Keine Verbindung zu RabbitMQ hergestellt. Beende Anwendung.");
                    return;
                }

                IModel channel = null;
                try
                {
                    channel = connection.CreateModel();

                    // Deklarieren der Exchange
                    channel.ExchangeDeclare(exchange: "document_events", type: ExchangeType.Direct, durable: true, autoDelete: false);

                    // Deklarieren der Warteschlangen
                    channel.QueueDeclare(queue: "document.created", durable: true, exclusive: false, autoDelete: false, arguments: null);

                    // Binden der Queue an die Exchange mit dem Routing Key "created"
                    channel.QueueBind(queue: "document.created", exchange: "document_events", routingKey: "created");

                    consumer = new EventingBasicConsumer(channel);
                    consumer.Received += async (model, ea) =>
                    {
                        try
                        {
                            var body = ea.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);

                            Console.WriteLine($"[x] Empfangene Nachricht: {message}");

                            // Parsen der Nachricht (angenommen JSON)
                            var ocrMessage = JsonConvert.DeserializeObject<OcrMessage>(message);
                            if (ocrMessage == null || string.IsNullOrEmpty(ocrMessage.FileName))
                            {
                                Console.WriteLine("[!] Ungültige Nachricht: Datei-Name fehlt.");
                                return;
                            }

                            string fileName = ocrMessage.FileName;
                            string title = ocrMessage.Title;
                            Console.WriteLine($"[>] Verarbeite Datei: {fileName} mit Titel: {title}");

                            // Herunterladen der Datei von MinIO
                            string tempFilePath = Path.Combine(Path.GetTempPath(), fileName);
                            await DownloadFileFromMinioAsync(fileName, tempFilePath);

                            // OCR-Verarbeitung durchführen
                            string ocrText = OcrProcessor.PerformOcr(tempFilePath);
                            Console.WriteLine($"[+] OCR-Ergebnis für {fileName}:\n{ocrText}");

                            // Optional: Speichern des OCR-Ergebnisses, Senden an eine andere Queue etc.
                            // Beispiel: Speichern in eine Textdatei neben der Originaldatei
                            string ocrFilePath = Path.ChangeExtension(tempFilePath, ".txt");
                            File.WriteAllText(ocrFilePath, ocrText);
                            Console.WriteLine($"[+] OCR-Ergebnis gespeichert unter: {ocrFilePath}");

                            // Bereinigung der temporären Datei
                            File.Delete(tempFilePath);
                            Console.WriteLine($"[+] Temporäre Datei gelöscht: {tempFilePath}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[!] Fehler bei der Verarbeitung der Nachricht: {ex.Message}");
                            Console.WriteLine(ex.StackTrace);
                            // Optional: Nachricht in eine Fehlerwarteschlange verschieben oder erneut versuchen
                        }
                    };

                    // Starten des Konsumenten
                    channel.BasicConsume(queue: "document.created", autoAck: true, consumer: consumer);

                    Console.WriteLine(" [*] Wartet auf Nachrichten.");

                    // Anwendung am Laufen halten
                    Thread.Sleep(Timeout.Infinite);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[!] Unbehandelte Ausnahme im Channel-Bereich: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
                finally
                {
                    // Ressourcen freigeben
                    channel?.Close();
                    connection?.Close();
                }
            }catch(Exception ex)
            {
                Console.WriteLine($"[!] Unbehandelte Ausnahme im Hauptbereich: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }

            /// <summary>
            /// Lädt eine Datei von MinIO herunter und speichert sie am angegebenen Pfad.
            /// </summary>
            /// <param name="fileName">Der Name der Datei in MinIO.</param>
            /// <param name="destinationPath">Der lokale Pfad, wo die Datei gespeichert werden soll.</param>
            /// <returns></returns>
             static async Task DownloadFileFromMinioAsync(string fileName, string destinationPath)
            {
                try
                {
                    var args = new GetObjectArgs()
                        .WithBucket(minioBucketName)
                        .WithObject(fileName)
                        .WithFile(destinationPath);

                    await minioClient.GetObjectAsync(args);
                    Console.WriteLine($"[>] Datei heruntergeladen: {destinationPath}");
                }
                catch (Minio.Exceptions.MinioException ex)
                {
                    Console.WriteLine($"[!] MinIO Fehler beim Herunterladen der Datei {fileName}: {ex.Message}");
                    throw; // Weiterwerfen, damit der Fehler im Consumer-Handler behandelt wird
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[!] Allgemeiner Fehler beim Herunterladen der Datei {fileName}: {ex.Message}");
                    throw;
                }
            }
        }
    }
}
