namespace CommunicationProject.Services
    .CommunicationLinks.Import;

internal sealed record AccessLinkPreparationResult(
    IReadOnlyList<AccessLinkRow> Rows,
    IReadOnlyList<string> RequiredSiteIds,
    IReadOnlyList<string> ConflictingNames)
{
    public bool HasConflictingNames =>
        ConflictingNames.Count > 0;
}