using RabbitMQ.Client.Events;
using System;
using System.IO;
using System.Text;
using ImageMagick;
using Tesseract;
using System.Diagnostics;

namespace DMS_OCRWorker
{
    public class OcrWorker
    {
        using RabbitMQ.Client;
            private IConnection _connection;
            private IModel _channel;
            public OcrWorker()
            {
                ConnectToRabbitMQ();
            }

            private void ConnectToRabbitMQ()
            {
                int retries = 5;
                while (retries > 0)
                {
                    try
                    {
                        var factory = new ConnectionFactory() { HostName = "rabbitmq", UserName = "user", Password = "password" };
                        _connection = factory.CreateConnection();
                        _channel = _connection.CreateModel();
                        _channel.QueueDeclare(queue: "file_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
                        Console.WriteLine("Erfolgreich mit RabbitMQ verbunden und Queue erstellt.");
                        break; // Wenn die Verbindung klappt, verlässt es die Schleife
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Fehler beim Verbinden mit RabbitMQ: {ex.Message}. Versuche es in 5 Sekunden erneut...");
                        Thread.Sleep(5000);
                        retries--;
                    }
                }
                if (_connection == null || !_connection.IsOpen)
                {
                    throw new Exception("Konnte keine Verbindung zu RabbitMQ herstellen, alle Versuche fehlgeschlagen.");
                }
            }

            public void Start()
            {
                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var parts = message.Split('|');
                    if (parts.Length == 2)
                    {
                        var id = parts[0];
                        var filePath = parts[1];
                        Console.WriteLine($"[x] Received ID: {id}, FilePath: {filePath}");
                        // Stelle sicher, dass die Datei existiert
                        if (!File.Exists(filePath))
                        {
                            Console.WriteLine($"Fehler: Datei {filePath} nicht gefunden.");
                            return;
                        }
                        // OCR-Verarbeitung starten
                        var extractedText = PerformOcr(filePath);
                        if (!string.IsNullOrEmpty(extractedText))
                        {
                            // Ergebnis zurück an RabbitMQ senden
                            var resultBody = Encoding.UTF8.GetBytes($"{id}|{extractedText}");
                            _channel.BasicPublish(exchange: "", routingKey: "ocr_result_queue", basicProperties: null, body: resultBody);
                            Console.WriteLine($"[x] Sent result for ID: {id}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Fehler: Ungültige Nachricht empfangen, Split in weniger als 2 Teile.");
                    }
                };
                _channel.BasicConsume(queue: "file_queue", autoAck: true, consumer: consumer);
            }

            private string PerformOcr(string filePath)
            {
                var stringBuilder = new StringBuilder();
                try
                {
                    using (var images = new MagickImageCollection(filePath)) // MagickImageCollection für mehrere Seiten
                    {
                        foreach (var image in images)
                        {
                            var tempPngFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".png");
                            image.Density = new Density(300, 300); // Setze die Auflösung
                                                                   //image.ColorType = ColorType.Grayscale; //Unnötige Farben weg
                                                                   //image.Contrast(); // Erhöht den Kontrast
                                                                   //image.Sharpen(); // Schärft das Bild, um Unschärfen zu reduzieren
                                                                   //image.Despeckle(); // Entfernt Bildrauschen
                            image.Format = MagickFormat.Png;
                            //image.Resize(image.Width * 2, image.Height * 2); // Vergrößere das Bild um das Doppelte
                            // Prüfe, ob eine erhebliche Schräglage vorhanden ist
                            image.Write(tempPngFile);
                            // Verwende die Tesseract CLI für jede Seite
                            var psi = new ProcessStartInfo
                            {
                                FileName = "tesseract",
                                Arguments = $"{tempPngFile} stdout -l eng",
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };
                            using (var process = Process.Start(psi))
                            {
                                string result = process.StandardOutput.ReadToEnd();
                                stringBuilder.Append(result);
                            }
                            File.Delete(tempPngFile); // Lösche die temporäre PNG-Datei nach der Verarbeitung
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fehler bei der OCR-Verarbeitung: {ex.Message}");
                    if (ex.InnerException != null)
                    {
                        Console.WriteLine($"Innere Ausnahme: {ex.InnerException.Message}");
                        Console.WriteLine($"Stacktrace: {ex.StackTrace}");
                    }
                }
                return stringBuilder.ToString();
            }
            public void Dispose()
            {
                _channel?.Close();
                _connection?.Close();
            }
        }
    }
}

