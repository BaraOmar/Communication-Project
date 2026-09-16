namespace CommunicationProject.Services
    .CommunicationLinks.Import;

public sealed record CommunicationLinkImportResult(
    bool Succeeded,
    int CreatedSites,
    int InsertedLogicalLinks,
    int InsertedDirectionalRecords,
    int SkippedLogicalLinks,
    string Message)
{
    public static CommunicationLinkImportResult Success(
        int createdSites,
        int insertedLogicalLinks,
        int skippedLogicalLinks)
    {
        int directionalRecords =
            insertedLogicalLinks * 2;

        return new CommunicationLinkImportResult(
            true,
            createdSites,
            insertedLogicalLinks,
            directionalRecords,
            skippedLogicalLinks,
            $"Import completed. " +
            $"{createdSites} missing sites created. " +
            $"{insertedLogicalLinks} logical links " +
            $"({directionalRecords} directional records) inserted. " +
            $"{skippedLogicalLinks} existing or invalid links skipped.");
    }

    public static CommunicationLinkImportResult NoChanges(
        int skippedLogicalLinks)
    {
        return new CommunicationLinkImportResult(
            true,
            0,
            0,
            0,
            skippedLogicalLinks,
            "No data was inserted. All uploaded sites " +
            "and links already exist.");
    }

    public static CommunicationLinkImportResult Failure(
        string message)
    {
        return new CommunicationLinkImportResult(
            false,
            0,
            0,
            0,
            0,
            message);
    }
}