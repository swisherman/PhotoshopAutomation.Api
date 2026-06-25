using PhotoshopAutomationApi.Models;
using MongoDB.Driver;
using MockupWorkflow.Shared.Models;
namespace PhotoshopAutomationApi.Services
{
   
public class MongoService
    {
        //public IMongoCollection<PodItem> Records { get; }
        public IMongoCollection<LogItem> Logs { get; }
        public IMongoCollection<PSDItem> Psds { get; }

        public MongoService(IConfiguration config)
        {
            var client = new MongoClient(config["MongoSettings:ConnectionString"]);
            var database = client.GetDatabase(config["MongoSettings:DatabaseName"]);

            //Records = database.GetCollection<PodItem>(config["MongoSettings:RecordsCollection"]);
            Logs = database.GetCollection<LogItem>(config["MongoSettings:LogsCollection"]);
            Psds = database.GetCollection<PSDItem>(config["MongoSettings:PsdsCollection"]);
        }
    }
}
