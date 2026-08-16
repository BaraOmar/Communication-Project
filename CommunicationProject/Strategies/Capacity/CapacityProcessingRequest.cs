namespace CommunicationProject.Strategies.Capacity;

public sealed record CapacityProcessingRequest(
    Guid PrimaryLinkId,
    Guid ReverseLinkId,
    string LinkName,
    string? Capacity);