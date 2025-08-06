using ElectionApi.QueueModels.Votes;
using ElectionApi.QueueServices;
using ElectionApi.Repositories;
using ElectionApi.Services;
using ElectionApi.Models;

namespace ElectionApi.Workers
{
    public class AddSMSVoteWorker : BackgroundService
    {
        private readonly IBackgroundItemQueue<AddSMSVoteQueueModel> _queue;
        private readonly IVoteRepository _voteRepository;
        private readonly ILogger<AddSMSVoteWorker> _logger;

        public AddSMSVoteWorker(IBackgroundItemQueue<AddSMSVoteQueueModel> queue, IDataService ds, ILogger<AddSMSVoteWorker> logger)
        {
            _queue = queue;
            _voteRepository = ds.Votes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await BackgroundProcessing(stoppingToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogError(string.Format("The {0} is stopping due to a host shutdown, queued items might not be processed anymore.", nameof(AddSMSVoteWorker)));
            return base.StopAsync(cancellationToken);
        }

        private async Task BackgroundProcessing(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(200, stoppingToken);

                    var model = _queue.Dequeue();

                    if (model == null)
                        continue;
                    if (string.IsNullOrWhiteSpace(model.Text))
                        continue;
                    var textLines = model.Text.Split("\n").ToList();
                    if (textLines.Count == 0)
                        continue;
                    if (!textLines.First().ToLower().Contains("jap2k23"))
                        continue;
                    textLines.RemoveAt(0);
                    if (textLines.Count == 0)
                        continue;
                    var officeId = int.Parse(textLines.First());
                    var countVotes = await _voteRepository.CountAsync(x => x.OfficeId.Equals(officeId));
                    if (countVotes > 0)
                        continue;
                    textLines.RemoveAt(0);
                    foreach (var line in textLines)
                    {
                        var elements = line.Split(" ").ToList();
                        if (elements.Count != 2)
                            continue;
                        var candidateId = int.Parse(elements[0]);
                        var totalVotes = long.Parse(elements[1]);
                        await _voteRepository.InsertOneAsync(new Vote()
                        {
                            OfficeId = officeId,
                            CandidateId = candidateId,
                            TotalVote = totalVotes,
                            From = model.From
                        });   
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogCritical(DateTime.Now.ToString() + ": Exception " + ex.Message + " " + ex.StackTrace);
                }
            }
        }
    }
}
