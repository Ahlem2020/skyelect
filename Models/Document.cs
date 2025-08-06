using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ElectionApi.Models
{
    public interface IDocument
    {
        public string? Id { get; }
        DateTime CreatedAt { get; }
        DateTime UpdatedAt { get; }
    }

    public class Document : IDocument
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime CreatedAt { get; set; }
        [BsonDateTimeOptions(Kind = DateTimeKind.Local)]
        public DateTime UpdatedAt { get; set; }

        public Document()
        {
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
