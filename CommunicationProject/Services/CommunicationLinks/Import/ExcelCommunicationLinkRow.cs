namespace CommunicationProject.Services
    .CommunicationLinks.Import;

public sealed record ExcelCommunicationLinkRow(
    string SiteFromId,
    string SiteToId,
    int StmCapacity,
    string LinkTypeName)
{
    public string LinkName =>
        $"{SiteFromId} - {SiteToId}";
}