using Microsoft.AspNetCore.Mvc;
using DMS_REST_API.DTO;
using AutoMapper;
using DMS_DAL.Entities;

namespace DMS_REST_API.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class DocumentController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMapper _mapper;

        public DocumentController(IHttpClientFactory httpClientFactory, IMapper mapper)
        {
            _httpClientFactory = httpClientFactory;
            _mapper = mapper;
        }


        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var client = _httpClientFactory.CreateClient("DMS_DAL");
            var response = await client.GetAsync("/api/document"); 

            if (response.IsSuccessStatusCode)
            {
                var items = await response.Content.ReadFromJsonAsync<IEnumerable<Document>>();
                var dtoItems = _mapper.Map<IEnumerable<DocumentDto>>(items);
                return Ok(dtoItems);
            }

            return StatusCode((int)response.StatusCode, "Error retrieving Documents from DAL");
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var client = _httpClientFactory.CreateClient("DMS_DAL");
            var response = await client.GetAsync($"/api/document/{id}");
            if (response.IsSuccessStatusCode)
            {
                var item = await response.Content.ReadFromJsonAsync<DocumentDto>(); 
                if (item != null)
                {
                    var dtoItem = _mapper.Map<DocumentDto>(item);
                    return Ok(dtoItem);
                }
                return NotFound();
            }

            return StatusCode((int)response.StatusCode, "Error retrieving Document from DAL");
        }

        [HttpPost]
        public async Task<IActionResult> Create(DocumentDto dtoItem)
        {
            var client = _httpClientFactory.CreateClient("DMS_DAL");
            var item = _mapper.Map<Document>(dtoItem);
            var response = await client.PostAsJsonAsync("/api/document", item);

            if (response.IsSuccessStatusCode)
            {
                return CreatedAtAction(nameof(GetById), new { id = item.Id }, dtoItem);
            }

            return StatusCode((int)response.StatusCode, "Error creating Document in DAL");
        }


    }
    
}

