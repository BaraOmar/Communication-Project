namespace CommunicationProject.Services
    .CommunicationLinks.Import;

public interface ICommunicationLinkImportService
{
    Task<CommunicationLinkImportResult> ImportAsync(
        string accessFilePath,
        Guid linkTypeId,
        CancellationToken cancellationToken = default);
}