namespace CommunicationProject.Services.CommunicationPaths.Import;

public sealed record ExcelCommunicationPathRow(
    int DataRowNumber,
    string CircuitKey,
    string CustomerName,
    int Order,
    string SiteFromId,
    string SiteToId,
    string LinkName,
    string? StmNumber,
    string E1Number,
    string? MuxName = null,
    Guid? ExpectedE1Id = null,
    Guid? ExpectedConnectedE1Id = null);