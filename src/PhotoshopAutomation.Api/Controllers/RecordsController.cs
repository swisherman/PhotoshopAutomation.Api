using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Bson;
using PhotoshopAutomationApi.Models;
using PhotoshopAutomationApi.Services;
using MockupWorkflow.Shared.Models;

namespace PhotoshopAutomationApi.Controllers
{
    [ApiController]
    [Route("records")]
    public class RecordsController : ControllerBase
    {
        private readonly MongoService _mongo;
        private readonly PodCollection _podCollection;
        public RecordsController(MongoService mongo, PodCollection podCollection)
        {
           _mongo = mongo;

            _podCollection = podCollection;
            var records = _podCollection.GetAllRecordsAsync().Result;
        }

        [HttpGet]
        public async Task<IActionResult> GetRecords()
        {
            var records = await _podCollection.GetAllRecordsAsync();
            return Ok(records);
        }

        [HttpGet("ready")]
        public async Task<IActionResult> GetReadyRecords()
        {

            var records = await _podCollection.GetAllRecordsAsync();//.FindAsync(x => x.MockupProcessed != true && x.ProcessingStatus != "processed");
            records = records.Where(x => x.MockupProcessed != true && x.ProcessingStatus != "processed").ToList();  
            return Ok(records);
        }

        [HttpPatch("{id}/processed")]
        public async Task<IActionResult> MarkProcessed(ObjectId id)
        {
            
            var records = await _podCollection.GetAllRecordsAsync();
            foreach (var record in records) {
                if (record.Id == id)
                {
                    record.MockupProcessed = true;
                    record.ProcessingStatus = "processed";
                }
            }
            if (records.Count== 0)
                return NotFound();

            return Ok(new { processed = true });
        }
        
        [HttpPost("import")]
        public async Task<IActionResult> Import([FromBody] List<PodItem> items)
        {
            if (items == null || items.Count == 0)
                return BadRequest("No records supplied.");

            var inserted = 0;
            var alreadyPresent = 0;
            var exist= false;
            foreach (var item in items)
            {
                             
                var records = await _podCollection.GetAllRecordsAsync();
                foreach(var record in records)
                { 
                    if(record.SourceKey == item.SourceKey)
                    {
                        alreadyPresent++;
                        exist = true;
                        continue;
                    }
                }
                if(exist==false)
                {
                    item.ProcessingStatus = "ready";
                    item.MockupProcessed = false;

                    await _podCollection.AddDocument(item);
                    inserted++;
                }
                
            }

            return Ok(new
            {
                received = items.Count,
                inserted,
                alreadyPresent
            });
        }
    }
}
