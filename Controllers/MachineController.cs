  using Microsoft.AspNetCore.Mvc;
  using Test.Services;

  namespace Test.Controllers
  {
      [ApiController]
      [Route("api/[controller]")]
      public class MachineController : ControllerBase
      {
          private readonly MachineDataService _dataService;

          public MachineController(MachineDataService dataService)
          {
              _dataService = dataService;
          }

          [HttpGet]
          public IActionResult GetAll()
          {
              return Ok(_dataService.GetAll());
          }
      }
  }