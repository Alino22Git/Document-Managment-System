using Microsoft.AspNetCore.Mvc;
using DMS_REST_API.DTO;
using AutoMapper;
using DMS_DAL.Entities;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace DMS_REST_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DocumentController : ControllerBase, IDisposable
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMapper _mapper;
        private readonly ILogger<DocumentController> _logger;
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public DocumentController(IHttpClientFactory httpClientFactory, IMapper mapper, ILogger<DocumentController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _mapper = mapper;
            _logger = logger;

            // Verbindung zu RabbitMQ herstellen
            var factory = new ConnectionFactory()
            {
                HostName = "rabbitmq",
                UserName = "user",
                Password = "password"
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            // Queue deklarieren
            _channel.QueueDeclare(queue: "document_queue",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var client = _httpClientFactory.CreateClient("DMS_DAL");
            var response = await client.GetAsync("/api/documents");

            if (response.IsSuccessStatusCode)
            {
                var items = await response.Content.ReadFromJsonAsync<IEnumerable<Document>>();
                var dtoItems = _mapper.Map<IEnumerable<DocumentDto>>(items);
                return Ok(dtoItems);
            }

            return StatusCode((int)response.StatusCode, "Fehler beim Abrufen der Dokumente aus dem DAL");
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var client = _httpClientFactory.CreateClient("DMS_DAL");
            var response = await client.GetAsync($"/api/documents/{id}");
            if (response.IsSuccessStatusCode)
            {
                var item = await response.Content.ReadFromJsonAsync<Document>();
                if (item != null)
                {
                    var dtoItem = _mapper.Map<DocumentDto>(item);
                    return Ok(dtoItem);
                }
                return NotFound();
            }

            return StatusCode((int)response.StatusCode, "Fehler beim Abrufen des Dokuments aus dem DAL");
        }

        [HttpPost]
        public async Task<IActionResult> Create(DocumentDto dtoItem)
        {
            var client = _httpClientFactory.CreateClient("DMS_DAL");
            var item = _mapper.Map<Document>(dtoItem);
            var response = await client.PostAsJsonAsync("/api/documents", item);
            Console.WriteLine("TEST");
            if (response.IsSuccessStatusCode)
            {
                // Nachricht an RabbitMQ senden
                try
                {
                    var message = JsonSerializer.Serialize(dtoItem);
                    SendToMessageQueue(message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Senden der Nachricht an RabbitMQ");
                    // Optional: Hier können Sie entscheiden, ob Sie den Fehler weitergeben möchten
                }

                _logger.LogInformation("Dokument erfolgreich erstellt mit ID {DocumentId}", item.Id);
                return CreatedAtAction(nameof(GetById), new { id = item.Id }, dtoItem);
            }

            _logger.LogError("Fehler beim Erstellen des Dokuments im DAL mit Statuscode {StatusCode}", response.StatusCode);
            return StatusCode((int)response.StatusCode, "Fehler beim Erstellen des Dokuments im DAL");
        }

        private void SendToMessageQueue(string message)
        {
            var body = Encoding.UTF8.GetBytes(message);
            _channel.BasicPublish(exchange: "",
                                 routingKey: "document_queue",
                                 basicProperties: null,
                                 body: body);
            _logger.LogInformation($"[x] Sent message to RabbitMQ: {message}");
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
}
