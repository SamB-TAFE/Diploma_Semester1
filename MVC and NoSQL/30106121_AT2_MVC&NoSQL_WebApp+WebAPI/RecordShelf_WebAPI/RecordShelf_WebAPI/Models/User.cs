using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;

namespace RecordShelf_WebAPI.Models
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? UserId { get; set; }

        [BsonElement("Name")]
        [JsonPropertyName("username")]
        public string Username { get; set; } = null!;

        public string Email { get; set; } = null!;

        [JsonIgnore]
        public string PasswordHash { get; set; } = null!;

    }
}
