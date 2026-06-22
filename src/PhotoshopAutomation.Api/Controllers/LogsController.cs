using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using UXPPluginDemoApi.Models;
using UXPPluginDemoApi.Services;

namespace UXPPluginDemoApi.Controllers
{
  

    [ApiController]
    [Route("logs")]
    public class LogsController : ControllerBase
    {
        private readonly MongoService _mongo;

        public LogsController(MongoService mongo)
        {
            _mongo = mongo;
        }

        [HttpPost]
        public async Task<IActionResult> CreateLog([FromBody] LogItem item)
        {
            if (string.IsNullOrWhiteSpace(item.Description))
            {
                return BadRequest("Description is required.");
            }

            if (string.IsNullOrWhiteSpace(item.Created))
            {
                item.Created = DateTime.UtcNow.ToString("O");
            }

            await _mongo.Logs.InsertOneAsync(item);

            return Ok(new
            {
                inserted_id = item.Id,
                logged = true
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetLogs()
        {
            var logs = await _mongo.Logs
                .Find(_ => true)
                .SortByDescending(x => x.Created)
                .Limit(100)
                .ToListAsync();

            return Ok(logs);
        }
    }
}
