using CommunicationProject.Models;
using Microsoft.EntityFrameworkCore;

namespace CommunicationProject.Services.CommunicationPaths.Import;

public sealed partial class ExcelCommunicationPathImportService
{
    public async Task<ExcelCommunicationPathImportResult>
        ImportCrossConnectionsAsync(
            IReadOnlyList<ExcelSiteCrossConnectionRow> rows,
            string? customerName,
            CancellationToken ct = default)
    {
        customerName = customerName?.Trim();

        if (string.IsNullOrWhiteSpace(customerName) ||
            customerName.Length > 200)
        {
            throw new InvalidDataException(
                "Enter a customer name of 1–200 characters.");
        }

        if (rows.Count == 0)
        {
            throw new InvalidDataException(
                "The file contains no cross-connections.");
        }

        var connections = new List<ResolvedSiteCrossConnection>();

        foreach (var row in rows)
        {
            var incoming = await ResolveEndpointAsync(
                row.CurrentSiteId,
                row.Incoming,
                row.DataRowNumber,
                "left",
                ct);

            var outgoing = await ResolveEndpointAsync(
                row.CurrentSiteId,
                row.Outgoing,
                row.DataRowNumber,
                "right",
                ct);

            if (incoming.Id == outgoing.Id)
            {
                throw new InvalidDataException(
                    $"Data row {row.DataRowNumber}: " +
                    "incoming and outgoing E1s must differ.");
            }

            connections.Add(new(row, incoming, outgoing));
        }

        if (connections.Select(item => item.Incoming.Id)
                .Distinct().Count() != rows.Count ||
            connections.Select(item => item.Outgoing.Id)
                .Distinct().Count() != rows.Count)
        {
            throw new InvalidDataException(
                "An incoming or outgoing E1 is used more than once.");
        }

        // Find the first cross-connection using opposite E1 pairings.
        var starts = connections
            .Where(item => !connections.Any(previous =>
                previous.Outgoing.ConnectedE1Id == item.Incoming.Id))
            .ToList();

        if (starts.Count != 1)
        {
            throw new InvalidDataException(
                "The rows must describe one continuous, " +
                "directed path with an open start.");
        }

        var byIncoming = connections.ToDictionary(
            item => item.Incoming.Id);

        var ordered = new List<ResolvedSiteCrossConnection>();
        var visited = new HashSet<Guid>();
        var current = starts[0];

        while (true)
        {
            if (!visited.Add(current.Incoming.Id))
            {
                throw new InvalidDataException(
                    "The cross-connections contain a cycle.");
            }

            ordered.Add(current);

            if (!byIncoming.TryGetValue(
                    current.Outgoing.ConnectedE1Id!.Value,
                    out var next))
            {
                break;
            }

            current = next;
        }

        if (ordered.Count != connections.Count)
        {
            throw new InvalidDataException(
                "The rows contain disconnected sections " +
                "or mismatched E1s between sites.");
        }

        // First link: origin -> first current site.
        var segments = new List<ExcelCommunicationPathRow>
        {
            await BuildSegmentAsync(
                ordered[0].Incoming.ConnectedE1!,
                1,
                ordered[0].Row.DataRowNumber,
                customerName,
                ct)
        };

        // Remaining links: each current site -> its next site.
        foreach (var item in ordered)
        {
            segments.Add(await BuildSegmentAsync(
                item.Outgoing,
                segments.Count + 1,
                item.Row.DataRowNumber,
                customerName,
                ct));
        }

        return await ImportAsync(segments, ct);
    }

    private async Task<E1> ResolveEndpointAsync(
        string siteId,
        ExcelStmE1Reference reference,
        int dataRow,
        string side,
        CancellationToken ct)
    {
        var matches = await _context.E1s
            .AsNoTracking()
            .Where(e1 =>
                e1.Link.SiteFromId == siteId &&
                e1.E1Number == reference.E1Number &&
                e1.Stm != null &&
                e1.Stm.Number == reference.StmNumber &&
                _context.MuxPorts.Any(port =>
                    port.StmId == e1.StmId &&
                    port.MuxCard.Category == CardCategory.STM &&
                    port.MuxCard.Mux.SiteId == siteId &&
                    port.MuxCard.SlotNumber == reference.SlotNumber &&
                    port.PortNumber == reference.PortNumber))
            .Include(e1 => e1.Link)
            .Include(e1 => e1.Stm)
            .Include(e1 => e1.ConnectedE1)
                .ThenInclude(e1 => e1!.Link)
            .Include(e1 => e1.ConnectedE1)
                .ThenInclude(e1 => e1!.Stm)
            .Take(2)
            .ToListAsync(ct);

        if (matches.Count != 1)
        {
            throw new InvalidDataException(
                $"Data row {dataRow}, {side} side: " +
                $"STM/E1 not found or ambiguous at site {siteId}.");
        }

        var e1 = matches[0];

        if (e1.ConnectedE1 == null ||
            e1.ConnectedE1.ConnectedE1Id != e1.Id ||
            e1.ConnectedE1.Link.SiteFromId != e1.Link.SiteToId ||
            e1.ConnectedE1.Link.SiteToId != siteId)
        {
            throw new InvalidDataException(
                $"Data row {dataRow}, {side} side: " +
                "the opposite E1 pairing is invalid.");
        }

        return e1;
    }

    private async Task<ExcelCommunicationPathRow> BuildSegmentAsync(
        E1 local,
        int order,
        int dataRow,
        string customerName,
        CancellationToken ct)
    {
        if (local.StmId == null || local.ConnectedE1Id == null)
        {
            throw new InvalidDataException(
                $"Data row {dataRow}: the selected SDH endpoint " +
                "has no STM or opposite E1.");
        }

        var ports = await _context.MuxPorts
            .AsNoTracking()
            .Where(port =>
                port.StmId == local.StmId &&
                port.MuxCard.Category == CardCategory.STM &&
                port.MuxCard.Mux.SiteId == local.Link.SiteFromId)
            .Select(port => new
            {
                Slot = port.MuxCard.SlotNumber,
                Port = port.PortNumber,
                MuxName = port.MuxCard.Mux.Name
            })
            .Take(2)
            .ToListAsync(ct);

        if (ports.Count != 1)
        {
            throw new InvalidDataException(
                $"Data row {dataRow}: no unique STM port " +
                $"is installed at site {local.Link.SiteFromId}.");
        }

        var location = ports[0];

        string address = FormattableString.Invariant(
            $"1.{location.Slot}.{location.Port}");

        return new ExcelCommunicationPathRow(
            dataRow,
            customerName,
            customerName,
            order,
            local.Link.SiteFromId,
            local.Link.SiteToId,
            local.Link.Name,
            address,
            local.E1Number,
            location.MuxName,
            local.Id,
            local.ConnectedE1Id);
    }

    private sealed record ResolvedSiteCrossConnection(
        ExcelSiteCrossConnectionRow Row,
        E1 Incoming,
        E1 Outgoing);
}