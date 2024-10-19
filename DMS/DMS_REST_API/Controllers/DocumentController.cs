
using Microsoft.AspNetCore.Mvc;
using DMS_REST_API.Entities;
using DMS_REST_API.DataPersistence;
namespace DMS_REST_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DocumentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateDocument([FromBody] Document document)
        {
            _context.Documents.Add(document);
            await _context.SaveChangesAsync();

            return Ok(document);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocument(int id)
        {
            var document = await _context.Documents.FindAsync(id);
            if (document == null)
            {
                return NotFound();
            }

            return Ok(document);
        }
    }
}

