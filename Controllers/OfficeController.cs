using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MongoDB.Driver;
using ElectionApi.QueueModels.Votes;
using ElectionApi.QueueServices;
using ElectionApi.Repositories;
using ElectionApi.Services;
using ElectionApi.Models;
using ElectionApi.ViewModels;


namespace ElectionApi.Controllers;

[ApiController]
[Route("offices")]
public class OfficeController : ControllerBase
{

    private readonly ILogger<OfficeController> _logger;
    private readonly IOfficeRepository _officeRepository;

    public OfficeController(ILogger<OfficeController> logger, IDataService ds)
    {
        _logger = logger;
        _officeRepository = ds.Offices;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var offices = _officeRepository.AsQueryable();
        return Ok(offices);
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] AddOfficeViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }
        
        var office = new Office()
        {
            OriginalId = model.Id,
            Offices = model.Offices ?? 0,
            Code = model.Code,
            Registered = model.Registered,
            Province = model.Province
        };
        await _officeRepository.InsertOneAsync(office);
        return Ok(office);
    }
}
