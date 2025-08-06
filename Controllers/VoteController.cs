using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ElectionApi.QueueModels.Votes;
using ElectionApi.QueueServices;
using ElectionApi.Repositories;
using ElectionApi.Services;
using ElectionApi.Models;
using ElectionApi.ViewModels;

namespace ElectionApi.Controllers;

[ApiController]
[Route("votes")]
public class VoteController : ControllerBase
{

    private readonly ILogger<VoteController> _logger;
    private readonly IVoteRepository _voteRepository;


    public VoteController(ILogger<VoteController> logger, IDataService ds)
    {
        _logger = logger;
        _voteRepository = ds.Votes;
    }

    [HttpPost]
    public async Task<IActionResult> Post([FromBody] AddVoteViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return UnprocessableEntity(ModelState);
        }
        if (string.IsNullOrWhiteSpace(model.Password) || !model.Password.Equals("gvkzjfau114631"))
        {
            ModelState.AddModelError("errors", "The Password filed is not correct.");
            return UnprocessableEntity(ModelState);
        }
        if (model.Candidates != null && model.Candidates.Count > 0)
        {
            foreach (var candidate in model.Candidates)
            {
                await _voteRepository.InsertOneAsync(new Vote()
                {
                    OfficeId = model.Office,
                    CandidateId = candidate.Id,
                    TotalVote = candidate.Vote ?? 0
                });
            }
        }

        return Ok();
    }

}
