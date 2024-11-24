using System;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.IO;
using Tesseract;
using ImageMagick;

namespace DMS_OCR
{
    class Program
    {
        private static EventingBasicConsumer consumer;

        static void Main(string[] args)
        {
            try
            {
                // Lesen der Konfigurationswerte aus Umgebungsvariablen oder Verwendung von Standardwerten
                var hostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST") ?? "rabbitmq";
                var userName = Environment.GetEnvironmentVariable("RABBITMQ_USER") ?? "guest";
                var password = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest";
                var portStr = Environment.GetEnvironmentVariable("RABBITMQ_PORT") ?? "5672";
                int port = int.TryParse(portStr, out var parsedPort) ? parsedPort : 5672;

                var factory = new ConnectionFactory()
                {
                    HostName = hostName,
                    Port = port,
                    UserName = userName,
                    Password = password
                };

                Console.WriteLine($"Verbindung zu RabbitMQ: HostName={hostName}, Port={port}, UserName={userName}");

                // Anzahl der Wiederholungsversuche und Verzögerung zwischen den Versuchen
                int retryCount = 5;
                int delaySeconds = 5;
                IConnection connection = null;

                // Wiederholungslogik für die Verbindung zu RabbitMQ
                for (int i = 0; i < retryCount; i++)
                {
                    try
                    {
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
                        Thread.Sleep(delaySeconds * 5000);
                    }
                }

                IModel channel = null;
                try
                {
                    channel = connection.CreateModel();

                    // Deklarieren der Warteschlangen
                    channel.QueueDeclare(queue: "ocr_requests", durable: true, exclusive: false, autoDelete: false, arguments: null);
                    channel.QueueDeclare(queue: "ocr_results", durable: true, exclusive: false, autoDelete: false, arguments: null);

                    consumer = new EventingBasicConsumer(channel);
                    consumer.Received += (model, ea) =>
                    {
                        try
                        {
                            var body = ea.Body.ToArray(); // Ensure body is byte[]
                            var message = Encoding.UTF8.GetString(body); // No ambiguity

                            Console.WriteLine($"[x] Empfangene Nachricht: {message}");

                            // Nachricht sollte den Pfad oder die ID des Dokuments enthalten
                            string documentPath = GetDocumentPath(message); // Implementieren Sie diese Methode entsprechend

                            string ocrResult = OcrProcessor.PerformOcr(documentPath);

                            // Senden des OCR-Ergebnisses zurück an den REST-Server
                            var resultMessage = CreateResultMessage(message, ocrResult); // Implementieren Sie diese Methode entsprechend
                            var resultBody = Encoding.UTF8.GetBytes(resultMessage);

                            channel.BasicPublish(exchange: "", routingKey: "ocr_results", basicProperties: null, body: resultBody);
                            Console.WriteLine($"[x] OCR-Ergebnis gesendet: {resultMessage}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[!] Fehler bei der Verarbeitung der Nachricht: {ex.Message}");
                            Console.WriteLine(ex.StackTrace);
                            // Optional: Fehlerbehandlung implementieren, z.B. Nachricht in Fehlerwarteschlange verschieben
                        }
                    };

                    // Starten des Konsumenten
                    channel.BasicConsume(queue: "ocr_requests", autoAck: true, consumer: consumer);

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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Unbehandelte Ausnahme: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                // Optional: Warten, damit die Logs gelesen werden können
                Thread.Sleep(Timeout.Infinite);
            }
        }

        // Hilfsmethoden zur Verarbeitung der Nachrichten (Implementierung erforderlich)
        static string GetDocumentPath(string message)
        {
            // Hier extrahieren Sie den Dokumentpfad oder die Dokument-ID aus der Nachricht
            // Zum Beispiel könnten Sie die Nachricht als JSON parsen und den Pfad extrahieren
            // Hier ein einfaches Beispiel:
            return message; // Passen Sie dies an Ihre Bedürfnisse an
        }

        static string CreateResultMessage(string originalMessage, string ocrResult)
        {
            // Hier erstellen Sie die Nachricht mit dem OCR-Ergebnis
            // Zum Beispiel könnten Sie ein JSON-Objekt erstellen, das die ursprüngliche Nachricht und das Ergebnis enthält
            // Hier ein einfaches Beispiel:
            return ocrResult; // Passen Sie dies an Ihre Bedürfnisse an
        }
    }
}
