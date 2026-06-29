using MockupWorkflow.Shared.Models;

namespace PhotoshopAutomation.Api.Services;

public class MockupGenerationService : IMockupGenerationService
{
    private readonly PodCollection _podCollection;

    public MockupGenerationService(PodCollection podCollection)
    {
        _podCollection = podCollection;
    }

    public async Task<int> GenerateBatchAsync(string batchId, string? productType = null)
    {
        var records = await _podCollection.GetAllRecordsAsync();

        var items = records
            .Where(x => x.BatchId == batchId)
            .Where(x => string.IsNullOrWhiteSpace(productType) || x.ProductType == productType)
            .ToList();

        foreach (var item in items)
        {
            await _podCollection.UpdateDocument(item, nameof(PodItem.ProcessingStatus), "processed");
            await _podCollection.UpdateDocument(item, nameof(PodItem.MockupProcessed), true);
            await _podCollection.UpdateDocument(item, nameof(PodItem.MockupProcessedAt), DateTime.UtcNow);
            await _podCollection.UpdateDocument(item, nameof(PodItem.MockupOutputFolder), item.MockupFolderPath);
        }

        return items.Count;
    }
}