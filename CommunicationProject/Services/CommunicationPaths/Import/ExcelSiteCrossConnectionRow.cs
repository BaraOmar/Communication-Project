using CommunicationProject.Models;

namespace CommunicationProject.Services.CommunicationPaths.Import;

public sealed record ExcelStmE1Reference(
    string? StmNumber,
    int ShelfNumber,
    int SlotNumber,
    int PortNumber,
    string E1Number,
    string? MuxName = null,
    MuxModel? Model = null);

public sealed record ExcelSiteCrossConnectionRow(
    int DataRowNumber,
    string CurrentSiteId,
    ExcelStmE1Reference Incoming,
    ExcelStmE1Reference Outgoing);