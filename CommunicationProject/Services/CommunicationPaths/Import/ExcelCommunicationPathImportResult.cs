namespace CommunicationProject.Services.CommunicationPaths.Import;

public sealed record ExcelCommunicationPathImportResult(
    int ImportedCircuits,
    int SkippedCircuits,
    IReadOnlyList<string> Errors)
{
    public bool Succeeded => Errors.Count == 0;

    public string Message =>
        $"Paths imported: {ImportedCircuits}. " +
        $"Already present: {SkippedCircuits}." +
        (Succeeded
            ? ""
            : " Errors: " + string.Join(" | ", Errors));
}