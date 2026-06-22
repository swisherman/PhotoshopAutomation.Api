using Microsoft.AspNetCore.Mvc;

namespace UXPPluginDemoApi.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using MongoDB.Driver;
    using UXPPluginDemoApi.Models;
    using UXPPluginDemoApi.Services;

    [ApiController]
    [Route("psds")]
    public class PsdsController : ControllerBase
    {
        private readonly MongoService _mongo;

        public PsdsController(MongoService mongo)
        {
            _mongo = mongo;
        }

        // GET /psds
        [HttpGet]
        public async Task<IActionResult> GetPsds()
        {
            var psds = await _mongo.Psds
                .Find(_ => true)
                .SortByDescending(x => x.Created)
                .ToListAsync();

            return Ok(psds);
        }

        // GET /psds/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPsd(string id)
        {
            var psd = await _mongo.Psds.Find(x => x.Id == id).FirstOrDefaultAsync();

            if (psd == null)
            {
                return NotFound(new { detail = "PSD record not found" });
            }

            return Ok(psd);
        }

        // POST /psds
        [HttpPost]
        public async Task<IActionResult> CreatePsd([FromBody] PSDItem item)
        {
            if (string.IsNullOrWhiteSpace(item.FilePathName))
            {
                return BadRequest("FilePathName is required.");
            }

            if (string.IsNullOrWhiteSpace(item.Created))
            {
                item.Created = DateTime.UtcNow.ToString("O");
            }

            await _mongo.Psds.InsertOneAsync(item);

            return Ok(new
            {
                inserted_id = item.Id,
                created = true
            });
        }

        // PUT /psds/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdatePsd(string id, [FromBody] PSDItem item)
        {
            item.Id = id;

            var result = await _mongo.Psds.ReplaceOneAsync(
                x => x.Id == id,
                item
            );

            if (result.MatchedCount == 0)
            {
                return NotFound(new { detail = "PSD record not found" });
            }

            return Ok(new
            {
                updated = true
            });
        }

        // DELETE /psds/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePsd(string id)
        {
            var result = await _mongo.Psds.DeleteOneAsync(x => x.Id == id);

            if (result.DeletedCount == 0)
            {
                return NotFound(new { detail = "PSD record not found" });
            }

            return Ok(new
            {
                deleted = true
            });
        }
    }
}
