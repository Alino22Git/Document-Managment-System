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
using Minio;
using Minio.DataModel.Args;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Nodes;
using log4net;

namespace DMS_REST_API.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly ILogger<DocumentController> _logger;

        private readonly IDocumentRepository _repository;
        private readonly IMapper _mapper;

        private readonly IRabbitMQPublisher _rabbitMQPublisher;
        private readonly IMinioClient _minioClient; 
        private const string BucketName = "uploads";
        private readonly ElasticsearchClient _client;
        public DocumentController(
            ElasticsearchClient client,
            IDocumentRepository repository,
            IMapper mapper,
            ILogger<DocumentController> logger,
            IRabbitMQPublisher rabbitMQPublisher,
            IMinioClient minio)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
            _rabbitMQPublisher = rabbitMQPublisher;
            _client = client;
            _minioClient = minio;
        }

        /// <summary>
        /// Ruft alle Dokumente ab.
        /// </summary>
        /// <returns>Liste aller Dokumente.</returns>
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            _logger.LogInformation("GET /api/document aufgerufen.");
            try
            {
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
                _logger.LogInformation("GET /api/document/{Id} aufgerufen.", id);
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
        /// Stellt sicher, dass der MinIO-Bucket existiert.
        /// </summary>
        private async Task EnsureBucketExists()
        {
            bool found = await _minioClient.BucketExistsAsync(new BucketExistsArgs().WithBucket(BucketName));
            if (!found)
            {
                await _minioClient.MakeBucketAsync(new MakeBucketArgs().WithBucket(BucketName));
                _logger.LogInformation("Bucket '{Bucket}' erstellt.", BucketName);
            }
        }
        
        /// <summary>
        /// Lädt eine Datei hoch und erstellt einen neuen Dokumenteintrag in der Datenbank.
        /// </summary>
        /// <param name="uploadDto">Die hochzuladende Datei sowie Titel und Typ des Dokuments.</param>
        /// <returns>Details des hochgeladenen Dokuments.</returns>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadFile([FromForm] DocumentUploadDto uploadDto)
        {
            _logger.LogInformation("UploadFile-Methode aufgerufen.");

            // Validierung der Eingabedaten
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model-Validierung für UploadFile fehlgeschlagen: {ModelState}", ModelState);

                // Detaillierte Fehler in die Logs schreiben
                foreach (var key in ModelState.Keys)
                {
                    var errors = ModelState[key].Errors;
                    foreach (var error in errors)
                    {
                        _logger.LogWarning("Model-Validation error for key '{Key}': {Error}", key, error.ErrorMessage);
                    }
                }

                return BadRequest(ModelState);
            }

            // Zusätzliche Validierungen (optional, da [Required] bereits gesetzt sind)
            if (uploadDto.File == null || uploadDto.File.Length == 0)
            {
                _logger.LogWarning("Keine Datei zum Hochladen erhalten.");
                return BadRequest("Datei fehlt!");
            }

            if (string.IsNullOrWhiteSpace(uploadDto.Title))
            {
                _logger.LogWarning("Dokumenttitel fehlt.");
                return BadRequest("Dokumenttitel fehlt!");
            }

            if (string.IsNullOrWhiteSpace(uploadDto.FileType))
            {
                _logger.LogWarning("Dokumenttyp fehlt.");
                return BadRequest("Dokumenttyp fehlt!");
            }

            await EnsureBucketExists();

            var fileName = Path.GetFileName(uploadDto.File.FileName);
            await using var fileStream = uploadDto.File.OpenReadStream();

            try
            {
                // Datei zu MinIO hochladen
                await _minioClient.PutObjectAsync(new PutObjectArgs()
                    .WithBucket(BucketName)
                    .WithObject(fileName)
                    .WithStreamData(fileStream)
                    .WithObjectSize(uploadDto.File.Length));

                _logger.LogInformation("Datei {FileName} erfolgreich hochgeladen.", fileName);

                // Dokumententität ohne Content erstellen
                var document = new Document
                {
                    Title = uploadDto.Title,
                    FileType = uploadDto.FileType,
                    FileName = fileName,
                    Content = "OCR wird verarbeitet..." // Wird später durch den Listener-Service aktualisiert
                };

                // Dokument in die Datenbank einfügen, um die ID zu erhalten
                await _repository.AddDocumentAsync(document);
                _logger.LogInformation("Dokumententität ohne Content in die Datenbank eingefügt mit ID {DocumentId}.", document.Id);

                // Nachricht an RabbitMQ senden, um den OCR-Prozess zu initiieren
                try
                {
                    var createdDto = _mapper.Map<DocumentDto>(document);
                    _logger.LogInformation("Sende OCR-Anfrage für Dokument {DocumentId}.", document.Id);

                    // Nachricht an RabbitMQ senden
                    _rabbitMQPublisher.PublishDocumentCreated(createdDto);
                    _logger.LogInformation("OCR-Anfrage an RabbitMQ gesendet für Dokument ID {DocumentId}.", document.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Senden der 'Created'-Nachricht an RabbitMQ.");
                    // Optional: Sie könnten hier weitere Schritte unternehmen, z.B. den Upload rückgängig machen
                }

                // Rückgabe der Dokumentdetails ohne Content (wird später vom Listener-Service aktualisiert)
                var returnItem = _mapper.Map<DocumentDto>(document);
                return CreatedAtAction(nameof(GetById), new { id = document.Id }, returnItem);
            }
            catch (Minio.Exceptions.MinioException ex)
            {
                _logger.LogError(ex, "MinIO Fehler beim Hochladen der Datei {FileName}.", fileName);
                return StatusCode(500, "Interner Serverfehler beim Hochladen der Datei.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Allgemeiner Fehler beim Hochladen der Datei {FileName}.", fileName);
                return StatusCode(500, "Interner Serverfehler beim Hochladen der Datei.");
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
            _logger.LogInformation("Create-Methode aufgerufen.");

            if (dtoItem == null)
            {
                _logger.LogWarning("POST-Anfrage mit null DocumentDto empfangen.");
                return BadRequest(new { message = "Dokument darf nicht null sein." });
            }

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model-Validierung für POST fehlgeschlagen: {ModelState}", ModelState);
                return BadRequest(ModelState);
            }

            try
            {
                // Dokumententität ohne Content erstellen
                var document = _mapper.Map<Document>(dtoItem);
                await _repository.AddDocumentAsync(document);
                _logger.LogInformation("Dokumententität ohne Content in die Datenbank eingefügt mit ID {DocumentId}.", document.Id);

                // Nachricht an RabbitMQ senden, um den OCR-Prozess zu initiieren
                try
                {
                    var createdDto = _mapper.Map<DocumentDto>(document);
                    _logger.LogInformation("Sende OCR-Anfrage für Dokument {DocumentId}.", document.Id);

                    // Nachricht an RabbitMQ senden
                    _rabbitMQPublisher.PublishDocumentCreated(createdDto);
                    _logger.LogInformation("OCR-Anfrage an RabbitMQ gesendet für Dokument ID {DocumentId}.", document.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Senden der 'Created'-Nachricht an RabbitMQ.");
                    // Optional: Sie könnten hier weitere Schritte unternehmen, z.B. den Upload rückgängig machen
                }

                // Rückgabe der Dokumentdetails ohne Content (wird später vom Listener-Service aktualisiert)
                var returnItem = _mapper.Map<DocumentDto>(document);
                returnItem.Content = "OCR wird verarbeitet...";
                return CreatedAtAction(nameof(GetById), new { id = document.Id }, returnItem);
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
        public async Task<IActionResult> Update(int id, [FromBody] DocumentUpdateDto dtoItem)
        {
            if (dtoItem == null)
            {
                _logger.LogWarning("PUT-Anfrage mit null DocumentDto empfangen.");
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

                // Aktualisieren der Eigenschaften ohne Content
                existingDocument.Title = dtoItem.Title;

                await _repository.UpdateDocumentAsync(existingDocument);
                _logger.LogInformation("Dokumententität ohne Content aktualisiert für ID {Id}.", id);

                // Nachricht an RabbitMQ senden, um den OCR-Prozess zu initiieren
                try
                {
                    var createdDto = _mapper.Map<DocumentDto>(existingDocument);
                    _logger.LogInformation("Sende OCR-Anfrage für Dokument {DocumentId}.", existingDocument.Id);

                    // Nachricht an RabbitMQ senden
                    _rabbitMQPublisher.PublishDocumentCreated(createdDto);
                    _logger.LogInformation("OCR-Anfrage an RabbitMQ gesendet für Dokument ID {DocumentId}.", existingDocument.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Senden der 'Created'-Nachricht an RabbitMQ.");
                    // Optional: Weitere Schritte unternehmen
                }

                // Rückgabe der Dokumentdetails ohne Content (wird später vom Listener-Service aktualisiert)
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
            try
            {
                var document = await _repository.GetDocumentAsync(id);
                if (document == null)
                {
                    _logger.LogWarning("Dokument mit ID {Id} wurde nicht gefunden.", id);
                    return NotFound(new { message = $"Dokument mit ID {id} wurde nicht gefunden." });
                }

                await _repository.DeleteDocumentAsync(id);
                _logger.LogInformation("Dokumententität gelöscht für ID {Id}.", id);

                // Nachricht an RabbitMQ senden
                try
                {
                    _rabbitMQPublisher.PublishDocumentDeleted(id);
                    _logger.LogInformation("Dokument erfolgreich gelöscht mit ID {DocumentId}.", id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Senden der 'Deleted'-Nachricht an RabbitMQ.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Löschen des Dokuments mit ID {Id}.", id);
                return StatusCode(500, "Interner Serverfehler beim Löschen des Dokuments.");
            }
        }

        /// <summary>
        /// Sucht in Elasticsearch mit einem query.
        /// </summary>
        /// <param name="searchTerm">Zu suchender Text.</param>
        /// <returns>Eine Liste von Dokumenten, die den Searchterm enthalten.</returns>
        [HttpPost("search/fuzzy")]
        public async Task<IActionResult> SearchByFuzzy([FromBody] string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest(new { message = "Search term cannot be empty" });
            }
            try
            {
                var response = await _client.SearchAsync<Document>(s => s
                    .Index("documents")
                    .Query(q => q.MultiMatch(mm => mm
                        .Fields(new[] { "content", "title" })   // Beide Felder angeben
                        .Query(searchTerm)
                        .Fuzziness(new Fuzziness(2))
                    ))
                );

            return HandleSearchResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Suchen von Dokumenten mit dem Suchbegriff {SearchTerm}.", searchTerm);
                return StatusCode(500, "Interner Serverfehler beim Suchen von Dokumenten.");
            }
        }


        private IActionResult HandleSearchResponse(SearchResponse<Document> response)
        {
            if (response.IsValidResponse)
            {
                if (response.Documents.Any())
                {
                    return Ok(response.Documents);
                }
                return NotFound(new { message = "No documents found matching the search term." });
            }

            return StatusCode(500, new { message = "Failed to search documents", details = response.DebugInformation });
        }

        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadFile(int id)
        {
            try
            {
                _logger.LogInformation("GET /api/document/download/{Id} aufgerufen.", id);

                var document = await _repository.GetDocumentAsync(id);
                if (document == null)
                {
                    _logger.LogWarning("Dokument mit ID {Id} wurde nicht gefunden.", id);
                    return NotFound(new { message = $"Dokument mit ID {id} wurde nicht gefunden." });
                }

                var fileName = document.FileName;
                if (string.IsNullOrEmpty(fileName))
                {
                    _logger.LogWarning("Dokument mit ID {Id} hat keinen gültigen Dateinamen.", id);
                    return BadRequest(new { message = $"Dokument mit ID {id} hat keinen gültigen Dateinamen." });
                }

                var ms = new MemoryStream();
                try
                {
                    var args = new GetObjectArgs()
                        .WithBucket(BucketName)
                        .WithObject(fileName)
                        .WithCallbackStream((stream) =>
                        {
                            stream.CopyTo(ms);
                        });

                    await _minioClient.GetObjectAsync(args);

                    ms.Position = 0; // Stream zurücksetzen, um von Anfang an zu lesen
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Herunterladen der Datei {FileName} von MinIO.", fileName);
                    return StatusCode(500, "Interner Serverfehler beim Herunterladen der Datei.");
                }

                // PDF ausliefern
                return File(ms, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fehler beim Herunterladen der Datei für Dokument mit ID {Id}.", id);
                return StatusCode(500, "Interner Serverfehler beim Herunterladen der Datei.");
            }
        }

    }
}
