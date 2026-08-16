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

        string message =
            $"Import completed. " +
            $"{createdSites} missing sites created. " +
            $"{insertedLogicalLinks} logical links " +
            $"({directionalRecords} directional records) inserted. " +
            $"{skippedLogicalLinks} existing or invalid links skipped.";

        return new CommunicationLinkImportResult(
            Succeeded: true,
            CreatedSites: createdSites,
            InsertedLogicalLinks: insertedLogicalLinks,
            InsertedDirectionalRecords: directionalRecords,
            SkippedLogicalLinks: skippedLogicalLinks,
            Message: message);
    }

    public static CommunicationLinkImportResult NoChanges(
        int skippedLogicalLinks)
    {
        return new CommunicationLinkImportResult(
            Succeeded: true,
            CreatedSites: 0,
            InsertedLogicalLinks: 0,
            InsertedDirectionalRecords: 0,
            SkippedLogicalLinks: skippedLogicalLinks,
            Message:
                "No data was inserted. All uploaded sites " +
                "and links already exist.");
    }

    public static CommunicationLinkImportResult Failure(
        string message)
    {
        return new CommunicationLinkImportResult(
            Succeeded: false,
            CreatedSites: 0,
            InsertedLogicalLinks: 0,
            InsertedDirectionalRecords: 0,
            SkippedLogicalLinks: 0,
            Message: message);
    }
}