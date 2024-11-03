using Microsoft.AspNetCore.Mvc;
using DMS_REST_API.DTO;
using AutoMapper;
using DMS_DAL.Entities;
using Microsoft.Extensions.Logging;
using DMS_REST_API.Services; // Namespace für RabbitMQPublisher

namespace DMS_REST_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMapper _mapper;
        private readonly ILogger<DocumentController> _logger;
        private readonly RabbitMQPublisher _rabbitMQPublisher;

        public DocumentController(
            IHttpClientFactory httpClientFactory,
            IMapper mapper,
            ILogger<DocumentController> logger,
            RabbitMQPublisher rabbitMQPublisher)
        {
            _httpClientFactory = httpClientFactory;
            _mapper = mapper;
            _logger = logger;
            _rabbitMQPublisher = rabbitMQPublisher;
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

            _logger.LogError("Fehler beim Abrufen der Dokumente aus dem DAL mit Statuscode {StatusCode}", response.StatusCode);
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

            _logger.LogError("Fehler beim Abrufen des Dokuments aus dem DAL mit Statuscode {StatusCode}", response.StatusCode);
            return StatusCode((int)response.StatusCode, "Fehler beim Abrufen des Dokuments aus dem DAL");
        }

        [HttpPost]
        public async Task<IActionResult> Create(DocumentDto dtoItem)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var client = _httpClientFactory.CreateClient("DMS_DAL");
            var item = _mapper.Map<Document>(dtoItem);
            var response = await client.PostAsJsonAsync("/api/documents", item);

            if (response.IsSuccessStatusCode)
            {
                // Nachricht an RabbitMQ senden
                try
                {
                    _rabbitMQPublisher.PublishDocumentCreated(dtoItem);
                    _logger.LogInformation("Dokument erfolgreich erstellt mit ID {DocumentId}", dtoItem.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Senden der 'Created'-Nachricht an RabbitMQ");
                }

                return CreatedAtAction(nameof(GetById), new { id = dtoItem.Id }, dtoItem);
            }

            _logger.LogError("Fehler beim Erstellen des Dokuments im DAL mit Statuscode {StatusCode}", response.StatusCode);
            return StatusCode((int)response.StatusCode, "Fehler beim Erstellen des Dokuments im DAL");
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, DocumentDto dtoItem)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != dtoItem.Id)
            {
                return BadRequest("Die ID in der URL stimmt nicht mit der ID des Dokuments überein.");
            }

            var client = _httpClientFactory.CreateClient("DMS_DAL");
            var item = _mapper.Map<Document>(dtoItem);
            var response = await client.PutAsJsonAsync($"/api/documents/{id}", item);

            if (response.IsSuccessStatusCode)
            {
                // Nachricht an RabbitMQ senden
                try
                {
                    _rabbitMQPublisher.PublishDocumentUpdated(dtoItem);
                    _logger.LogInformation("Dokument erfolgreich aktualisiert mit ID {DocumentId}", dtoItem.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Senden der 'Updated'-Nachricht an RabbitMQ");
                }

                return NoContent();
            }

            _logger.LogError("Fehler beim Aktualisieren des Dokuments im DAL mit Statuscode {StatusCode}", response.StatusCode);
            return StatusCode((int)response.StatusCode, "Fehler beim Aktualisieren des Dokuments im DAL");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {            
            _logger.LogInformation("Delete-Methode aufgerufen mit ID {Id}", id);
            Console.WriteLine("TEST123");
            var client = _httpClientFactory.CreateClient("DMS_DAL");
            var response = await client.DeleteAsync($"/api/documents/{id}");
            if (response.IsSuccessStatusCode)
            {
                // Nachricht an RabbitMQ senden
                try
                {
                    _rabbitMQPublisher.PublishDocumentDeleted(id);
                    _logger.LogInformation("Dokument erfolgreich gelöscht mit ID {DocumentId}", id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Senden der 'Deleted'-Nachricht an RabbitMQ");
                }

                return NoContent();
            }

            _logger.LogError("Fehler beim Löschen des Dokuments im DAL mit Statuscode {StatusCode}", response.StatusCode);
            return StatusCode((int)response.StatusCode, "Fehler beim Löschen des Dokuments im DAL");
        }
    }
}
