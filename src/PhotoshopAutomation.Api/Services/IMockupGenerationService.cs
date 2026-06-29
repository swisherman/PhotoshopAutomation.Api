namespace PhotoshopAutomation.Api.Services;

public interface IMockupGenerationService
{
    Task<int> GenerateBatchAsync(string batchId, string? productType = null);
}