using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using UXPPluginDemoApi.Models;
using UXPPluginDemoApi.Services;

namespace UXPPluginDemoApi.Controllers
{
    [ApiController]
    [Route("records")]
    public class RecordsController : ControllerBase
    {
        private readonly MongoService _mongo;

        public RecordsController(MongoService mongo)
        {
            _mongo = mongo;
        }

        [HttpGet]
        public async Task<IActionResult> GetRecords()
        {
            var records = await _mongo.Records.Find(_ => true).ToListAsync();
            return Ok(records);
        }

        [HttpGet("ready")]
        public async Task<IActionResult> GetReadyRecords()
        {
            var filter = Builders<PodItem>.Filter.Ne(x => x.MockupProcessed, true) &
                         Builders<PodItem>.Filter.Ne(x => x.ProcessingStatus, "processed");

            var records = await _mongo.Records.Find(filter).ToListAsync();

            return Ok(records);
        }

        [HttpPatch("{id}/processed")]
        public async Task<IActionResult> MarkProcessed(string id)
        {
            var update = Builders<PodItem>.Update
                .Set(x => x.MockupProcessed, true)
                .Set(x => x.ProcessingStatus, "processed");

            var result = await _mongo.Records.UpdateOneAsync(x => x.Id == id, update);

            if (result.MatchedCount == 0)
                return NotFound();

            return Ok(new { processed = true });
        }
    }
}
