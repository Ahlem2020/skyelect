using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using MongoDB.Driver;
using MongoDB.Bson; // Ajout de l'import n�cessaire
using ElectionApi.QueueModels.Votes;
using ElectionApi.QueueServices;
using ElectionApi.Repositories;
using ElectionApi.Services;
using ElectionApi.Models;

namespace ElectionApi.Controllers;

[ApiController]
[Route("candidates")]
public class CandidateController : ControllerBase
{

    private readonly ILogger<CandidateController> _logger;
    private readonly ICandidateRepository _candidateRepository;
    private readonly IVoteRepository _voteRepository;

    public CandidateController(ILogger<CandidateController> logger, IDataService ds)
    {
        _logger = logger;
        _candidateRepository = ds.Candidates;
        _voteRepository = ds.Votes;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var filter = Builders<Vote>.Filter;
        var candidates = _candidateRepository.AsQueryable();
        var results = await _voteRepository.CountByCandidateAsync(filter.Empty);
        var response = candidates.Select(x => new
        {
            Id = x.OriginalId,
            LastName = x.LastName,
            FirstName = x.FirstName,
            Part = x.Part,
            Image = x.Image,
            TotalVote = results.Where(z => (int)z["_id"]["candidateId"] == x.OriginalId).Sum(z => (long)z["totalVote"])
        });
        return Ok(response);
    }

    [HttpGet("stats/perHours")]
    public async Task<IActionResult> PerHours()
    {
        var filter = Builders<Vote>.Filter;
        var voteFilter = filter.In(x => x.CandidateId, new List<int>() { 1, 3, 2 });
        var candidates = await _candidateRepository.FilterByAsync( x=> x.OriginalId == 1 || x.OriginalId == 3 || x.OriginalId == 2 );
        var results = await _voteRepository.CountByCandidateByHoureAsync(voteFilter);
        var hours = EachHours(DateTime.Now, 13);
        var response = hours.Select(x => new
        {
            Time = x.ToString("htt").ToLower(),
            Data = candidates.Select(z => new
            {
                Id = z.OriginalId,
                FullName= z.FirstName+" "+z.LastName,
                TotalVote = (long)(results
                    .Where(w => (int)w["_id"]["candidateId"] == z.OriginalId && (string)w["_id"]["date"] == x.ToString("yyyy-MM-dd HH"))
                    .Sum(w => w["totalVote"] is BsonInt32 ? 
                        ((BsonInt32)w["totalVote"]).Value : 
                        ((BsonInt64)w["totalVote"]).Value))
            })
        });
        return Ok(response);    
    }

    private IEnumerable<DateTime> EachHours(DateTime from, int hours)
    {
        foreach (var hour in Enumerable.Range(0, Math.Abs(hours)))
        {
            yield return from.AddHours(-hour);
        }
    }


    [HttpGet("stats/perMinutes")]
    public async Task<IActionResult> PerMinutes()
    {
        var filter = Builders<Vote>.Filter;
        var voteFilter = filter.In(x => x.CandidateId, new List<int>() { 1, 3, 2 });

        var candidates = await _candidateRepository.FilterByAsync(
            x => x.OriginalId == 1 || x.OriginalId == 3 || x.OriginalId == 2
        );

        var results = await _voteRepository.CountByCandidateByMinuteAsync(voteFilter);

        var minutes = EachMinutes(DateTime.Now, 60); // Generate last 60 minutes

        var response = minutes.Select(minute => new
        {
            Time = minute.ToString("h:mmtt").ToLower(), // e.g., 2:45pm
            Data = candidates.Select(candidate => new
            {
                Id = candidate.OriginalId,
                FullName = candidate.FirstName + " " + candidate.LastName,
                TotalVote = (long)(results
                    .Where(w =>
                        (int)w["_id"]["candidateId"] == candidate.OriginalId &&
                        (string)w["_id"]["date"] == minute.ToString("yyyy-MM-dd HH:mm"))
                    .Sum(w => w["totalVote"] is BsonInt32 ?
                        ((BsonInt32)w["totalVote"]).Value :
                        ((BsonInt64)w["totalVote"]).Value))
            })
        });

        return Ok(response);
    }
    private List<DateTime> EachMinutes(DateTime now, int range)
    {
        var list = new List<DateTime>();
        for (int i = range - 1; i >= 0; i--)
        {
            list.Add(now.AddMinutes(-i).AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond));
        }
        return list;
    }

}
