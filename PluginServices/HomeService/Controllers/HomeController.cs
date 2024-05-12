using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Shared.Models;

namespace HomeService.Controllers
{
    /// <summary>
    /// Controller for the home object
    /// </summary>
    [ApiController]
    [Route("/api/home", Name = "get_home")]
    public class HomeController(IMongoDatabase database) : ControllerBase
    {
        private readonly IMongoCollection<Home> _homes = database.GetCollection<Home>("Homes");

        /// <summary>
        /// Get the home object
        /// </summary>
        [HttpGet]
        public IActionResult GetHome()
        {
            var home = _homes.Find(_ => true).FirstOrDefault();
            if (home == null) return NotFound();
            return Ok(home);
        }
    }
}
