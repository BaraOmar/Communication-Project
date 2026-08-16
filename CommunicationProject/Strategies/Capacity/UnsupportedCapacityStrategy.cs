namespace CommunicationProject.Strategies.Capacity;

public sealed class UnsupportedCapacityStrategy
    : ICapacityStrategy
{
    public bool CanHandle(string? capacity)
    {
        return true;
    }

    public Task<CapacityProcessingResult> ProcessAsync(
        CapacityProcessingRequest request,
        CancellationToken cancellationToken = default)
    {
        string displayedCapacity =
            string.IsNullOrWhiteSpace(request.Capacity)
                ? "(empty)"
                : request.Capacity;

        CapacityProcessingResult result =
            CapacityProcessingResult.Skip(
                $"Capacity '{displayedCapacity}' is not supported.");

        return Task.FromResult(result);
    }
}