using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DMS_REST_API.Services
{
    public class OcrWorker : BackgroundService
    {
        private readonly ILogger<OcrWorker> _logger;
        private IConnection _connection;
        private IModel _channel;

        public OcrWorker(ILogger<OcrWorker> logger)
        {
            _logger = logger;
            // Verbindung zu RabbitMQ wird in StartAsync hergestellt
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                var factory = new ConnectionFactory() { HostName = "rabbitmq" };
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.QueueDeclare(queue: "document_queue",
                    durable: false,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                _logger.LogInformation("OCR Worker gestartet und hört auf Nachrichten in der Queue: document_queue");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Herstellen der Verbindung zu RabbitMQ");
                // Entscheiden Sie, ob Sie die Ausnahme behandeln oder erneut auslösen möchten
            }

            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_channel == null)
            {
                _logger.LogWarning("RabbitMQ-Kanal nicht verfügbar. OCR Worker wird nicht ausgeführt.");
                return;
            }

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var message = System.Text.Encoding.UTF8.GetString(body);
                _logger.LogInformation("Empfangene Nachricht: {Message}", message);

                // TODO: Fügen Sie hier die OCR-Verarbeitung hinzu

                await Task.CompletedTask;
            };

            _channel.BasicConsume(queue: "document_queue",
                                 autoAck: true,
                                 consumer: consumer);

            // Halten Sie den Task am Leben, solange der Dienst aktiv ist
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public override void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
            base.Dispose();
        }
    }
}
