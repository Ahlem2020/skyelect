using ElectionApi.Models;
using ElectionApi.Repositories;
using ElectionApi.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ElectionApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "superadmin")]
    public class SuperAdminController : ControllerBase
    {
        private readonly ICandidateRepository _candidateRepository;
        private readonly ICountryRepository _countryRepository;
        private readonly IOfficeRepository _officeRepository;
        private readonly IVoteRepository _voteRepository;
        private readonly IUserRepository _userRepository;

        public SuperAdminController(
            ICandidateRepository candidateRepository,
            ICountryRepository countryRepository,
            IOfficeRepository officeRepository,
            IVoteRepository voteRepository,
            IUserRepository userRepository)
        {
            _candidateRepository = candidateRepository;
            _countryRepository = countryRepository;
            _officeRepository = officeRepository;
            _voteRepository = voteRepository;
            _userRepository = userRepository;
        }

        #region Candidate CRUD Operations

        [HttpGet("candidates")]
        public async Task<ActionResult<IEnumerable<Candidate>>> GetAllCandidates()
        {
            var candidates = await _candidateRepository.GetAllAsync();
            return Ok(candidates);
        }

        [HttpGet("candidates/{id}")]
        public async Task<ActionResult<Candidate>> GetCandidate(string id)
        {
            var candidate = await _candidateRepository.GetByIdAsync(id);
            if (candidate == null)
            {
                return NotFound();
            }
            return Ok(candidate);
        }

        [HttpPost("candidates")]
        public async Task<ActionResult<Candidate>> CreateCandidate(AddCandidateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var candidate = new Candidate
            {
                OriginalId = model.OriginalId,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Part = model.Part,
                Country = model.Country,
                Parti = model.Parti,
                Image = model.Image
            };

            await _candidateRepository.CreateAsync(candidate);
            return CreatedAtAction(nameof(GetCandidate), new { id = candidate.Id }, candidate);
        }

        [HttpPut("candidates/{id}")]
        public async Task<IActionResult> UpdateCandidate(string id, UpdateCandidateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var candidate = await _candidateRepository.GetByIdAsync(id);
            if (candidate == null)
            {
                return NotFound();
            }

            candidate.OriginalId = model.OriginalId;
            candidate.FirstName = model.FirstName;
            candidate.LastName = model.LastName;
            candidate.Part = model.Part;
            candidate.Country = model.Country;
            candidate.Parti = model.Parti;
            candidate.Image = model.Image;

            await _candidateRepository.UpdateAsync(candidate);
            return NoContent();
        }

        [HttpDelete("candidates/{id}")]
        public async Task<IActionResult> DeleteCandidate(string id)
        {
            var candidate = await _candidateRepository.GetByIdAsync(id);
            if (candidate == null)
            {
                return NotFound();
            }

            await _candidateRepository.DeleteAsync(id);
            return NoContent();
        }

        #endregion

        #region Country CRUD Operations

        [HttpGet("countries")]
        public async Task<ActionResult<IEnumerable<Country>>> GetAllCountries()
        {
            var countries = await _countryRepository.GetAllAsync();
            return Ok(countries);
        }

        [HttpGet("countries/{id}")]
        public async Task<ActionResult<Country>> GetCountry(string id)
        {
            var country = await _countryRepository.GetByIdAsync(id);
            if (country == null)
            {
                return NotFound();
            }
            return Ok(country);
        }

        [HttpPost("countries")]
        public async Task<ActionResult<Country>> CreateCountry(AddCountryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var existingCountry = await _countryRepository.GetByNameAsync(model.Name);
            if (existingCountry != null)
            {
                return BadRequest("Country with this name already exists");
            }

            var country = new Country
            {
                Name = model.Name,
                Flag = model.Flag,
                Offices = model.Offices,
                Candidates = model.Candidates
            };

            await _countryRepository.CreateAsync(country);
            return CreatedAtAction(nameof(GetCountry), new { id = country.Id }, country);
        }

        [HttpPut("countries/{id}")]
        public async Task<IActionResult> UpdateCountry(string id, UpdateCountryViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var country = await _countryRepository.GetByIdAsync(id);
            if (country == null)
            {
                return NotFound();
            }

            var existingCountry = await _countryRepository.GetByNameAsync(model.Name);
            if (existingCountry != null && existingCountry.Id != id)
            {
                return BadRequest("Country with this name already exists");
            }

            country.Name = model.Name;
            country.Flag = model.Flag;
            country.Offices = model.Offices;
            country.Candidates = model.Candidates;

            await _countryRepository.UpdateAsync(country);
            return NoContent();
        }

        [HttpDelete("countries/{id}")]
        public async Task<IActionResult> DeleteCountry(string id)
        {
            var country = await _countryRepository.GetByIdAsync(id);
            if (country == null)
            {
                return NotFound();
            }

            await _countryRepository.DeleteAsync(id);
            return NoContent();
        }

        #endregion

        #region Office CRUD Operations

        [HttpGet("offices")]
        public async Task<ActionResult<IEnumerable<Office>>> GetAllOffices()
        {
            var offices = await _officeRepository.GetAllAsync();
            return Ok(offices);
        }

        [HttpGet("offices/{id}")]
        public async Task<ActionResult<Office>> GetOffice(string id)
        {
            var office = await _officeRepository.GetByIdAsync(id);
            if (office == null)
            {
                return NotFound();
            }
            return Ok(office);
        }

        [HttpPost("offices")]
        public async Task<ActionResult<Office>> CreateOffice(AddOfficeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Country? country = null;
            if (!string.IsNullOrEmpty(model.CountryId))
            {
                country = await _countryRepository.GetByIdAsync(model.CountryId);
                if (country == null)
                {
                    return BadRequest("Country not found");
                }
            }

            var office = new Office
            {
                OriginalId = model.Id,
                Offices = model.Offices ?? 0,
                Code = model.Code,
                Registered = model.Registered,
                Province = model.Province,
                CountryId = model.CountryId,
                CountryName = country?.Name
            };

            await _officeRepository.CreateAsync(office);

            // Update the country's offices list if country is specified
            if (country != null && office.Id != null)
            {
                if (!country.Offices.Contains(office.Id.ToString()))
                {
                    country.Offices.Add(office.Id.ToString());
                    await _countryRepository.UpdateAsync(country);
                }
            }

            return CreatedAtAction(nameof(GetOffice), new { id = office.Id }, office);
        }

        [HttpPut("offices/{id}")]
        public async Task<IActionResult> UpdateOffice(string id, AddOfficeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var office = await _officeRepository.GetByIdAsync(id);
            if (office == null)
            {
                return NotFound();
            }

            // Handle country changes
            Country? newCountry = null;
            if (!string.IsNullOrEmpty(model.CountryId))
            {
                newCountry = await _countryRepository.GetByIdAsync(model.CountryId);
                if (newCountry == null)
                {
                    return BadRequest("Country not found");
                }
            }

            // Remove from previous country if linked to a different one
            if (!string.IsNullOrEmpty(office.CountryId) && office.CountryId != model.CountryId)
            {
                var previousCountry = await _countryRepository.GetByIdAsync(office.CountryId);
                if (previousCountry != null)
                {
                    previousCountry.Offices.Remove(id);
                    await _countryRepository.UpdateAsync(previousCountry);
                }
            }

            office.OriginalId = model.Id;
            office.Offices = model.Offices ?? 0;
            office.Code = model.Code;
            office.Registered = model.Registered;
            office.Province = model.Province;
            office.CountryId = model.CountryId;
            office.CountryName = newCountry?.Name;

            await _officeRepository.UpdateAsync(office);

            // Add to new country if specified
            if (newCountry != null && !string.IsNullOrEmpty(model.CountryId))
            {
                if (!newCountry.Offices.Contains(id))
                {
                    newCountry.Offices.Add(id);
                    await _countryRepository.UpdateAsync(newCountry);
                }
            }

            return NoContent();
        }

        [HttpDelete("offices/{id}")]
        public async Task<IActionResult> DeleteOffice(string id)
        {
            var office = await _officeRepository.GetByIdAsync(id);
            if (office == null)
            {
                return NotFound();
            }

            // Remove from country's offices list if linked
            if (!string.IsNullOrEmpty(office.CountryId))
            {
                var country = await _countryRepository.GetByIdAsync(office.CountryId);
                if (country != null)
                {
                    country.Offices.Remove(id);
                    await _countryRepository.UpdateAsync(country);
                }
            }

            await _officeRepository.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("offices/by-country/{countryId}")]
        public async Task<ActionResult<IEnumerable<Office>>> GetOfficesByCountry(string countryId)
        {
            var country = await _countryRepository.GetByIdAsync(countryId);
            if (country == null)
            {
                return NotFound("Country not found");
            }

            var offices = await _officeRepository.GetAllAsync();
            var countryOffices = offices.Where(o => o.CountryId == countryId);
            
            return Ok(countryOffices);
        }

        [HttpPost("offices/{officeId}/link-country/{countryId}")]
        public async Task<IActionResult> LinkOfficeToCountry(string officeId, string countryId)
        {
            var office = await _officeRepository.GetByIdAsync(officeId);
            if (office == null)
            {
                return NotFound("Office not found");
            }

            var country = await _countryRepository.GetByIdAsync(countryId);
            if (country == null)
            {
                return NotFound("Country not found");
            }

            // Remove from previous country if linked
            if (!string.IsNullOrEmpty(office.CountryId) && office.CountryId != countryId)
            {
                var previousCountry = await _countryRepository.GetByIdAsync(office.CountryId);
                if (previousCountry != null)
                {
                    previousCountry.Offices.Remove(officeId);
                    await _countryRepository.UpdateAsync(previousCountry);
                }
            }

            // Update office
            office.CountryId = countryId;
            office.CountryName = country.Name;
            await _officeRepository.UpdateAsync(office);

            // Update country
            if (!country.Offices.Contains(officeId))
            {
                country.Offices.Add(officeId);
                await _countryRepository.UpdateAsync(country);
            }

            return Ok(new { message = "Office successfully linked to country", office, country = country.Name });
        }

        [HttpPost("offices/{officeId}/unlink-country")]
        public async Task<IActionResult> UnlinkOfficeFromCountry(string officeId)
        {
            var office = await _officeRepository.GetByIdAsync(officeId);
            if (office == null)
            {
                return NotFound("Office not found");
            }

            if (!string.IsNullOrEmpty(office.CountryId))
            {
                var country = await _countryRepository.GetByIdAsync(office.CountryId);
                if (country != null)
                {
                    country.Offices.Remove(officeId);
                    await _countryRepository.UpdateAsync(country);
                }

                office.CountryId = null;
                office.CountryName = null;
                await _officeRepository.UpdateAsync(office);
            }

            return Ok(new { message = "Office successfully unlinked from country", office });
        }

        [HttpPost("offices/link-multiple-to-country/{countryId}")]
        public async Task<IActionResult> LinkMultipleOfficesToCountry(string countryId, [FromBody] List<string> officeIds)
        {
            var country = await _countryRepository.GetByIdAsync(countryId);
            if (country == null)
            {
                return NotFound("Country not found");
            }

            var linkedOffices = new List<Office>();
            var errors = new List<string>();

            foreach (var officeId in officeIds)
            {
                var office = await _officeRepository.GetByIdAsync(officeId);
                if (office == null)
                {
                    errors.Add($"Office with ID {officeId} not found");
                    continue;
                }

                // Remove from previous country if linked
                if (!string.IsNullOrEmpty(office.CountryId) && office.CountryId != countryId)
                {
                    var previousCountry = await _countryRepository.GetByIdAsync(office.CountryId);
                    if (previousCountry != null)
                    {
                        previousCountry.Offices.Remove(officeId);
                        await _countryRepository.UpdateAsync(previousCountry);
                    }
                }

                // Update office
                office.CountryId = countryId;
                office.CountryName = country.Name;
                await _officeRepository.UpdateAsync(office);

                // Add to country's offices list
                if (!country.Offices.Contains(officeId))
                {
                    country.Offices.Add(officeId);
                }

                linkedOffices.Add(office);
            }

            // Update country with all new office IDs
            await _countryRepository.UpdateAsync(country);

            return Ok(new { 
                message = $"Successfully linked {linkedOffices.Count} offices to country", 
                linkedOffices = linkedOffices.Count,
                errors,
                country = country.Name 
            });
        }

        #endregion

        #region Vote CRUD Operations

        [HttpGet("votes")]
        public async Task<ActionResult<IEnumerable<Vote>>> GetAllVotes()
        {
            var votes = await _voteRepository.GetAllAsync();
            return Ok(votes);
        }

        [HttpGet("votes/{id}")]
        public async Task<ActionResult<Vote>> GetVote(string id)
        {
            var vote = await _voteRepository.GetByIdAsync(id);
            if (vote == null)
            {
                return NotFound();
            }
            return Ok(vote);
        }

        [HttpPost("votes")]
        public async Task<ActionResult<Vote>> CreateVote(SuperAdminVoteViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var vote = new Vote
            {
                Password = model.Password,
                Office = model.Office,
                Candidates = model.Candidates?.Select(c => new Vote.CandidateVote 
                { 
                    Id = c.Id, 
                    Vote = c.Vote 
                }).ToList() ?? new List<Vote.CandidateVote>(),
                From = model.From,
                OfficeId = model.OfficeId,
                CandidateId = model.CandidateId,
                TotalVote = model.TotalVote
            };

            await _voteRepository.CreateAsync(vote);
            return CreatedAtAction(nameof(GetVote), new { id = vote.Id }, vote);
        }

        [HttpPut("votes/{id}")]
        public async Task<IActionResult> UpdateVote(string id, SuperAdminVoteViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var vote = await _voteRepository.GetByIdAsync(id);
            if (vote == null)
            {
                return NotFound();
            }

            vote.Password = model.Password;
            vote.Office = model.Office;
            vote.Candidates = model.Candidates?.Select(c => new Vote.CandidateVote 
            { 
                Id = c.Id, 
                Vote = c.Vote 
            }).ToList() ?? new List<Vote.CandidateVote>();
            vote.From = model.From;
            vote.OfficeId = model.OfficeId;
            vote.CandidateId = model.CandidateId;
            vote.TotalVote = model.TotalVote;

            await _voteRepository.UpdateAsync(vote);
            return NoContent();
        }

        [HttpDelete("votes/{id}")]
        public async Task<IActionResult> DeleteVote(string id)
        {
            var vote = await _voteRepository.GetByIdAsync(id);
            if (vote == null)
            {
                return NotFound();
            }

            await _voteRepository.DeleteAsync(id);
            return NoContent();
        }

        #endregion

        #region User CRUD Operations

        [HttpGet("users")]
        public async Task<ActionResult<IEnumerable<User>>> GetAllUsers()
        {
            var users = await _userRepository.GetAllAsync();
            // Remove password hashes from response for security
            var usersResponse = users.Select(u => new
            {
                u.Id,
                u.Username,
                u.Email,
                u.Roles,
                u.IsActive,
                u.CreatedAt,
                u.LastLoginAt
            });
            return Ok(usersResponse);
        }

        [HttpGet("users/{id}")]
        public async Task<ActionResult<User>> GetUser(string id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Remove password hash from response for security
            var userResponse = new
            {
                user.Id,
                user.Username,
                user.Email,
                user.Roles,
                user.IsActive,
                user.CreatedAt,
                user.LastLoginAt
            };

            return Ok(userResponse);
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateUser(string id, UpdateUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Check if email is being changed and if it already exists
            if (user.Email != model.Email && await _userRepository.EmailExistsAsync(model.Email))
            {
                return BadRequest("Email already exists");
            }

            // Check if username is being changed and if it already exists
            if (user.Username != model.Username && await _userRepository.UsernameExistsAsync(model.Username))
            {
                return BadRequest("Username already exists");
            }

            user.Username = model.Username;
            user.Email = model.Email;
            user.Roles = model.Roles;
            user.IsActive = model.IsActive;

            await _userRepository.UpdateAsync(user);
            return NoContent();
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            await _userRepository.DeleteAsync(id);
            return NoContent();
        }

        [HttpPut("users/{id}/activate")]
        public async Task<IActionResult> ActivateUser(string id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = true;
            await _userRepository.UpdateAsync(user);
            return NoContent();
        }

        [HttpPut("users/{id}/deactivate")]
        public async Task<IActionResult> DeactivateUser(string id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.IsActive = false;
            await _userRepository.UpdateAsync(user);
            return NoContent();
        }

        #endregion
    }
}
