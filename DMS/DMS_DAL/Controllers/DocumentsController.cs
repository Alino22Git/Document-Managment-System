using Microsoft.AspNetCore.Mvc;
using DMS_DAL.Repositories;
using DMS_DAL.Entities;

namespace DMS_DAL.Controllers
{
    
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController(IDocumentRepository repository) : ControllerBase
    {
        [HttpGet]
        public async Task<IEnumerable<Document>> GetAsync()
        {
            return await repository.GetAllDocumentsAsync();
        }

        [HttpPost]
        public async Task<IActionResult> PostAsync(Document item)
        {
            if (string.IsNullOrWhiteSpace(item.Title))
            {
                return BadRequest(new { message = "Task name cannot be empty." });
            }
            await repository.AddDocumentAsync(item);
            return Ok();
        }
    }
    
}
