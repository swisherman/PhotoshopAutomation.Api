using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;


namespace PhotoshopAutomationApi.Models
{
    public class PSDItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("WorkflowStep")]
        public int WorkflowStep { get; set; }

        [BsonElement("ProductType")]
        public string? ProductType { get; set; } = null;

        [BsonElement("TemplateKey")]
        public string? TemplateKey { get; set; } = null;

        [BsonElement("FilePathName")]
        public string FilePathName { get; set; } = "";

        [BsonElement("Description")]
        public string Description { get; set; } = "";

        [BsonElement("PixelWidth")]
        public int PixelWidth { get; set; } = 0;

        [BsonElement("PixelHeight")]
        public int PixelHeight { get; set; } = 0;

        [BsonElement("Created")]
        public string? Created { get; set; } = null;
    }
}
