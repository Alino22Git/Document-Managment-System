using Microsoft.AspNetCore.Mvc;
using DMS_DAL.Repositories;
using DMS_DAL.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace DMS_DAL.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentRepository _repository;

        public DocumentsController(IDocumentRepository repository)
        {
            _repository = repository;
        }

        // GET: api/Documents
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Document>>> GetAsync()
        {
            Console.WriteLine("TESTDAL");
            var documents = await _repository.GetAllDocumentsAsync();
            return Ok(documents);
        }

        // GET: api/Documents/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Document>> GetByIdAsync(int id)
        {
            var document = await _repository.GetDocumentAsync(id);
            if (document == null)
            {
                return NotFound(new { message = $"Dokument mit ID {id} wurde nicht gefunden." });
            }
            return Ok(document);
        }

        // POST: api/Documents
        [HttpPost]
        public async Task<IActionResult> PostAsync([FromBody] Document item)
        {
            if (item == null)
            {
                return BadRequest(new { message = "Dokument darf nicht null sein." });
            }

            // Model-Validierung prüfen
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            await _repository.AddDocumentAsync(item);

            // Nach dem Hinzufügen sollte das Item eine generierte ID haben
            return CreatedAtAction(nameof(GetByIdAsync), new { id = item.Id }, item);
        }

        // PUT: api/Documents/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAsync(int id, [FromBody] Document item)
        {
            if (item == null)
            {
                return BadRequest(new { message = "Dokument darf nicht null sein." });
            }

            // Überprüfen, ob die ID in der URL mit der ID im Body übereinstimmt
            if (id != item.Id)
            {
                return BadRequest(new { message = "Die ID in der URL stimmt nicht mit der ID im Body überein." });
            }

            // Model-Validierung prüfen
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingDocument = await _repository.GetDocumentAsync(id);
            if (existingDocument == null)
            {
                return NotFound(new { message = $"Dokument mit ID {id} wurde nicht gefunden." });
            }

            // Aktualisieren der Eigenschaften
            existingDocument.Title = item.Title;
            existingDocument.FileType = item.FileType;

            // Speichern der Änderungen
            try
            {
                await _repository.UpdateDocumentAsync(existingDocument);
            }
            catch (DbUpdateException ex)
            {
                // Optional: Loggen Sie den Fehler hier, falls Logging eingerichtet ist
                return StatusCode(500, new { message = "Fehler beim Aktualisieren des Dokuments.", details = ex.Message });
            }

            return NoContent();
        }

        // DELETE: api/Documents/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            var document = await _repository.GetDocumentAsync(id);
            if (document == null)
            {
                return NotFound(new { message = $"Dokument mit ID {id} wurde nicht gefunden." });
            }

            try
            {
                await _repository.DeleteDocumentAsync(id);
            }
            catch (DbUpdateException ex)
            {
                // Optional: Loggen Sie den Fehler hier, falls Logging eingerichtet ist
                return StatusCode(500, new { message = "Fehler beim Löschen des Dokuments.", details = ex.Message });
            }

            return NoContent();
        }
    }
}
