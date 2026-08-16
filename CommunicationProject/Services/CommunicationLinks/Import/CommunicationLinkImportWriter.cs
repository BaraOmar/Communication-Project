using CommunicationProject.Data;
using CommunicationProject.Models;

namespace CommunicationProject.Services
    .CommunicationLinks.Import;

internal sealed class CommunicationLinkImportWriter
{
    private readonly CommunicationDbContext _context;

    public CommunicationLinkImportWriter(
        CommunicationDbContext context)
    {
        _context = context;
    }

    public async Task SaveAsync(
        CommunicationLinkImportPlan plan,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _context.Database
                .BeginTransactionAsync(
                    cancellationToken);

        try
        {
            await InsertSitesAsync(
                plan.SitesToInsert,
                cancellationToken);

            await InsertLinksAsync(
                plan.LinkPairs,
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }

    private async Task InsertSitesAsync(
        IReadOnlyList<Site> sites,
        CancellationToken cancellationToken)
    {
        if (sites.Count == 0)
        {
            return;
        }

        _context.Sites.AddRange(sites);

        await _context.SaveChangesAsync(
            cancellationToken);
    }

    private async Task InsertLinksAsync(
        IReadOnlyList<CommunicationLinkPair> pairs,
        CancellationToken cancellationToken)
    {
        if (pairs.Count == 0)
        {
            return;
        }

        List<CommunicationLink> links =
            pairs
                .SelectMany(pair =>
                    new[]
                    {
                        pair.Primary,
                        pair.Reverse
                    })
                .ToList();

        /*
         * First save:
         * ConnectedLinkId remains null.
         */
        _context.CommunicationLinks.AddRange(
            links);

        await _context.SaveChangesAsync(
            cancellationToken);

        ConnectPairs(pairs);

        /*
         * Second save:
         * reciprocal relationships are persisted.
         */
        await _context.SaveChangesAsync(
            cancellationToken);
    }

    private static void ConnectPairs(
        IEnumerable<CommunicationLinkPair> pairs)
    {
        foreach (CommunicationLinkPair pair in pairs)
        {
            pair.Primary.ConnectedLinkId =
                pair.Reverse.Id;

            pair.Reverse.ConnectedLinkId =
                pair.Primary.Id;
        }
    }
}