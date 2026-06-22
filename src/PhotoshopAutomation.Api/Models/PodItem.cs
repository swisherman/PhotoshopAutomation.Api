using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace UXPPluginDemoApi.Models
{
    [BsonIgnoreExtraElements]
    public class PodItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        public string Phrase { get; set; } = "";

        public string PrintifyFilename { get; set; } = "";
        public string ImagePrompt { get; set; } = "";
        public string FolderName { get; set; } = "";

        public string ExpectedFolderName { get; set; } = "";

        public string Filename { get; set; } = "";

        public string ProcessingStatus { get; set; } = "ready";

        public bool MockupProcessed { get; set; } = false;
        public DateTime DateTime { get; set; } = System.DateTime.Now;

        public List<string> Tags { get; set; } = [];

        public string EtsyTitle { get; set; } = String.Empty;

    }
}
