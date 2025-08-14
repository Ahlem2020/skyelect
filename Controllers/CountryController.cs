using Microsoft.AspNetCore.Mvc;
using ElectionApi.Repositories;
using ElectionApi.Services;
using ElectionApi.Models;
using ElectionApi.ViewModels;

namespace ElectionApi.Controllers;

[ApiController]
[Route("countries")]
public class CountryController : ControllerBase
{
    private readonly ILogger<CountryController> _logger;
    private readonly ICountryRepository _countryRepository;
    private readonly IOfficeRepository _officeRepository;

    public CountryController(ILogger<CountryController> logger, IDataService ds)
    {
        _logger = logger;
        _countryRepository = ds.Countries;
        _officeRepository = ds.Offices;
    }

    [HttpGet]
    public IActionResult GetAll()
    {
        var countries = _countryRepository.AsQueryable();
        return Ok(countries);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var country = await _countryRepository.FindByIdAsync(id);
        if (country == null)
        {
            return NotFound("Country not found");
        }
        return Ok(country);
    }

    [HttpGet("{id}/offices")]
    public async Task<IActionResult> GetCountryOffices(string id)
    {
        var country = await _countryRepository.FindByIdAsync(id);
        if (country == null)
        {
            return NotFound("Country not found");
        }

        var offices = _officeRepository.AsQueryable()
            .Where(o => o.CountryId == id)
            .ToList();

        return Ok(new { 
            country = country.Name, 
            officeCount = offices.Count,
            offices 
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AddCountryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        // Check if country already exists
        var existingCountry = await _countryRepository.GetByNameAsync(model.Name);
        if (existingCountry != null)
        {
            ModelState.AddModelError("Name", "Country with this name already exists");
            return UnprocessableEntity(ModelState);
        }

        var country = new Country
        {
            Name = model.Name,
            Flag = model.Flag ?? string.Empty,
            Offices = model.Offices ?? new List<string>(),
            Candidates = model.Candidates ?? new List<string>()
        };

        await _countryRepository.InsertOneAsync(country);
        return Ok(country);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateCountryViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }

        var country = await _countryRepository.FindByIdAsync(id);
        if (country == null)
        {
            return NotFound("Country not found");
        }

        // Check if new name conflicts with another country
        if (model.Name != country.Name)
        {
            var existingCountry = await _countryRepository.GetByNameAsync(model.Name);
            if (existingCountry != null)
            {
                ModelState.AddModelError("Name", "Country with this name already exists");
                return UnprocessableEntity(ModelState);
            }
        }

        country.Name = model.Name;
        country.Flag = model.Flag ?? country.Flag;
        country.Offices = model.Offices ?? country.Offices;
        country.Candidates = model.Candidates ?? country.Candidates;

        await _countryRepository.ReplaceOneAsync(country);

        // Update all offices that reference this country
        var offices = _officeRepository.AsQueryable()
            .Where(o => o.CountryId == id)
            .ToList();

        foreach (var office in offices)
        {
            office.CountryName = country.Name;
            await _officeRepository.ReplaceOneAsync(office);
        }

        return Ok(country);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var country = await _countryRepository.FindByIdAsync(id);
        if (country == null)
        {
            return NotFound("Country not found");
        }

        // Unlink all offices from this country
        var offices = _officeRepository.AsQueryable()
            .Where(o => o.CountryId == id)
            .ToList();

        foreach (var office in offices)
        {
            office.CountryId = null;
            office.CountryName = null;
            await _officeRepository.ReplaceOneAsync(office);
        }

        await _countryRepository.DeleteByIdAsync(id);
        return Ok(new { message = "Country deleted successfully" });
    }
}
