using Microsoft.AspNetCore.Mvc;
using MockupWorkflow.Shared.Models;
using MongoDB.Bson;
using MongoDB.Driver;
using PhotoshopAutomation.Api.Services;
using PhotoshopAutomationApi.Models;
using PhotoshopAutomationApi.Services;
using System.Net.Http.Json;
using static System.Net.WebRequestMethods;

namespace PhotoshopAutomationApi.Controllers
{
    [ApiController]
    [Route("records")]
    public class RecordsController : ControllerBase
    {
        private readonly MongoService _mongo;
        private readonly PodCollection _podCollection;
        private readonly IMockupGenerationService _mockupGenerationService;
        public RecordsController(
     MongoService mongo,
     PodCollection podCollection,
     IMockupGenerationService mockupGenerationService)
        {
            _mongo = mongo;
            _podCollection = podCollection;
            _mockupGenerationService = mockupGenerationService;
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

            var existingRecords = await _podCollection.GetAllRecordsAsync();

            var existingSourceKeys = existingRecords
                .Where(x => !string.IsNullOrWhiteSpace(x.SourceKey))
                .Select(x => x.SourceKey)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var inserted = 0;
            var alreadyPresent = 0;

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.SourceKey))
                    continue;

                if (existingSourceKeys.Contains(item.SourceKey))
                {
                    alreadyPresent++;
                    continue;
                }

                item.ProcessingStatus = "ready";
                item.MockupProcessed = false;

                await _podCollection.AddDocument(item);

                existingSourceKeys.Add(item.SourceKey);
                inserted++;
            }

            return Ok(new
            {
                received = items.Count,
                inserted,
                alreadyPresent
            });
        }










        [HttpGet("batches")]
        public async Task<IActionResult> GetBatches()
        {
            var records = await _podCollection.GetAllRecordsAsync();

            var batches = records
                .Where(x => !string.IsNullOrWhiteSpace(x.BatchId))
                .GroupBy(x => new
                {
                    x.BatchId,
                    x.ProductType
                })
                .Select(g => new BatchSummary
                {
                    BatchId = g.Key.BatchId,
                    ProductType = g.Key.ProductType,
                    ItemCount = g.Count(),
                    MockupProcessedCount = g.Count(x => x.MockupProcessed),
                    LastModified = g.Max(x => x.LastModified)
                })
                .OrderByDescending(x => x.LastModified)
                .ToList();

            return Ok(batches);
        }
        [HttpGet("batches/{batchId}")]
        public async Task<IActionResult> GetBatchItems(string batchId, [FromQuery] string? productType = null)
        {
            var records = await _podCollection.GetAllRecordsAsync();

            var items = records
                .Where(x => x.BatchId == batchId)
                .Where(x => string.IsNullOrWhiteSpace(productType) || x.ProductType == productType)
                .OrderBy(x => x.Phrase)
                .ToList();

            return Ok(items);
        }

        [HttpPost("batches/{batchId}/process-mockups")]
        public async Task<IActionResult> ProcessBatchMockups(
    string batchId,
    [FromQuery] string? productType = null)
        {
            var processed = await _mockupGenerationService.GenerateBatchAsync(batchId, productType);

            if (processed == 0)
                return NotFound($"No items found for batch {batchId}.");

            return Ok(new
            {
                batchId,
                productType,
                processed
            });
        }
        [HttpGet("batches/{batchId}/ready")]
        public async Task<IActionResult> GetReadyRecordsForBatch(
    string batchId,
    [FromQuery] string? productType = null)
        {
            var records = await _podCollection.GetAllRecordsAsync();

            var ready = records
                .Where(x => x.BatchId == batchId)
                .Where(x => string.IsNullOrWhiteSpace(productType) || x.ProductType == productType)
                .Where(x => !x.MockupProcessed)
                .Where(x => string.Equals(x.ProcessingStatus, "ready", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(x.ProcessingStatus, "processing", StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Phrase)
                .ToList();

            return Ok(ready);
        }
    }
}
