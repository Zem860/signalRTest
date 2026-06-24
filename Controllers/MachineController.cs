using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Test.Models;
namespace Test.Controller
{
    public class MachineController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;

        public MachineController(IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet]
        public IActionResult GetAll()
        {
            var dataPath = Path.Combine(_env.ContentRootPath, "data");
            var files = Directory.GetFiles(dataPath, "*.json");
            var machines = new List<Machine>();
            foreach (var file in files)
            {
                var json = System.IO.File.ReadAllText(file);
                var machine = JsonSerializer.Deserialize<Machine>(json);
                if (machine != null)
                {
                    machines.Add(machine);
                }


            }
            return Ok(machines);
        }


    }
}