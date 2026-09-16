using CommunicationProject.Data;
using CommunicationProject.Models;
using CommunicationProject.Strategies.Capacity;
using Microsoft.EntityFrameworkCore;

namespace CommunicationProject.Services
    .CommunicationLinks.Import;

public sealed class ExcelCommunicationLinkImportService
{
    private readonly CommunicationDbContext _context;

    private readonly IStmInventoryFactory
        _stmInventoryFactory;

    public ExcelCommunicationLinkImportService(
        CommunicationDbContext context,
        IStmInventoryFactory stmInventoryFactory)
    {
        _context = context;
        _stmInventoryFactory = stmInventoryFactory;
    }


    public async Task<ExcelCommunicationLinkImportResult> ImportAsync(
        IReadOnlyList<ExcelCommunicationLinkRow> rows,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
        {
            return ExcelCommunicationLinkImportResult.Failure(
                "The Excel file contains no communication-link rows.");
        }


        /*
         * Load reference data once.
         */
        var sites =
            await _context.Sites
                .AsNoTracking()
                .ToListAsync(cancellationToken);

        var linkTypes =
            await _context.LinkTypes
                .AsNoTracking()
                .ToListAsync(cancellationToken);


        var siteIds =
            sites
                .Select(site => site.Id)
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);


        var linkTypesByName =
            linkTypes
                .ToDictionary(
                    type => type.Name.Trim(),
                    type => type,
                    StringComparer.OrdinalIgnoreCase);


        /*
         * Validate every Excel row before inserting anything.
         */
        for (int i = 0; i < rows.Count; i++)
        {
            ExcelCommunicationLinkRow row =
                rows[i];

            int excelRowNumber =
                i + 2;




            if (string.Equals(
                    row.SiteFromId,
                    row.SiteToId,
                    StringComparison.OrdinalIgnoreCase))
            {
                return ExcelCommunicationLinkImportResult.Failure(
                    $"Excel row {excelRowNumber}: " +
                    "Site From and Site To cannot be the same.");
            }




            if (row.StmCapacity is < 1 or > 100)
            {
                return ExcelCommunicationLinkImportResult.Failure(
                    $"Excel row {excelRowNumber}: " +
                    "STM capacity must be between 1 and 100.");
            }
        }


        /*
         * Existing logical link names.
         *
         * Since the generated name is:
         * 005 - 006
         *
         * we must not generate it again.
         */
        var existingNames =
            await _context.CommunicationLinks
                .AsNoTracking()
                .Where(link => link.IsPrimary)
                .Select(link => link.Name)
                .ToListAsync(cancellationToken);


        var usedNames =
            existingNames.ToHashSet(
                StringComparer.OrdinalIgnoreCase);
        var existingRoutes =
    await _context.CommunicationLinks
        .AsNoTracking()
        .Select(link => new
        {
            link.LinkTypeId,
            link.SiteFromId,
            link.SiteToId
        })
        .ToListAsync(cancellationToken);


        var usedRoutes =
            existingRoutes
                .Select(route =>
                    CommunicationLinkImportKeys
                        .GetDirectionalRouteKey(
                            route.LinkTypeId,
                            route.SiteFromId,
                            route.SiteToId))
                .ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

        int importedLinks = 0;
        int skippedLinks = 0;
        int createdStms = 0;
        int createdE1s = 0;


        await using var transaction =
            await _context.Database
                .BeginTransactionAsync(
                    cancellationToken);


