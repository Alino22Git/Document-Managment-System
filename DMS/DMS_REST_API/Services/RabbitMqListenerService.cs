// DMS_REST_API/Services/RabbitMqListenerService.cs
using AutoMapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Newtonsoft.Json;
using DMS_DAL.Repositories;
using DMS_DAL.Entities;
using System.Threading;
using System.Threading.Tasks;

namespace DMS_REST_API.Services
{
    public class RabbitMqListenerService : IHostedService, IDisposable
    {
        private readonly ILogger<RabbitMqListenerService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMapper _mapper;
        private IConnection _connection;
        private IModel _channel;
        private readonly string _exchangeName = "ocr_results";
        private readonly string _queueName = "ocr_result_queue";
        private AsyncEventingBasicConsumer _consumer;

        // Konfigurierbare Retry-Parameter
        private readonly int _maxRetryAttempts = 50; // Erhöht auf 50 gemäß den Logs
        private readonly int _retryDelaySeconds = 3; // Wartezeit zwischen Versuchen

        public RabbitMqListenerService(ILogger<RabbitMqListenerService> logger, IServiceProvider serviceProvider, IMapper mapper)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _mapper = mapper;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("RabbitMQ Listener Service startet.");

            // Starten Sie die Verbindung in einem separaten Task, um den Host nicht zu blockieren
            Task.Run(() => InitializeRabbitMQWithRetry(cancellationToken), cancellationToken);

            return Task.CompletedTask;
        }

        private void InitializeRabbitMQ()
        {
            var factory = new ConnectionFactory()
            {
                HostName = "rabbitmq",
                Port = 5672,
                UserName = "guest",
                Password = "guest",
                DispatchConsumersAsync = true // Für asynchrone Consumer
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Exchange und Queue deklarieren
            _channel.ExchangeDeclare(exchange: _exchangeName, type: ExchangeType.Direct, durable: true, autoDelete: false);
            _channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);
            _channel.QueueBind(queue: _queueName, exchange: _exchangeName, routingKey: "ocr_result");

            _logger.LogInformation("RabbitMQ Listener Service verbunden und Exchange/Queue deklariert.");

            // Verwenden Sie AsyncEventingBasicConsumer
            _consumer = new AsyncEventingBasicConsumer(_channel);
            _consumer.Received += OnMessageReceivedAsync;

            _channel.BasicConsume(queue: _queueName, autoAck: true, consumer: _consumer);
        }

        private void InitializeRabbitMQWithRetry(CancellationToken cancellationToken)
        {
            for (int attempt = 1; attempt <= _maxRetryAttempts; attempt++)
            {
                try
                {
                    _logger.LogInformation("Versuch {Attempt} von {_maxRetryAttempts} zur Verbindung mit RabbitMQ...", attempt, _maxRetryAttempts);
                    InitializeRabbitMQ();
                    _logger.LogInformation("Erfolgreich mit RabbitMQ verbunden.");
                    return; // Erfolgreich verbunden, Schleife verlassen
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Verbindungsversuch {Attempt} zu RabbitMQ fehlgeschlagen.", attempt);

                    if (attempt == _maxRetryAttempts)
                    {
                        _logger.LogCritical("Maximale Anzahl von Verbindungsversuchen erreicht. Beende RabbitMqListenerService.");
                        return; // Optional: Anwendung beenden oder andere Maßnahmen ergreifen
                    }

                    _logger.LogInformation("Warte {_retryDelaySeconds} Sekunden vor dem nächsten Verbindungsversuch...", _retryDelaySeconds);
                    Thread.Sleep(TimeSpan.FromSeconds(_retryDelaySeconds));

                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogWarning("Verbindung zum RabbitMQ wurde durch Abbruchanforderung unterbrochen.");
                        return;
                    }
                }
            }
        }

        private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            _logger.LogInformation("OCR-Ergebnisnachricht empfangen: {Message}", message);

            try
            {
                var ocrResult = JsonConvert.DeserializeObject<OcrResultMessage>(message);
                if (ocrResult == null)
                {
                    _logger.LogWarning("Ungültige OCR-Ergebnisnachricht empfangen.");
                    return;
                }

                using (var scope = _serviceProvider.CreateScope())
                {
                    var documentRepository = scope.ServiceProvider.GetRequiredService<IDocumentRepository>();

                    // Dokument anhand der ID abrufen
                    var document = await documentRepository.GetDocumentAsync(ocrResult.Id);
                    if (document == null)
                    {
                        _logger.LogWarning("Dokument mit ID {Id} wurde nicht gefunden.", ocrResult.Id);
                        return;
                    }

                    // Content aktualisieren
                    document.Content = ocrResult.Content;
                    await documentRepository.UpdateDocumentAsync(document);

                    _logger.LogInformation("Dokument mit ID {Id} erfolgreich mit OCR-Inhalt aktualisiert.", ocrResult.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Verarbeiten der OCR-Ergebnisnachricht.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("RabbitMQ Listener Service stoppt.");

            _channel?.Close();
            _connection?.Close();

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }

        /// <summary>
        /// Klasse zur Darstellung der OCR-Ergebnisnachricht.
        /// </summary>
        private class OcrResultMessage
        {
            public int Id { get; set; }          // ID des Dokuments
            public string Content { get; set; }  // OCR-Ergebnistext
        }
    }
}
