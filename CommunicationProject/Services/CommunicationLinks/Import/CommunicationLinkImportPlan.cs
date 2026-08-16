using CommunicationProject.Models;

namespace CommunicationProject.Services
    .CommunicationLinks.Import;

internal sealed record CommunicationLinkImportPlan(
    IReadOnlyList<Site> SitesToInsert,
    IReadOnlyList<CommunicationLinkPair> LinkPairs,
    int SkippedLogicalLinks)
{
    public bool HasChanges =>
        SitesToInsert.Count > 0 ||
        LinkPairs.Count > 0;
}