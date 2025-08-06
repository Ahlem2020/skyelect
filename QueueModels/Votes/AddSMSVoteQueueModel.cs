namespace ElectionApi.QueueModels.Votes
{
    public class AddSMSVoteQueueModel
    {
        public string? From { get; set; }
        public string? Text { get; set; }
        public long SentStamp { get; set; }
        public long ReceivedStamp { get; set; }
    }
}