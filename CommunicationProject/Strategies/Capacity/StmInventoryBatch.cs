using CommunicationProject.Models;

namespace CommunicationProject.Strategies.Capacity;

public sealed class StmInventoryBatch
{
    private readonly IReadOnlyList<(
        Stm Primary,
        Stm Reverse)> _stmPairs;

    private readonly IReadOnlyList<(
        E1 Primary,
        E1 Reverse)> _e1Pairs;

    public StmInventoryBatch(
        IReadOnlyList<Stm> stms,
        IReadOnlyList<E1> e1s,
        IReadOnlyList<(
            Stm Primary,
            Stm Reverse)> stmPairs,
        IReadOnlyList<(
            E1 Primary,
            E1 Reverse)> e1Pairs)
    {
        Stms = stms;
        E1s = e1s;
        _stmPairs = stmPairs;
        _e1Pairs = e1Pairs;
    }

    public IReadOnlyList<Stm> Stms { get; }

    public IReadOnlyList<E1> E1s { get; }

    public int StmCount => Stms.Count;

    public int E1Count => E1s.Count;

    public void ConnectReciprocalRecords()
    {
        foreach (var pair in _stmPairs)
        {
            pair.Primary.ConnectedStmId =
                pair.Reverse.Id;

            pair.Reverse.ConnectedStmId =
                pair.Primary.Id;
        }

        foreach (var pair in _e1Pairs)
        {
            pair.Primary.ConnectedE1Id =
                pair.Reverse.Id;

            pair.Reverse.ConnectedE1Id =
                pair.Primary.Id;
        }
    }
}