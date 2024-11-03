// DMS_REST_API/Controllers/DocumentsController.cs
using Microsoft.AspNetCore.Mvc;
using DMS_REST_API.DTO;
using AutoMapper;
using DMS_DAL.Entities;
using Microsoft.Extensions.Logging;
using DMS_REST_API.Services; // Namespace für RabbitMQPublisher
using DMS_DAL.Repositories;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DMS_REST_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<DocumentController> _logger;
        private readonly RabbitMQPublisher _rabbitMQPublisher;

        public DocumentController(
            IDocumentRepository repository,
            IMapper mapper,
            ILogger<DocumentController> logger,
            RabbitMQPublisher rabbitMQPublisher)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
            _rabbitMQPublisher = rabbitMQPublisher;
        }

        /// <summary>
        /// Ruft alle Dokumente ab.
        /// </summary>
        /// <returns>Liste aller Dokumente.</returns>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            Console.WriteLine("TESTAPI123");
            try
            {
                _logger.LogInformation("GET /api/documents aufgerufen.");
                var documents = await _repository.GetAllDocumentsAsync();
                var dtoDocuments = _mapper.Map<IEnumerable<DocumentDto>>(documents);
                return Ok(dtoDocuments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Abrufen der Dokumente.");
                return StatusCode(500, "Interner Serverfehler beim Abrufen der Dokumente.");
            }
        }

        /// <summary>
        /// Ruft ein Dokument anhand der ID ab.
        /// </summary>
        /// <param name="id">ID des Dokuments.</param>
        /// <returns>Das angeforderte Dokument.</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                _logger.LogInformation("GET /api/documents/{Id} aufgerufen.", id);
                var document = await _repository.GetDocumentAsync(id);
                if (document == null)
                {
                    _logger.LogWarning("Dokument mit ID {Id} wurde nicht gefunden.", id);
                    return NotFound(new { message = $"Dokument mit ID {id} wurde nicht gefunden." });
                }

                var dtoDocument = _mapper.Map<DocumentDto>(document);
                return Ok(dtoDocument);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Abrufen des Dokuments mit ID {Id}.", id);
                return StatusCode(500, "Interner Serverfehler beim Abrufen des Dokuments.");
            }
        }

        /// <summary>
        /// Erstellt ein neues Dokument.
        /// </summary>
        /// <param name="dtoItem">Die Daten des zu erstellenden Dokuments.</param>
        /// <returns>Das erstellte Dokument.</returns>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DocumentDto dtoItem)
        {
            Console.WriteLine("TESTAPI12345678");
            if (dtoItem == null)
            {
                _logger.LogWarning("POST-Anfrage mit null DokumentDto empfangen.");
                return BadRequest(new { message = "Dokument darf nicht null sein." });
            }
            if (!ModelState.IsValid)
            {
                Console.WriteLine("TESTVALIDATION");
                _logger.LogWarning("Model-Validierung für POST fehlgeschlagen: {ModelState}", ModelState);
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Erstelle neues Dokument.");
                var document = _mapper.Map<Document>(dtoItem);
                await _repository.AddDocumentAsync(document);
                var createdDto = _mapper.Map<DocumentDto>(document);

                // Nachricht an RabbitMQ senden
                try
                {
                    _rabbitMQPublisher.PublishDocumentCreated(createdDto);
                    _logger.LogInformation("Dokument erfolgreich erstellt mit ID {DocumentId}.", createdDto.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Senden der 'Created'-Nachricht an RabbitMQ.");
                    // Optional: Weiteres Handling, z.B. Rollback oder spezielle Rückmeldung
                }

                return CreatedAtAction(nameof(GetById), new { id = createdDto.Id }, createdDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Erstellen des Dokuments.");
                return StatusCode(500, "Interner Serverfehler beim Erstellen des Dokuments.");
            }
        }

        /// <summary>
        /// Aktualisiert ein bestehendes Dokument.
        /// </summary>
        /// <param name="id">ID des zu aktualisierenden Dokuments.</param>
        /// <param name="dtoItem">Die aktualisierten Daten des Dokuments.</param>
        /// <returns>Kein Inhalt bei Erfolg.</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] DocumentDto dtoItem)
        {
            if (dtoItem == null)
            {
                _logger.LogWarning("PUT-Anfrage mit null DokumentDto empfangen.");
                return BadRequest(new { message = "Dokument darf nicht null sein." });
            }

            if (id != dtoItem.Id)
            {
                _logger.LogWarning("ID in URL ({UrlId}) stimmt nicht mit ID im Body ({BodyId}) überein.", id, dtoItem.Id);
                return BadRequest(new { message = "Die ID in der URL stimmt nicht mit der ID im Body überein." });
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model-Validierung für PUT fehlgeschlagen: {ModelState}", ModelState);
                return BadRequest(ModelState);
            }

            try
            {
                _logger.LogInformation("Aktualisiere Dokument mit ID {Id}.", id);
                var existingDocument = await _repository.GetDocumentAsync(id);
                if (existingDocument == null)
                {
                    _logger.LogWarning("Dokument mit ID {Id} wurde nicht gefunden.", id);
                    return NotFound(new { message = $"Dokument mit ID {id} wurde nicht gefunden." });
                }

                // Aktualisieren der Eigenschaften
                existingDocument.Title = dtoItem.Title;
                existingDocument.FileType = dtoItem.FileType;

                await _repository.UpdateDocumentAsync(existingDocument);

                // Nachricht an RabbitMQ senden
                try
                {
                    _rabbitMQPublisher.PublishDocumentUpdated(dtoItem);
                    _logger.LogInformation("Dokument erfolgreich aktualisiert mit ID {DocumentId}.", dtoItem.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Senden der 'Updated'-Nachricht an RabbitMQ.");
                    // Optional: Weiteres Handling
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Aktualisieren des Dokuments mit ID {Id}.", id);
                return StatusCode(500, "Interner Serverfehler beim Aktualisieren des Dokuments.");
            }
        }

        /// <summary>
        /// Löscht ein bestehendes Dokument.
        /// </summary>
        /// <param name="id">ID des zu löschenden Dokuments.</param>
        /// <returns>Kein Inhalt bei Erfolg.</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {            
            _logger.LogInformation("Delete-Methode aufgerufen mit ID {Id}.", id);
            Console.WriteLine("TEST123");
            try
            {
                var document = await _repository.GetDocumentAsync(id);
                if (document == null)
                {
                    _logger.LogWarning("Dokument mit ID {Id} wurde nicht gefunden.", id);
                    return NotFound(new { message = $"Dokument mit ID {id} wurde nicht gefunden." });
                }

                await _repository.DeleteDocumentAsync(id);

                // Nachricht an RabbitMQ senden
                try
                {
                    _rabbitMQPublisher.PublishDocumentDeleted(id);
                    _logger.LogInformation("Dokument erfolgreich gelöscht mit ID {DocumentId}.", id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Senden der 'Deleted'-Nachricht an RabbitMQ.");
                    // Optional: Weiteres Handling
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Löschen des Dokuments mit ID {Id}.", id);
                return StatusCode(500, "Interner Serverfehler beim Löschen des Dokuments.");
            }
        }
    }
}
