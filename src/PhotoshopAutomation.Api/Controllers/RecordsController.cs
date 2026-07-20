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
            foreach (var record in records)
            {
                if (record.Id == id)
                {
                    record.MockupProcessed = true;
                    record.ProcessingStatus = "processed";
                }
            }
            if (records.Count == 0)
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
        [HttpGet("batches/{batchId:long}")]
        public async Task<IActionResult> GetBatchItems(long batchId, [FromQuery] string? productType = null)
        {
            var records = await _podCollection.GetAllRecordsAsync();
            var batchIdText = batchId.ToString();
            var items = records
                .Where(x => x.BatchId == batchIdText)
                .Where(x => string.IsNullOrWhiteSpace(productType) || x.ProductType == productType)
                .OrderBy(x => x.Phrase)
                .ToList();

            return Ok(items);
        }

        [HttpPost("batches/{batchId}/process-mockups")]
        public async Task<IActionResult> ProcessBatchMockups(
    long batchId,
    [FromQuery] string? productType = null)
        {
            var batchIdText = batchId.ToString();
            var processed = await _mockupGenerationService.GenerateBatchAsync(batchIdText, productType);

            if (processed == 0)
                return NotFound($"No items found for batch {batchId}.");

            return Ok(new
            {
                batchId,
                productType,
                processed
            });
        }
        [HttpGet("batches/{batchId:long}/ready")]
        public async Task<IActionResult> GetReadyRecordsForBatch(
    long batchId,
    [FromQuery] string? productType = null)
        {
            var batchIdText = batchId.ToString();
            var records = await _podCollection.GetAllRecordsAsync();

            var ready = records
                .Where(x => x.BatchId == batchIdText)
                .Where(x => string.IsNullOrWhiteSpace(productType) || x.ProductType == productType)
                .Where(x => !x.MockupProcessed)
                .Where(x => string.Equals(x.ProcessingStatus, "ready", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(x.ProcessingStatus, "processing", StringComparison.OrdinalIgnoreCase))
                .OrderBy(x => x.Phrase)
                .ToList();

            foreach (var record in ready)
            {
                record.IdString = record.Id.ToString();
                if (string.IsNullOrWhiteSpace(record.InputFolderPath))
                    continue;

                if (!Directory.Exists(record.InputFolderPath))
                    continue;

                var imageFile = Directory
                    .EnumerateFiles(record.InputFolderPath, "*.png")
                    .OrderBy(Path.GetFileName)
                    .FirstOrDefault();

                if (imageFile != null)
                {
                    record.Filename = Path.GetFileName(imageFile);
                }

            }

            return Ok(ready);
        }
        [HttpGet("batches/pending")]
        public async Task<IActionResult> GetPendingBatches()
        {
            var records = await _podCollection.GetAllRecordsAsync();

            var batches = records
                .Where(x => string.Equals(
                    x.ProcessingStatus,
                    "processing",
                    StringComparison.OrdinalIgnoreCase))
                .Where(x => !x.MockupProcessed)
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
        [HttpPost("{id}/mockup-complete")]
        public async Task<IActionResult> MarkMockupComplete(string id)
        {
            if (!ObjectId.TryParse(id, out var objectId))
            {
                return BadRequest($"Invalid MongoDB ObjectId: {id}");
            }

            var completedAt = DateTime.UtcNow;

            var update = Builders<PodItem>.Update
                .Set(x => x.MockupProcessed, true)
                .Set(x => x.ProcessingStatus, "processed")
                .Set(x => x.MockupProcessedAt, completedAt)
                .Set(x => x.MockupError, string.Empty)
                .Set(x => x.LastModified, completedAt);

            var updated = await _podCollection.UpdateOneAsync(
                x => x.Id == objectId,
                update
            );

            if (!updated)
            {
                return NotFound();
            }

            return Ok(new
            {
                Id = id,
                ProcessingStatus = "processed",
                MockupProcessed = true,
                MockupProcessedAt = completedAt
            });
        }

        [HttpPost("{id}/mockup-failed")]
        public async Task<IActionResult> MarkMockupFailed(
            string id,
            [FromBody] MockupFailureRequest request)
        {
            if (!ObjectId.TryParse(id, out var objectId))
            {
                return BadRequest($"Invalid MongoDB ObjectId: {id}");
            }

            if (request == null || string.IsNullOrWhiteSpace(request.Error))
            {
                return BadRequest("An error message is required.");
            }

            var failedAt = DateTime.UtcNow;

            var update = Builders<PodItem>.Update
                .Set(x => x.MockupProcessed, false)
                .Set(x => x.ProcessingStatus, "failed")
                .Set(x => x.MockupError, request.Error)
                .Set(x => x.LastModified, failedAt);

            var updated = await _podCollection.UpdateOneAsync(
                x => x.Id == objectId,
                update
            );

            if (!updated)
            {
                return NotFound();
            }

            return Ok(new
            {
                Id = id,
                ProcessingStatus = "failed",
                MockupProcessed = false,
                MockupError = request.Error
            });
        }
        [HttpPost("batches/{batchId}/retry-failed")]
        public async Task<IActionResult> RetryFailedBatch(string batchId)
        {
            if (string.IsNullOrWhiteSpace(batchId))
            {
                return BadRequest("Batch ID is required.");
            }

            var records = await _podCollection.GetAllRecordsAsync();

            var failedRecords = records
                .Where(x => string.Equals(
                    x.BatchId,
                    batchId,
                    StringComparison.OrdinalIgnoreCase))
                .Where(x => string.Equals(
                    x.ProcessingStatus,
                    "failed",
                    StringComparison.OrdinalIgnoreCase))
                .Where(x => !x.MockupProcessed)
                .ToList();

            if (failedRecords.Count == 0)
            {
                return Ok(new
                {
                    BatchId = batchId,
                    RetriedCount = 0,
                    Message = "No failed records were found."
                });
            }

            var retriedAt = DateTime.UtcNow;
            var retriedCount = 0;

            foreach (var record in failedRecords)
            {
                var update = Builders<PodItem>.Update
                    .Set(x => x.ProcessingStatus, "processing")
                    .Set(x => x.MockupProcessed, false)
                    .Set(x => x.MockupProcessedAt, null)
                    .Set(x => x.MockupError, string.Empty)
                    .Set(x => x.LastModified, retriedAt);

                var updated = await _podCollection.UpdateOneAsync(
                    x => x.Id == record.Id,
                    update
                );

                if (updated)
                {
                    retriedCount++;
                }
            }

            return Ok(new
            {
                BatchId = batchId,
                RetriedCount = retriedCount
            });
        }
    }
}
