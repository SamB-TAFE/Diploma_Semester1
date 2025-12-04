using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace RecordShelf_WebAPI.Models
{
    public class Audio
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? AudioId { get; set; }

        [BsonElement("Name")]
        public string AudioTitle { get; set; } = null!;

        public string UserId { get; set; } = null!;

        public string Artist { get; set; } = null!;

        public List<string> Tags { get; set; } = new();

        public int DurationSeconds { get; set; }

        public string FilePath { get; set; } = null!;

        public DateTime UploadDate { get; set; } = DateTime.Now;
    }
}
