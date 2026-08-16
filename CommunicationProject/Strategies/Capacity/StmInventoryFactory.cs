using CommunicationProject.Models;

namespace CommunicationProject.Strategies.Capacity;

public sealed class StmInventoryFactory
    : IStmInventoryFactory
{
    private const int E1ChannelsPerStm = 63;

    public StmInventoryBatch Create(
        Guid primaryLinkId,
        Guid reverseLinkId,
        int stmCount)
    {
        if (stmCount is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stmCount),
                "STM count must be between 1 and 100.");
        }

        var stms =
            new List<Stm>(stmCount * 2);

        var e1s =
            new List<E1>(
                stmCount *
                2 *
                E1ChannelsPerStm);

        var stmPairs =
            new List<(
                Stm Primary,
                Stm Reverse)>(stmCount);

        var e1Pairs =
            new List<(
                E1 Primary,
                E1 Reverse)>(
                    stmCount *
                    E1ChannelsPerStm);

        for (int stmNumber = 1;
             stmNumber <= stmCount;
             stmNumber++)
        {
            Stm primaryStm =
                CreateStm(
                    primaryLinkId,
                    stmNumber);

            Stm reverseStm =
                CreateStm(
                    reverseLinkId,
                    stmNumber);

            stms.Add(primaryStm);
            stms.Add(reverseStm);

            stmPairs.Add(
                (primaryStm, reverseStm));

            AddE1Inventory(
                primaryStm,
                reverseStm,
                e1s,
                e1Pairs);
        }

        return new StmInventoryBatch(
            stms,
            e1s,
            stmPairs,
            e1Pairs);
    }

    private static Stm CreateStm(
        Guid linkId,
        int number)
    {
        return new Stm
        {
            Id = Guid.NewGuid(),
            Number = number.ToString(),
            LinkId = linkId,
            ConnectedStmId = null
        };
    }

    private static void AddE1Inventory(
        Stm primaryStm,
        Stm reverseStm,
        ICollection<E1> e1s,
        ICollection<(
            E1 Primary,
            E1 Reverse)> e1Pairs)
    {
        for (int firstPart = 1;
             firstPart <= 3;
             firstPart++)
        {
            for (int secondPart = 1;
                 secondPart <= 7;
                 secondPart++)
            {
                for (int thirdPart = 1;
                     thirdPart <= 3;
                     thirdPart++)
                {
                    string number =
                        $"{firstPart}." +
                        $"{secondPart}." +
                        $"{thirdPart}";

                    E1 primaryE1 =
                        CreateE1(
                            primaryStm.Id,
                            number);

                    E1 reverseE1 =
                        CreateE1(
                            reverseStm.Id,
                            number);

                    e1s.Add(primaryE1);
                    e1s.Add(reverseE1);

                    e1Pairs.Add(
                        (primaryE1, reverseE1));
                }
            }
        }
    }

    private static E1 CreateE1(
        Guid stmId,
        string number)
    {
        return new E1
        {
            Id = Guid.NewGuid(),
            E1Number = number,
            StmId = stmId,
            ConnectedE1Id = null,
            Description = null,
            CrossConnectionState =
                E1CrossConnectionState.Available
        };
    }
}