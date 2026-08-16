namespace CommunicationProject.Strategies.Capacity;

public sealed record CapacityProcessingResult(
    bool Succeeded,
    bool Skipped,
    int CreatedStms,
    int CreatedE1s,
    string Message)
{
    public static CapacityProcessingResult Success(
        int createdStms,
        int createdE1s,
        string message)
    {
        return new CapacityProcessingResult(
            Succeeded: true,
            Skipped: false,
            CreatedStms: createdStms,
            CreatedE1s: createdE1s,
            Message: message);
    }

    public static CapacityProcessingResult Skip(
        string message)
    {
        return new CapacityProcessingResult(
            Succeeded: false,
            Skipped: true,
            CreatedStms: 0,
            CreatedE1s: 0,
            Message: message);
    }

    public static CapacityProcessingResult Failure(
        string message)
    {
        return new CapacityProcessingResult(
            Succeeded: false,
            Skipped: false,
            CreatedStms: 0,
            CreatedE1s: 0,
            Message: message);
    }
}