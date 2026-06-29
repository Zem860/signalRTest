using Microsoft.AspNetCore.Mvc;

namespace Test.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImagesController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        public ImagesController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet]
        public IActionResult Get()
        {
            var path = Path.Combine(_env.ContentRootPath, "data", "images.json");
            var json = System.IO.File.ReadAllText(path);
            return Content(json, "application/json");
        }
    }
}