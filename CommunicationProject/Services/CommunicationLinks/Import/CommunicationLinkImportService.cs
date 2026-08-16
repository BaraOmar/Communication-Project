using CommunicationProject.Data;
using Microsoft.EntityFrameworkCore;

namespace CommunicationProject.Services
    .CommunicationLinks.Import;

internal sealed class CommunicationLinkImportService
    : ICommunicationLinkImportService
{
    private readonly CommunicationDbContext _context;

    private readonly CommunicationLinkImportPlanner
        _planner;

    private readonly CommunicationLinkImportWriter
        _writer;

    public CommunicationLinkImportService(
        CommunicationDbContext context,
        CommunicationLinkImportPlanner planner,
        CommunicationLinkImportWriter writer)
    {
        _context = context;
        _planner = planner;
        _writer = writer;
    }

    public async Task<CommunicationLinkImportResult> ImportAsync(
        string accessFilePath,
        Guid linkTypeId,
        CancellationToken cancellationToken = default)
    {
        if (linkTypeId == Guid.Empty)
        {
            return CommunicationLinkImportResult.Failure(
                "A link type must be selected.");
        }

        bool linkTypeExists =
            await _context.LinkTypes
                .AsNoTracking()
                .AnyAsync(
                    type => type.Id == linkTypeId,
                    cancellationToken);

        if (!linkTypeExists)
        {
            return CommunicationLinkImportResult.Failure(
                "The selected link type does not exist.");
        }

        List<AccessLinkRow> rows =
            AccessLinkFileReader.Read(
                accessFilePath);

        if (rows.Count == 0)
        {
            return CommunicationLinkImportResult.Failure(
                "The Access LINKS table contains no records.");
        }

        AccessLinkPreparationResult preparation =
            AccessLinkImportPreprocessor.Prepare(
                rows);

        if (preparation.HasConflictingNames)
        {
            return CommunicationLinkImportResult.Failure(
                "These link names belong to multiple site pairs: " +
                string.Join(
                    ", ",
                    preparation.ConflictingNames));
        }

        CommunicationLinkImportPlan plan =
            await _planner.BuildAsync(
                preparation.Rows,
                preparation.RequiredSiteIds,
                linkTypeId,
                cancellationToken);

        if (!plan.HasChanges)
        {
            return CommunicationLinkImportResult.NoChanges(
                plan.SkippedLogicalLinks);
        }

        await _writer.SaveAsync(
            plan,
            cancellationToken);

        return CommunicationLinkImportResult.Success(
            createdSites:
                plan.SitesToInsert.Count,

            insertedLogicalLinks:
                plan.LinkPairs.Count,

            skippedLogicalLinks:
                plan.SkippedLogicalLinks);
    }
}