        try
        {
            /*
 * Create Sites that appear in the Excel file
 * but do not exist in the database.
 */
            var requiredSiteIds =
                rows
                    .SelectMany(row =>
                        new[]
                        {
                row.SiteFromId,
                row.SiteToId
                        })
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();


            var missingSiteIds =
                requiredSiteIds
                    .Where(siteId =>
                        !siteIds.Contains(siteId))
                    .ToList();

            int createdSites =
                missingSiteIds.Count;

            foreach (string siteId in missingSiteIds)
            {
                var newSite =
                    new Site
                    {
                        Id = siteId,
                        Name = siteId,
                        Location = "Imported from Excel"
                    };

                _context.Sites.Add(newSite);

                siteIds.Add(siteId);
            }

            if (missingSiteIds.Count > 0)
            {
                await _context.SaveChangesAsync(
                    cancellationToken);
            }
            /*
 * Create Link Types that appear in Excel
 * but do not exist in the database.
 */
            var requiredLinkTypeNames =
                rows
                    .Select(row =>
                        row.LinkTypeName.Trim())
                    .Distinct(
                        StringComparer.OrdinalIgnoreCase)
                    .ToList();

            int createdLinkTypes = 0;

            foreach (string linkTypeName
                     in requiredLinkTypeNames)
            {
                if (linkTypesByName.ContainsKey(
                        linkTypeName))
                {
                    continue;
                }

                var newLinkType =
                    new LinkType
                    {
                        Id = Guid.NewGuid(),
                        Name = linkTypeName
                    };

                _context.LinkTypes.Add(
                    newLinkType);

                linkTypesByName.Add(
                    linkTypeName,
                    newLinkType);

                createdLinkTypes++;
            }


            await _context.SaveChangesAsync(
                cancellationToken);

            foreach (ExcelCommunicationLinkRow row in rows)
            {
                string linkName =
                    row.LinkName;


                LinkType linkType =
                    linkTypesByName[
                        row.LinkTypeName];


                string forwardRoute =
                    CommunicationLinkImportKeys
                        .GetDirectionalRouteKey(
                            linkType.Id,
                            row.SiteFromId,
                            row.SiteToId);

                string reverseRoute =
                    CommunicationLinkImportKeys
                        .GetDirectionalRouteKey(
                            linkType.Id,
                            row.SiteToId,
                            row.SiteFromId);


                /*
                 * Skip if:
                 *
                 * - the generated link name already exists
                 * - the same route already exists
                 * - the reverse route already exists
                 * - the same route appears twice in this Excel file
                 */
                if (usedNames.Contains(linkName) ||
                    usedRoutes.Contains(forwardRoute) ||
                    usedRoutes.Contains(reverseRoute))
                {
                    skippedLinks++;
                    continue;
                }

                var primaryLink =
                    new CommunicationLink
                    {
                        Id = Guid.NewGuid(),

                        Name = linkName,

                        LinkTypeId =
                            linkType.Id,

                        SiteFromId =
                            row.SiteFromId,

                        SiteToId =
                            row.SiteToId,

                        Capacity =
                            $"{row.StmCapacity} STM",

                        IsPrimary = true,

                        ConnectedLinkId = null
                    };


                var reverseLink =
                    new CommunicationLink
                    {
                        Id = Guid.NewGuid(),

                        Name = linkName,

                        LinkTypeId =
                            linkType.Id,

                        SiteFromId =
                            row.SiteToId,

                        SiteToId =
                            row.SiteFromId,

                        Capacity =
                            $"{row.StmCapacity} STM",

                        IsPrimary = false,

                        ConnectedLinkId = null
                    };


                /*
                 * First insert both directional link records.
                 */
                _context.CommunicationLinks.AddRange(
                    primaryLink,
                    reverseLink);

                await _context.SaveChangesAsync(
                    cancellationToken);


                /*
                 * Then connect the two records.
                 */
                primaryLink.ConnectedLinkId =
                    reverseLink.Id;

                reverseLink.ConnectedLinkId =
                    primaryLink.Id;

                await _context.SaveChangesAsync(
                    cancellationToken);


                /*
                 * Reuse the same STM/E1 factory already used
                 * by the capacity strategy.
                 */
                StmInventoryBatch inventory =
                    _stmInventoryFactory.Create(
                        primaryLink.Id,
                        reverseLink.Id,
                        row.StmCapacity);


                _context.Stms.AddRange(
                    inventory.Stms);

                _context.E1s.AddRange(
                    inventory.E1s);


                /*
                 * First save with reciprocal IDs still null.
                 */
                await _context.SaveChangesAsync(
                    cancellationToken);


                inventory.ConnectReciprocalRecords();


                /*
                 * Second save persists ConnectedStmId
                 * and ConnectedE1Id.
                 */
                await _context.SaveChangesAsync(
                    cancellationToken);


                importedLinks++;

                createdStms +=
                    inventory.StmCount;

                createdE1s +=
                    inventory.E1Count;


                usedNames.Add(linkName);

                usedRoutes.Add(forwardRoute);
                usedRoutes.Add(reverseRoute);
            }


            await transaction.CommitAsync(
                cancellationToken);


            return ExcelCommunicationLinkImportResult.Success(
                createdSites,
                createdLinkTypes,
                importedLinks,
                skippedLinks,
                createdStms,
                createdE1s);
        }
        catch
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw;
        }
    }
}