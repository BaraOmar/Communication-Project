using CommunicationProject.Data;
using CommunicationProject.Models;
using Microsoft.EntityFrameworkCore;

namespace CommunicationProject.Services
    .CommunicationLinks.Import;

internal sealed class CommunicationLinkImportPlanner
{
    private readonly CommunicationDbContext _context;

    public CommunicationLinkImportPlanner(
        CommunicationDbContext context)
    {
        _context = context;
    }

    public async Task<CommunicationLinkImportPlan> BuildAsync(
        IReadOnlyList<AccessLinkRow> rows,
        IReadOnlyList<string> requiredSiteIds,
        Guid linkTypeId,
        CancellationToken cancellationToken = default)
    {
        List<Site> sitesToInsert =
            await FindMissingSitesAsync(
                requiredSiteIds,
                cancellationToken);

        HashSet<string> usedNames =
            await GetUsedNamesAsync(
                rows,
                cancellationToken);

        HashSet<string> usedRoutes =
            await GetUsedRouteKeysAsync(
                linkTypeId,
                cancellationToken);

        List<CommunicationLinkPair> pairs =
            PreparePairs(
                rows,
                linkTypeId,
                usedNames,
                usedRoutes,
                out int skippedLogicalLinks);

        return new CommunicationLinkImportPlan(
            sitesToInsert,
            pairs,
            skippedLogicalLinks);
    }

    private async Task<List<Site>> FindMissingSitesAsync(
        IReadOnlyList<string> requiredSiteIds,
        CancellationToken cancellationToken)
    {
        List<string> existingIds =
            await _context.Sites
                .AsNoTracking()
                .Where(site =>
                    requiredSiteIds.Contains(site.Id))
                .Select(site => site.Id)
                .ToListAsync(cancellationToken);

        HashSet<string> existingIdSet =
            existingIds.ToHashSet(
                StringComparer.OrdinalIgnoreCase);

        return requiredSiteIds
            .Where(siteId =>
                !existingIdSet.Contains(siteId))
            .Select(siteId =>
                new Site
                {
                    Id = siteId,
                    Name = siteId,
                    Location = string.Empty
                })
            .ToList();
    }

    private async Task<HashSet<string>> GetUsedNamesAsync(
        IReadOnlyList<AccessLinkRow> rows,
        CancellationToken cancellationToken)
    {
        List<string> importedNames =
            rows
                .Select(row => row.Name)
                .Distinct(
                    StringComparer.OrdinalIgnoreCase)
                .ToList();

        List<string> existingNames =
            await _context.CommunicationLinks
                .AsNoTracking()
                .Where(link =>
                    link.IsPrimary &&
                    importedNames.Contains(link.Name))
                .Select(link => link.Name)
                .ToListAsync(cancellationToken);

        return existingNames.ToHashSet(
            StringComparer.OrdinalIgnoreCase);
    }

    private async Task<HashSet<string>> GetUsedRouteKeysAsync(
        Guid linkTypeId,
        CancellationToken cancellationToken)
    {
        var existingRoutes =
            await _context.CommunicationLinks
                .AsNoTracking()
                .Where(link =>
                    link.LinkTypeId == linkTypeId)
                .Select(link => new
                {
                    link.SiteFromId,
                    link.SiteToId
                })
                .ToListAsync(cancellationToken);

        return existingRoutes
            .Select(route =>
                CommunicationLinkImportKeys
                    .GetDirectionalRouteKey(
                        linkTypeId,
                        route.SiteFromId,
                        route.SiteToId))
            .ToHashSet(
                StringComparer.OrdinalIgnoreCase);
    }

    private static List<CommunicationLinkPair>
        PreparePairs(
            IReadOnlyList<AccessLinkRow> rows,
            Guid linkTypeId,
            HashSet<string> usedNames,
            HashSet<string> usedRoutes,
            out int skippedLogicalLinks)
    {
        var pairs =
            new List<CommunicationLinkPair>();

        skippedLogicalLinks = 0;

        foreach (AccessLinkRow row in rows)
        {
            if (ShouldSkipSelfLink(row))
            {
                skippedLogicalLinks++;
                continue;
            }

            string forwardRoute =
                CommunicationLinkImportKeys
                    .GetDirectionalRouteKey(
                        linkTypeId,
                        row.SiteAId,
                        row.SiteBId);

            string reverseRoute =
                CommunicationLinkImportKeys
                    .GetDirectionalRouteKey(
                        linkTypeId,
                        row.SiteBId,
                        row.SiteAId);

            bool alreadyUsed =
                usedNames.Contains(row.Name) ||
                usedRoutes.Contains(forwardRoute) ||
                usedRoutes.Contains(reverseRoute);

            if (alreadyUsed)
            {
                skippedLogicalLinks++;
                continue;
            }

            pairs.Add(
                CreatePair(
                    row,
                    linkTypeId));

            usedNames.Add(row.Name);
            usedRoutes.Add(forwardRoute);
            usedRoutes.Add(reverseRoute);
        }

        return pairs;
    }

    private static bool ShouldSkipSelfLink(
        AccessLinkRow row)
    {
        return string.Equals(
            row.SiteAId,
            row.SiteBId,
            StringComparison.OrdinalIgnoreCase);
    }

    private static CommunicationLinkPair CreatePair(
        AccessLinkRow row,
        Guid linkTypeId)
    {
        var primary =
            new CommunicationLink
            {
                Id = Guid.NewGuid(),
                Name = row.Name,
                Capacity = row.Capacity,
                LinkTypeId = linkTypeId,
                SiteFromId = row.SiteAId,
                SiteToId = row.SiteBId,
                ConnectedLinkId = null,
                IsPrimary = true
            };

        var reverse =
            new CommunicationLink
            {
                Id = Guid.NewGuid(),
                Name = row.Name,
                Capacity = row.Capacity,
                LinkTypeId = linkTypeId,
                SiteFromId = row.SiteBId,
                SiteToId = row.SiteAId,
                ConnectedLinkId = null,
                IsPrimary = false
            };

        return new CommunicationLinkPair(
            primary,
            reverse);
    }
}