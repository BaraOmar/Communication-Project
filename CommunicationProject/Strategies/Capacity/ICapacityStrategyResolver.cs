namespace CommunicationProject.Strategies.Capacity;

public interface ICapacityStrategyResolver
{
    ICapacityStrategy Resolve(
        string? capacity);
}