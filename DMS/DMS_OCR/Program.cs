using System;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.IO;
using Tesseract;
using ImageMagick;

namespace DMS_OCR
{
    class Program
    {
        static void Main(string[] args)
        {
            var factory = new ConnectionFactory() { HostName = "rabbitmq", UserName = "user", Password = "password" };
            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();

            // Deklarieren der Warteschlangen
            channel.QueueDeclare(queue: "ocr_requests", durable: true, exclusive: false, autoDelete: false, arguments: null);
            channel.QueueDeclare(queue: "ocr_results", durable: true, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) =>
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
            };

            channel.BasicConsume(queue: "ocr_requests", autoAck: true, consumer: consumer);

            Console.WriteLine(" [*] Wartet auf Nachrichten.");
            Console.ReadLine();
        }

        // Hilfsmethoden zur Verarbeitung der Nachrichten (Implementierung erforderlich)
        static string GetDocumentPath(string message)
        {
            // Extrahieren Sie den Dokumentpfad aus der Nachricht
            return message; // Beispielhaft
        }

        static string CreateResultMessage(string originalMessage, string ocrResult)
        {
            // Erstellen Sie eine Nachricht, die das OCR-Ergebnis enthält
            return ocrResult; // Beispielhaft
        }
    }
}
