using Microsoft.AspNetCore.Mvc;

namespace DMS.Controllers
{
    [ApiController]
    [Route("/")] // Basis-Route (http://localhost:8081/)
    public class HomeController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetHardcodedData()
        {
            var hardcodedData = new
            {
                Message = "This is hardcoded data",
                Timestamp = DateTime.UtcNow,
                Status = 200
            };

            return Ok(hardcodedData);
        }
    }
}
