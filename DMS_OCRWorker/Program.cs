using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.IO;
using System.Threading.Tasks;

namespace DMS_OCRWorker
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Boolean test = true;
            // Prüfe, ob ein Testmodus aktiviert ist
            if (test)
            {
                RunOcrTest();
            }
            else
            {
                await RunRabbitMqWorker();
            }
        }

        // OCR-Testfunktion
        static void RunOcrTest()
        {
            Console.WriteLine("Testing OCR functionality...");

            // Relativer Pfad zur Test-PDF-Datei
            string testPdfPath = "test.pdf"; // Stelle sicher, dass test.pdf im Arbeitsverzeichnis liegt

            try
            {
                // OCR-Verarbeitung ausführen
                var result = OcrProcessor.PerformOcr(testPdfPath);

                // Ergebnis anzeigen
                Console.WriteLine("OCR Result:");
                Console.WriteLine(result);
            }
            catch (Exception ex)
            {
                // Fehler behandeln und ausgeben
                Console.WriteLine($"Error during OCR processing: {ex.Message}");
            }

            Console.WriteLine("OCR test completed.");
        }

        // RabbitMQ-Worker starten
        static async Task RunRabbitMqWorker()
        {
            Console.WriteLine("OCR Worker started...");
            var factory = new ConnectionFactory() { HostName = "rabbitmq" };
            using var connection = await factory.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            // Declare the queues
            await channel.QueueDeclareAsync(queue: "OCR_QUEUE", durable: false, exclusive: false, autoDelete: false, arguments: null);
            await channel.QueueDeclareAsync(queue: "RESULT_QUEUE", durable: false, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.ReceivedAsync += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var filePath = Encoding.UTF8.GetString(body);
                Console.WriteLine($"[x] Received file path: {filePath}");

                string ocrResult = string.Empty;
                try
                {
                    ocrResult = OcrProcessor.PerformOcr(filePath);
                    Console.WriteLine($"[x] OCR processing completed for file: {filePath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[!] Error during OCR processing: {ex.Message}");
                }

                await SendToResultQueue(channel, ocrResult);
            };

            await channel.BasicConsumeAsync(queue: "OCR_QUEUE", autoAck: true, consumer: consumer);

            Console.WriteLine("Waiting for messages. Press [enter] to exit.");
            Console.ReadLine();
        }

        private static async Task SendToResultQueue(RabbitMQ.Client.IChannel channel, string result)
        {
            var body = Encoding.UTF8.GetBytes(result);
            var basicProperties = new RabbitMQ.Client.BasicProperties();
            await channel.BasicPublishAsync<RabbitMQ.Client.BasicProperties>(exchange: "", routingKey: "RESULT_QUEUE", mandatory: false, basicProperties: basicProperties, body: body);
            Console.WriteLine($"[x] Sent OCR result to RESULT_QUEUE");
        }
    }

    public static class OcrProcessor
    {
        public static string PerformOcr(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"File not found: {filePath}");
            }

            var ocrResult = new StringBuilder();

            using (var engine = new Tesseract.TesseractEngine(@"./tessdata", "eng", Tesseract.EngineMode.Default))
            {
                using (var img = Tesseract.Pix.LoadFromFile(filePath))
                {
                    using (var page = engine.Process(img))
                    {
                        ocrResult.Append(page.GetText());
                    }
                }
            }

            return ocrResult.ToString();
        }
    }
}
