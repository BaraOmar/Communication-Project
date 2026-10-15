using CommunicationProject.Data;
using CommunicationProject.Models;
using Microsoft.EntityFrameworkCore;
using System.Data;
using static CommunicationProject.Models.E1;
using System.Globalization;

namespace CommunicationProject.Services.CommunicationPaths.Import;

public sealed partial class ExcelCommunicationPathImportService
{
    private readonly CommunicationDbContext _context;
    private readonly ILogger<ExcelCommunicationPathImportService> _logger;

    public ExcelCommunicationPathImportService(
        CommunicationDbContext context,
        ILogger<ExcelCommunicationPathImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ExcelCommunicationPathImportResult> ImportAsync(
        IReadOnlyList<ExcelCommunicationPathRow> rows,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
        {
            throw new InvalidDataException(
                "There is no path data to import.");
        }

        int imported = 0;
        int skipped = 0;
        var errors = new List<string>();

        foreach (var circuit in rows.GroupBy(
                     row => row.CircuitKey,
                     StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                bool created = await ImportCircuitAsync(
                    circuit.OrderBy(row => row.Order).ToList(),
                    cancellationToken);

                if (created)
                    imported++;
                else
                    skipped++;
            }
            catch (InvalidDataException exception)
            {
                errors.Add(
                    $"Circuit '{circuit.Key}': {exception.Message}");
            }
            catch (Exception exception)
                when (exception is not OperationCanceledException)
            {
                _logger.LogError(
                    exception,
                    "Path import failed for circuit {CircuitKey}.",
                    circuit.Key);

                errors.Add(
                    $"Circuit '{circuit.Key}': could not be saved.");
            }
            finally
            {
                // Discard tracked changes from a failed circuit
                // before processing another circuit.
                _context.ChangeTracker.Clear();
            }
        }

        return new(imported, skipped, errors);
    }

    private async Task<bool> ImportCircuitAsync(
        List<ExcelCommunicationPathRow> rows,
        CancellationToken ct)
    {
        await using var transaction = await _context.Database
            .BeginTransactionAsync(IsolationLevel.Serializable, ct);

        var pairs =
            new List<(CommunicationLink Link, E1 Local, E1 Remote)>();

        var endpointIds = new HashSet<Guid>();

        // Resolve each directional link and its E1 pair.
        foreach (var row in rows)
        {
            var links = await _context.CommunicationLinks
                .Where(link =>
                    link.Name == row.LinkName &&
                    link.SiteFromId == row.SiteFromId &&
                    link.SiteToId == row.SiteToId)
                .Take(2)
                .ToListAsync(ct);

            if (links.Count != 1)
            {
                throw new InvalidDataException(
                    $"Data row {row.DataRowNumber}: " +
                    "link not found or ambiguous.");
            }

            var link = links[0];

            Guid? selectedStmId = null;

            // An empty STMNumber selects direct PDH inventory.
            if (row.StmNumber != null)
            {
                var address = row.StmNumber.Split('.');

                int slot = int.Parse(
                    address[1],
                    CultureInfo.InvariantCulture);

                int portNumber = int.Parse(
                    address[2],
                    CultureInfo.InvariantCulture);

                var portQuery = _context.MuxPorts
                    .AsNoTracking()
                    .Where(port =>
                        port.MuxCard.Category == CardCategory.STM &&
                        port.MuxCard.Mux.SiteId == row.SiteFromId &&
                        port.MuxCard.SlotNumber == slot &&
                        port.PortNumber == portNumber &&
                        port.StmId.HasValue &&
                        port.Stm!.LinkId == link.Id &&
                        port.Stm.Link.SiteFromId == row.SiteFromId);

                if (!string.IsNullOrWhiteSpace(row.MuxName))
                {
                    portQuery = portQuery.Where(port =>
                        port.MuxCard.Mux.Name == row.MuxName);
                }

                var matches = await portQuery
                    .Select(port => port.StmId!.Value)
                    .Take(2)
                    .ToListAsync(ct);

                if (matches.Count == 0)
                {
                    throw new InvalidDataException(
                        $"Data row {row.DataRowNumber}: " +
                        $"STM address {row.StmNumber} is not assigned " +
                        $"to the selected link at site {row.SiteFromId}.");
                }

                if (matches.Count > 1)
                {
                    throw new InvalidDataException(
                        $"Data row {row.DataRowNumber}: " +
                        $"STM address {row.StmNumber} is ambiguous. " +
                        "Add a MuxName column to identify the installed MUX.");
                }

                selectedStmId = matches[0];
            }

            var query = _context.E1s.Where(e1 =>
                e1.LinkId == link.Id &&
                e1.StmId == selectedStmId &&
                e1.E1Number == row.E1Number);

            var channels = await query
                .Include(e1 => e1.Stm)
                .Include(e1 => e1.ConnectedE1)
                    .ThenInclude(e1 => e1!.Link)
                .Include(e1 => e1.ConnectedE1)
                    .ThenInclude(e1 => e1!.Stm)
                .Take(2)
                .ToListAsync(ct);

            if (channels.Count != 1)
            {
                throw new InvalidDataException(
                    $"Data row {row.DataRowNumber}: " +
                    "E1/STM not found or ambiguous.");
            }

            var local = channels[0];
            var remote = local.ConnectedE1;

            if ((row.ExpectedE1Id.HasValue &&
     row.ExpectedE1Id.Value != local.Id) ||
    (row.ExpectedConnectedE1Id.HasValue &&
     row.ExpectedConnectedE1Id.Value != local.ConnectedE1Id))
            {
                throw new InvalidDataException(
                    $"Data row {row.DataRowNumber}: resources changed. " +
                    "Upload and validate the file again.");
            }

            if (remote == null ||
                remote.ConnectedE1Id != local.Id ||
                remote.LinkId != link.ConnectedLinkId ||
                remote.Link.ConnectedLinkId != link.Id ||
                remote.Link.SiteFromId != link.SiteToId ||
                remote.Link.SiteToId != link.SiteFromId ||
                local.StmId.HasValue != remote.StmId.HasValue)
            {
                throw new InvalidDataException(
                    $"Data row {row.DataRowNumber}: " +
                    "invalid opposite E1 pair.");
            }

            if (!endpointIds.Add(local.Id) ||
                !endpointIds.Add(remote.Id))
            {
                throw new InvalidDataException(
                    $"Data row {row.DataRowNumber}: " +
                    "an E1 pair is used twice.");
            }

            pairs.Add((link, local, remote));
        }

        var endpoints = pairs
            .SelectMany(pair => new[] { pair.Local, pair.Remote })
            .ToList();

        var ids = endpointIds.ToList();

        // Check installed STM resources at both ends of SDH links.
        var stmIds = endpoints
            .Where(e1 => e1.StmId.HasValue)
            .Select(e1 => e1.StmId!.Value)
            .Distinct()
            .ToList();

        var installed = await _context.MuxPorts
            .Where(port =>
                port.StmId.HasValue &&
                stmIds.Contains(port.StmId.Value) &&
                port.MuxCard.Category == CardCategory.STM)
            .Select(port => new
            {
                StmId = port.StmId!.Value,
                SiteId = port.MuxCard.Mux.SiteId
            })
            .ToListAsync(ct);

        foreach (var pair in pairs)
        {
            foreach (var e1 in new[] { pair.Local, pair.Remote })
            {
                if (e1.ConnectionType == E1ConnectionType.Logical)
                {
                    throw new InvalidDataException(
                        $"E1 {e1.E1Number} is not eligible " +
                        "for this cross-connection.");
                }

                string siteId = e1.Id == pair.Local.Id
                    ? pair.Link.SiteFromId
                    : pair.Link.SiteToId;

                if (e1.StmId.HasValue &&
                    (
                        e1.Stm == null ||
                        e1.Stm.LinkId != e1.LinkId ||
                        !installed.Any(port =>
                            port.StmId == e1.StmId.Value &&
                            port.SiteId == siteId)
                    ))
                {
                    throw new InvalidDataException(
                        $"Site {siteId}: the selected STM " +
                        "is not installed correctly.");
                }
            }
        }

        string customerName = rows[0].CustomerName;

        var customers = await _context.Customers
            .Where(item => item.Name == customerName)
            .Take(2)
            .ToListAsync(ct);

        if (customers.Count > 1 ||
            (customers.Count == 1 && !customers[0].IsActive))
        {
            throw new InvalidDataException(
                "The customer name is ambiguous " +
                "or the customer is inactive.");
        }

        // Detect an identical existing circuit.
        var existing = await _context.CustomerConnections
            .Where(connection =>
                connection.IsActive &&
                connection.Customer.Name == customerName &&
                connection.CommunicationPath.IsActive &&
                connection.CommunicationPath.Segments.Count == pairs.Count &&
                connection.Segments.Count == pairs.Count)
            .Include(connection => connection.Segments)
                .ThenInclude(segment =>
                    segment.CommunicationPathSegment)
            .ToListAsync(ct);

        var exactMatches = existing.Where(connection =>
        {
            var assignments = connection.Segments
                .OrderBy(segment =>
                    segment.CommunicationPathSegment.Order)
                .ToList();

            return assignments.Select((segment, i) =>
                segment.CommunicationPathSegment.Order == i + 1 &&
                segment.CommunicationPathSegment.CommunicationLinkId
                    == pairs[i].Link.Id &&
                segment.E1Id == pairs[i].Local.Id)
                .All(match => match);
        }).ToList();

        bool hasExternalJoin = await _context.E1s.AnyAsync(e1 =>
            !ids.Contains(e1.Id) &&
            e1.JoinE1Id.HasValue &&
            ids.Contains(e1.JoinE1Id.Value), ct);

        if (exactMatches.Count == 1 && !hasExternalJoin)
        {
            var connection = exactMatches[0];

            bool valid = endpoints.All(e1 =>
                e1.ConnectionGroupId == connection.ConnectionGroupId &&
                (
                    e1.Status == E1OperationalStatus.Connected ||
                    e1.Status == E1OperationalStatus.Wrong
                ));

            for (int i = 0; i < pairs.Count; i++)
            {
                Guid? previous = i == 0
                    ? null
                    : pairs[i - 1].Remote.Id;

                Guid? next = i == pairs.Count - 1
                    ? null
                    : pairs[i + 1].Local.Id;

                valid &= pairs[i].Local.JoinE1Id == previous &&
                         pairs[i].Remote.JoinE1Id == next;
            }

            if (!valid)
            {
                throw new InvalidDataException(
                    "An existing assignment is incomplete; " +
                    "repair it before importing.");
            }

            await transaction.CommitAsync(ct);

            // Already present; preserve its current endpoint statuses.
            return false;
        }

        // New circuits require available, unassigned endpoints.
        if (exactMatches.Count > 0 ||
            hasExternalJoin ||
            endpoints.Any(e1 =>
                e1.Status != E1OperationalStatus.Available ||
                e1.CrossConnectionState != E1CrossConnectionState.Available ||
                e1.ConnectionGroupId.HasValue ||
                e1.JoinE1Id.HasValue))
        {
            throw new InvalidDataException(
                "One or more selected E1 endpoints are already in use.");
        }

        if (await _context.CustomerConnectionSegments.AnyAsync(segment =>
                ids.Contains(segment.E1Id) ||
                (
                    segment.E1.ConnectedE1Id.HasValue &&
                    ids.Contains(segment.E1.ConnectedE1Id.Value)
                ), ct))
        {
            throw new InvalidDataException(
                "One or more E1s already have a customer assignment.");
        }

        // Reuse a route with the same ordered directional links.
        var linkIds = pairs
            .Select(pair => pair.Link.Id)
            .ToList();

        Guid firstLinkId = linkIds[0];

        var paths = await _context.CommunicationPaths
            .Where(route =>
                route.IsActive &&
                route.Segments.Count == pairs.Count &&
                route.Segments.Any(segment =>
                    segment.CommunicationLinkId == firstLinkId))
            .Include(route => route.Segments)
            .OrderBy(route => route.CreatedAt)
            .ThenBy(route => route.Id)
            .ToListAsync(ct);

        var path = paths.FirstOrDefault(candidate =>
            candidate.Segments
                .OrderBy(segment => segment.Order)
                .Select(segment => segment.CommunicationLinkId)
                .SequenceEqual(linkIds) &&
            candidate.Segments
                .OrderBy(segment => segment.Order)
                .Select(segment => segment.Order)
                .SequenceEqual(Enumerable.Range(1, pairs.Count)));

        if (path == null)
        {
            string name = string.Join(
                " → ",
                rows.Select(row => row.SiteFromId)
                    .Append(rows[^1].SiteToId));

            path = new CommunicationPath();

            string suffix = $" [{path.Id:N}]";

            path.Name =
                name[..Math.Min(name.Length, 200 - suffix.Length)]
                + suffix;

            for (int i = 0; i < pairs.Count; i++)
            {
                path.Segments.Add(new CommunicationPathSegment
                {
                    CommunicationPathId = path.Id,
                    CommunicationLinkId = pairs[i].Link.Id,
                    Order = i + 1
                });
            }

            _context.CommunicationPaths.Add(path);
        }

        var customer = customers.SingleOrDefault();

        if (customer == null)
        {
            customer = new Customer
            {
                Id = Guid.NewGuid(),
                Name = customerName
            };

            _context.Customers.Add(customer);
        }

        var newConnection = new CustomerConnection
        {
            CustomerId = customer.Id,
            CommunicationPathId = path.Id
        };

        _context.CustomerConnections.Add(newConnection);

        foreach (var e1 in endpoints)
        {
            e1.ConnectionGroupId = newConnection.ConnectionGroupId;
            e1.Status = E1OperationalStatus.Connected;
            e1.CrossConnectionState =
                E1CrossConnectionState.ExtendExistingPath;
        }

        var orderedSegments = path.Segments
            .OrderBy(segment => segment.Order)
            .ToList();

        for (int i = 0; i < pairs.Count; i++)
        {
            newConnection.Segments.Add(new CustomerConnectionSegment
            {
                CustomerConnectionId = newConnection.Id,
                CommunicationPathSegmentId = orderedSegments[i].Id,
                E1Id = pairs[i].Local.Id
            });

            if (i == 0)
                continue;

            var incoming = pairs[i - 1].Remote;
            var outgoing = pairs[i].Local;

            incoming.JoinE1Id = outgoing.Id;
            outgoing.JoinE1Id = incoming.Id;

            incoming.CrossConnectionState =
                E1CrossConnectionState.CrossConnected;

            outgoing.CrossConnectionState =
                E1CrossConnectionState.CrossConnected;
        }

        // Synchronize any LTU ports already assigned to these E1s.
        var ltuPorts = await _context.MuxPorts
            .Where(port =>
                port.E1Id.HasValue &&
                ids.Contains(port.E1Id.Value))
            .ToListAsync(ct);

        foreach (var port in ltuPorts)
        {
            port.Status = MuxPortStatus.Connected;
        }

        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return true;
    }
}