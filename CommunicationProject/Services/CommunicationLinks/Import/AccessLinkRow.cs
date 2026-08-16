namespace CommunicationProject.Services
    .CommunicationLinks.Import;

internal sealed record AccessLinkRow(
    string Name,
    string SiteAId,
    string SiteBId,
    string Capacity);