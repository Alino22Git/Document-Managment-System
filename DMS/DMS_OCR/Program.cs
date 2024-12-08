using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Newtonsoft.Json;
using Minio;
using Minio.DataModel.Args;
using Elastic.Clients.Elasticsearch;
using System.IO;

namespace DMS_OCR
{
    class Program
    {
        private static EventingBasicConsumer consumer;
        private static IMinioClient minioClient;
        private static ElasticsearchClient _elasticClient;

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
                // Elasticsearch-Client initialisieren
                var elasticSettings = new ElasticsearchClientSettings(new Uri("http://elasticsearch:9200"))
                    .DefaultIndex("documents");
                _elasticClient = new ElasticsearchClient(elasticSettings);
                Console.WriteLine("Elasticsearch-Client initialisiert.");

                Console.WriteLine("Elasticsearch-Client initialisiert.");

                // MinIO-Client initialisieren
                minioClient = new MinioClient()
                    .WithEndpoint(minioEndpoint, minioPort)
                    .WithCredentials(minioAccessKey, minioSecretKey)
                    .WithSSL(minioUseSSL)
                    .Build();

                Console.WriteLine($"MinIO-Client initialisiert: {minioEndpoint}:{minioPort}");

                // RabbitMQ-Verbindung herstellen
                var rabbitHostName = "rabbitmq";
                var rabbitUserName = "guest";
                var rabbitPassword = "guest";
                var rabbitPort = 5672;

                int retryCount = 100;
                int delaySeconds = 5;
                IConnection connection = null;

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
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Verbindungsversuch {i + 1} fehlgeschlagen: {ex.Message}");
                        if (i == retryCount - 1)
                        {
                            Console.WriteLine("Maximale Anzahl von Verbindungsversuchen erreicht. Beende Anwendung.");
                            return;
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

                    // RabbitMQ Queues und Exchanges deklarieren
                    channel.ExchangeDeclare(exchange: "document_events", type: ExchangeType.Direct, durable: true, autoDelete: false);
                    channel.QueueDeclare(queue: "document.created", durable: true, exclusive: false, autoDelete: false, arguments: null);
                    channel.ExchangeDeclare(exchange: "ocr_results", type: ExchangeType.Direct, durable: true, autoDelete: false);
                    channel.QueueDeclare(queue: "ocr_result_queue", durable: true, exclusive: false, autoDelete: false, arguments: null);

                    channel.QueueBind(queue: "document.created", exchange: "document_events", routingKey: "created");
                    channel.QueueBind(queue: "ocr_result_queue", exchange: "ocr_results", routingKey: "ocr_result");

                    consumer = new EventingBasicConsumer(channel);
                    consumer.Received += async (model, ea) =>
                    {
                        try
                        {
                            var body = ea.Body.ToArray();
                            var message = Encoding.UTF8.GetString(body);

                            Console.WriteLine($"[x] Empfangene Nachricht: {message}");

                            var ocrMessage = JsonConvert.DeserializeObject<OcrMessage>(message);
                            if (ocrMessage == null || string.IsNullOrEmpty(ocrMessage.FileName) || string.IsNullOrEmpty(ocrMessage.Id))
                            {
                                Console.WriteLine("[!] Ungültige Nachricht: Datei-Name oder ID fehlt.");
                                return;
                            }

                            string fileName = ocrMessage.FileName;
                            string title = ocrMessage.Title;
                            string id = ocrMessage.Id;
                            Console.WriteLine($"[>] Verarbeite Datei: {fileName} mit Titel: {title}");

                            string tempFilePath = Path.Combine(Path.GetTempPath(), fileName);
                            await DownloadFileFromMinioAsync(fileName, tempFilePath);

                            string ocrText = OcrProcessor.PerformOcr(tempFilePath);
                            Console.WriteLine($"[+] OCR-Ergebnis für {fileName}:\n{ocrText}");

                            // Dokument in Elasticsearch speichern
                            await IndexDocumentInElasticsearch(id, title, ocrText);

                            // Ergebnis an RabbitMQ senden
                            SendOcrResultToRabbitMq(id, ocrText, channel);

                            File.Delete(tempFilePath);
                            Console.WriteLine($"[+] Temporäre Datei gelöscht: {tempFilePath}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[!] Fehler bei der Verarbeitung der Nachricht: {ex.Message}");
                            Console.WriteLine(ex.StackTrace);
                        }
                    };

                    // Starten des Konsumenten
                    channel.BasicConsume(queue: "document.created", autoAck: true, consumer: consumer);

                    Console.WriteLine(" [*] Wartet auf Nachrichten.");

                    Thread.Sleep(Timeout.Infinite);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[!] Unbehandelte Ausnahme im Channel-Bereich: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                }
                finally
                {
                    channel?.Close();
                    connection?.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Unbehandelte Ausnahme im Hauptbereich: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

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
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Fehler beim Herunterladen der Datei {fileName}: {ex.Message}");
                throw;
            }
        }
        
        static async Task IndexDocumentInElasticsearch(string id, string title, string content)
        {
            try
            {
                var document = new
                {
                    Id = id,
                    Title = title,
                    Content = content
                };

                var response = await _elasticClient.IndexAsync(document, idx => idx.Index("documents"));
                if (response.IsValidResponse)
                {
                    Console.WriteLine($"[+] Dokument mit ID {id} erfolgreich in Elasticsearch gespeichert.");
                }
                else
                {
                    Console.WriteLine($"[!] Fehler beim Speichern des Dokuments in Elasticsearch: {response.DebugInformation}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Ausnahme beim Indexieren des Dokuments: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        static void SendOcrResultToRabbitMq(string id, string ocrText, IModel channel)
        {
            try
            {
                var resultMessage = new OcrResultMessage
                {
                    Id = id,
                    Content = ocrText
                };

                var messageBody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(resultMessage));

                channel.BasicPublish(exchange: "ocr_results",
                                     routingKey: "ocr_result",
                                     basicProperties: null,
                                     body: messageBody);

                Console.WriteLine($"[>] OCR-Ergebnis an RabbitMQ gesendet für ID: {id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[!] Fehler beim Senden des OCR-Ergebnisses an RabbitMQ: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        public class OcrMessage
        {
            public string Id { get; set; }
            public string FileName { get; set; }
            public string Title { get; set; }
        }

        public class OcrResultMessage
        {
            public string Id { get; set; }
            public string Content { get; set; }
        }
    }
}
