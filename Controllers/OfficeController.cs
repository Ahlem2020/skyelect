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
    private readonly ICountryRepository _countryRepository;

    public OfficeController(ILogger<OfficeController> logger, IDataService ds)
    {
        _logger = logger;
        _officeRepository = ds.Offices;
        _countryRepository = ds.Countries;
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

        Country? country = null;
        if (!string.IsNullOrEmpty(model.CountryId))
        {
            country = await _countryRepository.FindByIdAsync(model.CountryId);
            if (country == null)
            {
                ModelState.AddModelError("CountryId", "Country not found");
                return UnprocessableEntity(ModelState);
            }
        }
        
        var office = new Office()
        {
            OriginalId = model.Id,
            Offices = model.Offices ?? 0,
            Code = model.Code,
            Registered = model.Registered,
            Province = model.Province,
            CountryId = model.CountryId,
            CountryName = country?.Name
        };

        await _officeRepository.InsertOneAsync(office);

        // Update the country's offices list if country is specified
        if (country != null && office.Id != null)
        {
            if (!country.Offices.Contains(office.Id.ToString()))
            {
                country.Offices.Add(office.Id.ToString());
                await _countryRepository.ReplaceOneAsync(country);
            }
        }

        return Ok(office);
    }

    [HttpGet("by-country/{countryId}")]
    public async Task<IActionResult> GetOfficesByCountry(string countryId)
    {
        var country = await _countryRepository.FindByIdAsync(countryId);
        if (country == null)
        {
            return NotFound("Country not found");
        }

        var offices = _officeRepository.AsQueryable()
            .Where(o => o.CountryId == countryId)
            .ToList();

        return Ok(offices);
    }

    [HttpPost("{officeId}/link-country/{countryId}")]
    public async Task<IActionResult> LinkOfficeToCountry(string officeId, string countryId)
    {
        var office = await _officeRepository.FindByIdAsync(officeId);
        if (office == null)
        {
            return NotFound("Office not found");
        }

        var country = await _countryRepository.FindByIdAsync(countryId);
        if (country == null)
        {
            return NotFound("Country not found");
        }

        // Remove from previous country if linked
        if (!string.IsNullOrEmpty(office.CountryId) && office.CountryId != countryId)
        {
            var previousCountry = await _countryRepository.FindByIdAsync(office.CountryId);
            if (previousCountry != null)
            {
                previousCountry.Offices.Remove(officeId);
                await _countryRepository.ReplaceOneAsync(previousCountry);
            }
        }

        // Update office
        office.CountryId = countryId;
        office.CountryName = country.Name;
        await _officeRepository.ReplaceOneAsync(office);

        // Update country
        if (!country.Offices.Contains(officeId))
        {
            country.Offices.Add(officeId);
            await _countryRepository.ReplaceOneAsync(country);
        }

        return Ok(new { message = "Office successfully linked to country", office, country = country.Name });
    }

    [HttpPost("{officeId}/unlink-country")]
    public async Task<IActionResult> UnlinkOfficeFromCountry(string officeId)
    {
        var office = await _officeRepository.FindByIdAsync(officeId);
        if (office == null)
        {
            return NotFound("Office not found");
        }

        if (!string.IsNullOrEmpty(office.CountryId))
        {
            var country = await _countryRepository.FindByIdAsync(office.CountryId);
            if (country != null)
            {
                country.Offices.Remove(officeId);
                await _countryRepository.ReplaceOneAsync(country);
            }

            office.CountryId = null;
            office.CountryName = null;
            await _officeRepository.ReplaceOneAsync(office);
        }

        return Ok(new { message = "Office successfully unlinked from country", office });
    }

    [HttpPost("link-multiple-to-country/{countryId}")]
    public async Task<IActionResult> LinkMultipleOfficesToCountry(string countryId, [FromBody] List<string> officeIds)
    {
        var country = await _countryRepository.FindByIdAsync(countryId);
        if (country == null)
        {
            return NotFound("Country not found");
        }

        var linkedOffices = new List<Office>();
        var errors = new List<string>();

        foreach (var officeId in officeIds)
        {
            var office = await _officeRepository.FindByIdAsync(officeId);
            if (office == null)
            {
                errors.Add($"Office with ID {officeId} not found");
                continue;
            }

            // Remove from previous country if linked
            if (!string.IsNullOrEmpty(office.CountryId) && office.CountryId != countryId)
            {
                var previousCountry = await _countryRepository.FindByIdAsync(office.CountryId);
                if (previousCountry != null)
                {
                    previousCountry.Offices.Remove(officeId);
                    await _countryRepository.ReplaceOneAsync(previousCountry);
                }
            }

            // Update office
            office.CountryId = countryId;
            office.CountryName = country.Name;
            await _officeRepository.ReplaceOneAsync(office);

            // Add to country's offices list
            if (!country.Offices.Contains(officeId))
            {
                country.Offices.Add(officeId);
            }

            linkedOffices.Add(office);
        }

        // Update country with all new office IDs
        await _countryRepository.ReplaceOneAsync(country);

        return Ok(new { 
            message = $"Successfully linked {linkedOffices.Count} offices to country", 
            linkedOffices = linkedOffices.Count,
            errors,
            country = country.Name 
        });
    }
}
