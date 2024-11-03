using Microsoft.AspNetCore.Mvc;
using DMS_DAL.Repositories;
using DMS_DAL.Entities;

namespace DMS_DAL.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentRepository _repository;

        public DocumentsController(IDocumentRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public async Task<IEnumerable<Document>> GetAsync()
        {
            return await _repository.GetAllDocumentsAsync();
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync(Document item)
        {
            if (string.IsNullOrWhiteSpace(item.Title))
            {
                return BadRequest(new { message = "Task name cannot be empty." });
            }
            await _repository.AddDocumentAsync(item);
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(int id)
        {
            var document = await _repository.GetDocumentAsync(id);
            if (document == null)
            {
                return NotFound();
            }

            await _repository.DeleteDocumentAsync(id);
            return NoContent();
        }
    }
}