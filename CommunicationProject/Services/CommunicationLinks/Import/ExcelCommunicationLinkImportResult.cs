namespace CommunicationProject.Services
    .CommunicationLinks.Import;

public sealed record ExcelCommunicationLinkImportResult(
    bool Succeeded,
    int CreatedSites,
    int CreatedLinkTypes,
    int ImportedLinks,
    int SkippedLinks,
    int CreatedStms,
    int CreatedE1s,
    string Message)
{
    public static ExcelCommunicationLinkImportResult Success(
        int createdSites,
        int createdLinkTypes,
        int importedLinks,
        int skippedLinks,
        int createdStms,
        int createdE1s)
    {
        return new ExcelCommunicationLinkImportResult(
            true,
            createdSites,
            createdLinkTypes,
            importedLinks,
            skippedLinks,
            createdStms,
            createdE1s,
            $"Import completed. " +
            $"{createdSites} sites created, " +
            $"{createdLinkTypes} link types created, " +
            $"{importedLinks} links imported, " +
            $"{skippedLinks} skipped, " +
            $"{createdStms} STM records created, " +
            $"{createdE1s} E1 records created.");
    }

    public static ExcelCommunicationLinkImportResult Failure(
        string message)
    {
        return new ExcelCommunicationLinkImportResult(
            false,
            0,
            0,
            0,
            0,
            0,
            0,
            message);
    }
}