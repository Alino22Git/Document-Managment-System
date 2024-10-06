using Microsoft.AspNetCore.Mvc;

namespace DMS.Controllers
{
    [ApiController]
    [Route("/")]
    public class HomeController : ControllerBase
    {
        
        
        /// <summary>
        /// Default route returning hardcoded data.
        /// </summary>
        /// <returns>A JSON object containing a message, timestamp, and status code.</returns>
        [HttpGet]
        public IActionResult GetHardcodedData()
        {
            var hardcodedData = new
            {
                Message = "Hello World",
                Timestamp = DateTime.UtcNow,
                Status = 200
            };
            return Ok(hardcodedData);
        }
    }
}
