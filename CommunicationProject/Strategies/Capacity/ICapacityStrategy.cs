namespace CommunicationProject.Strategies.Capacity;

public interface ICapacityStrategy
{
    bool CanHandle(string? capacity);

    Task<CapacityProcessingResult> ProcessAsync(
        CapacityProcessingRequest request,
        CancellationToken cancellationToken = default);
}