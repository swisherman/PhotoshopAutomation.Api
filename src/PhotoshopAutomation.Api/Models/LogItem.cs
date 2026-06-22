namespace UXPPluginDemoApi.Models
{
    using MongoDB.Bson;
    using MongoDB.Bson.Serialization.Attributes;

    public class LogItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("Description")]
        public string Description { get; set; } = "";

        [BsonElement("Created")]
        public string Created { get; set; } = DateTime.UtcNow.ToString("O");
    }
}
