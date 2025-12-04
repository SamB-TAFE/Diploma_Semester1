using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace RecordShelf_WebAPI.Models
{
    public class Analytics
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? AnalyticsId { get; set; }

        public string UserId { get; set; } = null!;

        public string AudioId { get; set; } = null!;

        public int ListenCount { get; set; }

        public int LikeCount { get; set; } 
    }
}
