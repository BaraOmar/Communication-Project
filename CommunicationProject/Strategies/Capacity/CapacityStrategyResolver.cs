namespace CommunicationProject.Strategies.Capacity;

public sealed class CapacityStrategyResolver
    : ICapacityStrategyResolver
{
    private readonly IEnumerable<ICapacityStrategy> _strategies;

    private readonly UnsupportedCapacityStrategy
        _unsupportedStrategy;

    public CapacityStrategyResolver(
        IEnumerable<ICapacityStrategy> strategies,
        UnsupportedCapacityStrategy unsupportedStrategy)
    {
        _strategies = strategies;
        _unsupportedStrategy = unsupportedStrategy;
    }

    public ICapacityStrategy Resolve(
        string? capacity)
    {
        return _strategies.FirstOrDefault(
                   strategy =>
                       strategy.CanHandle(capacity))
               ?? _unsupportedStrategy;
    }
}