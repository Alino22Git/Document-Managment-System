using Microsoft.AspNetCore.Mvc;
using DMS_REST_API.DTO;

namespace DMS_REST_API.Controllers
{
    public class DocumentContoller
    {

        [ApiController]
        [Route("[controller]")]
        public class DocumentController : ControllerBase
        {
            private readonly IHttpClientFactory _httpClientFactory;

            public DocumentController(IHttpClientFactory httpClientFactory)
            {
                _httpClientFactory = httpClientFactory;
            }


            [HttpGet]
            public async Task<IActionResult> Get()
            {
                var client = _httpClientFactory.CreateClient("documents");//Idk Pls Fix later
                var response = await client.GetAsync("/api/dms_dal"); 

                if (response.IsSuccessStatusCode)
                {
                    var items = await response.Content.ReadFromJsonAsync<IEnumerable<DocumentDto>>();
                    return Ok(items);
                }

                return StatusCode((int)response.StatusCode, "Error retrieving Documents from DAL");
            }
            [HttpGet("{id}")]
            public async Task<IActionResult> GetById(int id)
            {
                var client = _httpClientFactory.CreateClient("TodoDAL");//FIx Later
                var response = await client.GetAsync($"/api/todo/{id}");
                if (response.IsSuccessStatusCode)
                {
                    var item = await response.Content.ReadFromJsonAsync<DocumentDto>();
                    if (item != null)
                    {
                        return Ok(item);
                    }
                    return NotFound();
                }

                return StatusCode((int)response.StatusCode, "Error retrieving Document from DAL");
            }

            [HttpPost]
            public async Task<IActionResult> Create(DocumentDto item)
            {
                var client = _httpClientFactory.CreateClient("TodoDAL");
                var response = await client.PostAsJsonAsync("/api/todo", item); //FIX

                if (response.IsSuccessStatusCode)
                {
                    return CreatedAtAction(nameof(GetById), new { id = item.Id }, item);
                }

                return StatusCode((int)response.StatusCode, "Error creating Document in DAL");
            }

            
        }
    }
}

