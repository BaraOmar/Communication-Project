using CommunicationProject.Data;
using CommunicationProject.Models;
using CommunicationProject.Security;
using CommunicationProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;
using static CommunicationProject.Models.E1;

namespace CommunicationProject.Controllers;


[Authorize(
    Roles = AppRoles.Admin + "," + AppRoles.Operator)]
public class CrossConnectionsController : Controller
{
    private readonly CommunicationDbContext _context;
    private readonly ILogger<CrossConnectionsController> _logger;

    public CrossConnectionsController(
        CommunicationDbContext context,
        ILogger<CrossConnectionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: CrossConnections/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CreateCrossConnectionViewModel();

        await LoadSiteOptionsAsync(model);

        return View(model);
    }

    /*
     * Returns only the sites directly connected to the selected
     * cross-connection site.
     *
     * Example:
     * selected site = B
     *
     * Returns all SiteTo values from:
     * B -> A
     * B -> C
     * B -> D
     */
    [HttpGet]
    public async Task<IActionResult> GetConnectedSites(string? siteId)
    {
        siteId = siteId?.Trim();

        if (string.IsNullOrWhiteSpace(siteId))
        {
            return Json(Array.Empty<object>());
        }

        bool siteExists = await _context.Sites
            .AsNoTracking()
            .AnyAsync(site => site.Id == siteId);

        if (!siteExists)
        {
            return Json(Array.Empty<object>());
        }

        var connectedSites = await _context.CommunicationLinks
            .AsNoTracking()
            .Where(link =>
                link.SiteFromId == siteId &&
                link.SiteToId != siteId)
            .Select(link => new
            {
                value = link.SiteToId,
                text = link.SiteTo.Id + " - " + link.SiteTo.Name
            })
            .Distinct()
            .OrderBy(option => option.text)
            .ToListAsync();

        return Json(connectedSites);
    }
    [HttpGet]
    public async Task<IActionResult> GetLinks(
    string? siteId,
    string? connectedSiteId)
    {
        siteId = siteId?.Trim();
        connectedSiteId = connectedSiteId?.Trim();

        if (string.IsNullOrWhiteSpace(siteId) ||
            string.IsNullOrWhiteSpace(connectedSiteId) ||
            string.Equals(
                siteId,
                connectedSiteId,
                StringComparison.OrdinalIgnoreCase))
        {
            return Json(Array.Empty<object>());
        }

        var links = await _context.CommunicationLinks
            .AsNoTracking()
            .Where(link =>
                link.SiteFromId == siteId &&
                link.SiteToId == connectedSiteId)
            .OrderBy(link => link.Name)
            .Select(link => new
            {
                value = link.Id,

                text =
                    link.Name +
                    " — " +
                    link.LinkType.Name,

                technology =
                    link.LinkType.Name
            })
            .ToListAsync();

        return Json(links);
    }
    /*
     * Returns STMs physically located in the selected central site
     * and belonging to the directional link toward the connected site.
     *
     * Example:
     * central site = B
     * connected site = A
     *
     * Uses the directional record:
     * B -> A
     */
    [HttpGet]
    public async Task<IActionResult> GetStms(
        Guid? linkId)
    {
        if (!linkId.HasValue ||
            linkId.Value == Guid.Empty)
        {
            return Json(Array.Empty<object>());
        }

        var stms = await _context.Stms
            .AsNoTracking()
            .Where(stm =>
                stm.LinkId == linkId.Value)
            .OrderBy(stm => stm.Number)
            .Select(stm => new
            {
                value = stm.Id,
                text = "STM " + stm.Number
            })
            .ToListAsync();

        return Json(stms);
    }

    /*
     * Returns E1 channels that are not already involved
     * in another cross connection.
     *
     * An E1 is unavailable when:
     *
     * 1. It already points to another E1 through JoinE1Id.
     * 2. Another E1 already points to it through JoinE1Id.
     */
    [HttpGet]
    public async Task<IActionResult> GetAvailableE1s(
    Guid? linkId,
    Guid? stmId,
    bool isIncoming = false)
    {
        if (!linkId.HasValue ||
            linkId.Value == Guid.Empty)
        {
            return Json(Array.Empty<object>());
        }

        var baseQuery = _context.E1s
            .AsNoTracking()
            .Where(e1 =>
                e1.LinkId == linkId.Value &&
                (
                    stmId.HasValue
                        ? e1.StmId == stmId.Value
                        : e1.StmId == null
                ) &&
                e1.ConnectedE1Id != null &&
                e1.ConnectionType !=
    E1ConnectionType.Logical &&

e1.ConnectedE1!.ConnectionType !=
    E1ConnectionType.Logical);



        IQueryable<E1> query;

        if (isIncoming)
        {
            /*
             * Incoming:
             *
             * Available             -> creates a new path.
             * ExtendExistingPath    -> extends an existing path.
             * CrossConnected        -> never displayed.
             */
            query = baseQuery.Where(e1 =>
                /*
                 * Normal available outgoing E1.
                 */
                (
                    e1.CrossConnectionState ==
                        E1CrossConnectionState.Available &&

                    e1.Status ==
                        E1OperationalStatus.Available &&

                    e1.ConnectionGroupId == null &&
                    e1.JoinE1Id == null &&

                    e1.ConnectedE1!.CrossConnectionState ==
                        E1CrossConnectionState.Available &&

                    e1.ConnectedE1.Status ==
                        E1OperationalStatus.Available &&

                    e1.ConnectedE1.ConnectionGroupId == null &&
                    e1.ConnectedE1.JoinE1Id == null
                )
                ||
                /*
                 * Open endpoint of an existing customer path.
                 */
                (
                    e1.CrossConnectionState ==
                        E1CrossConnectionState.ExtendExistingPath &&

                    e1.Status ==
                        E1OperationalStatus.Connected &&

                    e1.ConnectionGroupId != null &&
                    e1.JoinE1Id == null &&

                    _context.CustomerConnectionSegments
                        .Any(customerSegment =>
                            customerSegment.CustomerConnection.ConnectionGroupId ==
                                e1.ConnectionGroupId &&

                            (
                                customerSegment.E1Id == e1.Id ||
                                customerSegment.E1.ConnectedE1Id ==
                                    e1.Id
                            ))
                )
            );
        }
        else
        {
            /*
             * Outgoing:
             * available physical E1s or open endpoints
             * of existing customer paths.
             */
            query = baseQuery.Where(e1 =>
                (
                    e1.CrossConnectionState ==
                        E1CrossConnectionState.Available &&

                    e1.Status ==
                        E1OperationalStatus.Available &&

                    e1.ConnectionGroupId == null &&
                    e1.JoinE1Id == null &&

                    e1.ConnectedE1!.CrossConnectionState ==
                        E1CrossConnectionState.Available &&

                    e1.ConnectedE1.Status ==
                        E1OperationalStatus.Available &&

                    e1.ConnectedE1.ConnectionGroupId == null &&
                    e1.ConnectedE1.JoinE1Id == null
                )
                ||
                (
                    e1.CrossConnectionState ==
                        E1CrossConnectionState.ExtendExistingPath &&

                    e1.Status ==
                        E1OperationalStatus.Connected &&

                    e1.ConnectionGroupId != null &&
                    e1.JoinE1Id == null &&

                    _context.CustomerConnectionSegments
                        .Any(customerSegment =>
                            customerSegment.CustomerConnection.ConnectionGroupId ==
                                e1.ConnectionGroupId &&

                            (
                                customerSegment.E1Id == e1.Id ||
                                customerSegment.E1.ConnectedE1Id == e1.Id
                            ))
                )
            );
        }

        var options = await query
            .OrderBy(e1 => e1.E1Number)
            .Select(e1 => new
            {
                value = e1.Id,

                text =
                    e1.CrossConnectionState ==
                        E1CrossConnectionState.ExtendExistingPath

                        ? $"E1 {e1.E1Number} — {e1.ConnectionType} — Extend existing path"
                        : $"E1 {e1.E1Number} — {e1.ConnectionType} — Available",

                state = e1.CrossConnectionState.ToString(),


                customerName =
                    e1.ConnectionGroupId.HasValue
                        ? _context.CustomerConnections
                            .Where(connection =>
                                connection.ConnectionGroupId ==
                                e1.ConnectionGroupId.Value)
                            .Select(connection =>
                                connection.Customer.Name)
                            .FirstOrDefault()
                        : null
            })
            .ToListAsync();

        return Json(options);
    }
    // POST: CrossConnections/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateCrossConnectionViewModel model)
    {
        NormalizeModel(model);

        ModelState.Clear();
        TryValidateModel(model);

        ValidateBasicSelections(model);

        if (!ModelState.IsValid)
        {
            await LoadSiteOptionsAsync(model);
            return View(model);
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var incomingE1 = await _context.E1s
                .Include(e1 => e1.Link)
                .Include(e1 => e1.Stm)

                .Include(e1 => e1.ConnectedE1)
                    .ThenInclude(connectedE1 =>
                        connectedE1!.Link)

                .Include(e1 => e1.ConnectedE1)
                    .ThenInclude(connectedE1 =>
                        connectedE1!.Stm)

                .FirstOrDefaultAsync(e1 =>
                    e1.Id == model.IncomingE1Id!.Value);

            var outgoingE1 = await _context.E1s
                .Include(e1 => e1.Link)
                .Include(e1 => e1.Stm)

                .Include(e1 => e1.ConnectedE1)
                    .ThenInclude(connectedE1 =>
                        connectedE1!.Link)

                .Include(e1 => e1.ConnectedE1)
                    .ThenInclude(connectedE1 =>
                        connectedE1!.Stm)

                .FirstOrDefaultAsync(e1 =>
                    e1.Id == model.OutgoingE1Id!.Value);


            ValidateIncomingE1(model, incomingE1);
            ValidateOutgoingE1(model, outgoingE1);
            if (incomingE1 != null &&
                incomingE1.CrossConnectionState !=
                    E1CrossConnectionState.Available &&
                incomingE1.CrossConnectionState !=
                    E1CrossConnectionState.ExtendExistingPath)
            {
                ModelState.AddModelError(
                    nameof(model.IncomingE1Id),
                    "The incoming E1 must be available or an existing path endpoint.");
            }

            if (outgoingE1 != null &&
                outgoingE1.CrossConnectionState !=
                    E1CrossConnectionState.Available &&
                outgoingE1.CrossConnectionState !=
                    E1CrossConnectionState.ExtendExistingPath)
            {
                ModelState.AddModelError(
                    nameof(model.OutgoingE1Id),
                    "The outgoing E1 must be available or an existing path endpoint.");
            }

            if (outgoingE1?.ConnectedE1 != null &&
                outgoingE1.CrossConnectionState ==
                    E1CrossConnectionState.Available &&
                outgoingE1.ConnectedE1.CrossConnectionState !=
                    E1CrossConnectionState.Available)
            {
                ModelState.AddModelError(
                    nameof(model.OutgoingE1Id),
                    "The connected endpoint of the outgoing E1 is not available.");
            }

            if (incomingE1?.ConnectedE1 == null)
            {
                ModelState.AddModelError(
                    nameof(model.IncomingE1Id),
                    "The incoming E1 does not have a connected endpoint at the previous site.");
            }

            if (outgoingE1?.ConnectedE1 == null)
            {
                ModelState.AddModelError(
                    nameof(model.OutgoingE1Id),
                    "The outgoing E1 does not have a connected endpoint at the next site.");
            }

            if (incomingE1 != null &&
                outgoingE1 != null &&
                incomingE1.ConnectedE1 != null &&
                outgoingE1.ConnectedE1 != null)
            {
                await ValidateCrossConnectionAvailabilityAsync(
                    incomingE1,
                    outgoingE1);

                await ValidatePathAvailabilityAsync(
                    incomingE1,
                    outgoingE1);
            }
            bool isExtendingExistingPath =
                incomingE1 != null &&
                incomingE1.CrossConnectionState ==
                    E1CrossConnectionState.ExtendExistingPath &&
                incomingE1.ConnectionGroupId.HasValue;

            bool outgoingIsExistingEndpoint =
    outgoingE1 != null &&
    outgoingE1.CrossConnectionState ==
        E1CrossConnectionState.ExtendExistingPath &&
    outgoingE1.ConnectionGroupId.HasValue;

            bool isMergingExistingPaths =
                isExtendingExistingPath &&
                outgoingIsExistingEndpoint;

            /*
             * An existing outgoing endpoint can only be joined
             * when the incoming side is also an existing endpoint.
             */
            if (outgoingIsExistingEndpoint &&
                !isExtendingExistingPath)
            {
                ModelState.AddModelError(
                    nameof(model.OutgoingE1Id),
                    "To merge paths, both selected E1s must be existing path endpoints.");
            }
            Guid? incomingEndpointConnectionId = null;
            Guid? outgoingEndpointConnectionId = null;
            Guid? outgoingEndpointPathId = null;
            Guid? outgoingEndpointGroupId = null;
            if (isMergingExistingPaths)
            {
                var endpointOwners =
                    await _context.CustomerConnections
                        .AsNoTracking()
                        .Where(connection =>
                            connection.ConnectionGroupId ==
                                incomingE1!.ConnectionGroupId!.Value ||

                            connection.ConnectionGroupId ==
                                outgoingE1!.ConnectionGroupId!.Value)
.Select(connection => new
{
    connection.Id,
    connection.CustomerId,
    connection.ConnectionGroupId,
    connection.CommunicationPathId
})
                        .ToListAsync();

                var incomingOwner =
                    endpointOwners.FirstOrDefault(connection =>
                        connection.ConnectionGroupId ==
                            incomingE1!.ConnectionGroupId!.Value);

                var outgoingOwner =
                    endpointOwners.FirstOrDefault(connection =>
                        connection.ConnectionGroupId ==
                            outgoingE1!.ConnectionGroupId!.Value);

                if (incomingOwner == null ||
                    outgoingOwner == null)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "One of the selected customer paths could not be found.");
                }
                else if (incomingOwner.CustomerId !=
                         outgoingOwner.CustomerId)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Paths belonging to different customers cannot be merged.");
                }

                else
                {
                    incomingEndpointConnectionId =
                        incomingOwner.Id;

                    outgoingEndpointConnectionId =
                        outgoingOwner.Id;

                    outgoingEndpointPathId =
                        outgoingOwner.CommunicationPathId;

                    outgoingEndpointGroupId =
    outgoingOwner.ConnectionGroupId;

                    if (incomingOwner.Id != outgoingOwner.Id)
                    {
                        var endpointPositions =
                            await _context.CustomerConnectionSegments
                                .Where(segment =>
                                    segment.CustomerConnectionId ==
                                        incomingOwner.Id ||

                                    segment.CustomerConnectionId ==
                                        outgoingOwner.Id)
                                .Select(segment => new
                                {
                                    segment.CustomerConnectionId,
                                    segment.E1Id,
                                    ConnectedE1Id =
                                        segment.E1.ConnectedE1Id,

                                    Order =
                                        segment.CommunicationPathSegment.Order
                                })
                                .ToListAsync();

                        var incomingPositions =
                            endpointPositions
                                .Where(position =>
                                    position.CustomerConnectionId ==
                                        incomingOwner.Id)
                                .ToList();

                        var outgoingPositions =
                            endpointPositions
                                .Where(position =>
                                    position.CustomerConnectionId ==
                                        outgoingOwner.Id)
                                .ToList();

                        var incomingPosition =
                            incomingPositions.FirstOrDefault(position =>
                                position.E1Id == incomingE1!.Id ||
                                position.ConnectedE1Id == incomingE1.Id);

                        var outgoingPosition =
                            outgoingPositions.FirstOrDefault(position =>
                                position.E1Id == outgoingE1!.Id ||
                                position.ConnectedE1Id == outgoingE1.Id);

                        bool incomingIsEdge =
                            incomingPosition != null &&
                            (
                                (
                                    incomingPosition.ConnectedE1Id ==
                                        incomingE1!.Id &&

                                    incomingPosition.Order ==
                                        incomingPositions.Max(position =>
                                            position.Order)
                                )
                                ||
                                (
                                    incomingPosition.E1Id ==
                                        incomingE1!.Id &&

                                    incomingPosition.Order ==
                                        incomingPositions.Min(position =>
                                            position.Order)
                                )
                            );

                        bool outgoingIsEdge =
                            outgoingPosition != null &&
                            (
                                (
                                    outgoingPosition.E1Id ==
                                        outgoingE1!.Id &&

                                    outgoingPosition.Order ==
                                        outgoingPositions.Min(position =>
                                            position.Order)
                                )
                                ||
                                (
                                    outgoingPosition.ConnectedE1Id ==
                                        outgoingE1!.Id &&

                                    outgoingPosition.Order ==
                                        outgoingPositions.Max(position =>
                                            position.Order)
                                )
                            );

                        if (!incomingIsEdge ||
                            !outgoingIsEdge)
                        {
                            ModelState.AddModelError(
                                string.Empty,
                                "Two separate customer paths can only be merged at their open end points.");
                        }
                    }
                }

            }

            Guid? existingCommunicationPathId = null;
            Guid? existingCustomerConnectionId = null;

            string customerName =
                model.CustomerName?.Trim() ?? string.Empty;

            if (isExtendingExistingPath)
            {
                if (!incomingE1!.ConnectionGroupId.HasValue)
                {
                    ModelState.AddModelError(
                        nameof(model.CustomerName),
                        "The existing connection does not have a customer connection group.");
                }
                else
                {
                    var existingConnection =
                        await _context.CustomerConnections
                            .AsNoTracking()
                            .Where(connection =>
                                connection.ConnectionGroupId ==
                                incomingE1.ConnectionGroupId.Value)
.Select(connection => new
{
    CustomerConnectionId = connection.Id,
    CustomerName = connection.Customer.Name,
    connection.CommunicationPathId
})
                            .FirstOrDefaultAsync();

                    if (existingConnection == null ||
                        string.IsNullOrWhiteSpace(existingConnection.CustomerName))
                    {
                        ModelState.AddModelError(
                            nameof(model.CustomerName),
                            "The customer for this existing connection could not be found.");
                    }
                    else
                    {
                        customerName =
                            existingConnection.CustomerName;

                        model.CustomerName =
                            existingConnection.CustomerName;

                        existingCustomerConnectionId =
                            existingConnection.CustomerConnectionId;



                        existingCommunicationPathId =
                            existingConnection.CommunicationPathId;

                        ModelState.Remove(
                            nameof(model.CustomerName));
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(customerName))
            {
                ModelState.AddModelError(
                    nameof(model.CustomerName),
                    "Enter the customer name.");
            }

            if (!ModelState.IsValid)
            {
                await transaction.RollbackAsync();
                await LoadSiteOptionsAsync(model);

                return View(model);
            }

            var customer =
    await _context.Customers
        .FirstOrDefaultAsync(c =>
            c.Name == customerName);

            if (customer == null)
            {
                customer = new Customer
                {
                    Id = Guid.NewGuid(),
                    Name = customerName,
                    IsActive = true
                };

                _context.Customers.Add(customer);
            }

            var incomingConnectedE1 =
                incomingE1!.ConnectedE1!;

            var outgoingConnectedE1 =
                outgoingE1!.ConnectedE1!;


            Guid connectionGroupId;

            if (isExtendingExistingPath &&
                incomingE1.ConnectionGroupId.HasValue)
            {
                connectionGroupId =
                    incomingE1.ConnectionGroupId.Value;
            }
            else
            {
                connectionGroupId =
                    Guid.NewGuid();
            }

            incomingConnectedE1.ConnectionGroupId =
                connectionGroupId;

            incomingE1.ConnectionGroupId =
                connectionGroupId;

            outgoingE1.ConnectionGroupId =
                connectionGroupId;

            outgoingConnectedE1.ConnectionGroupId =
                connectionGroupId;


            incomingConnectedE1.Status =
    E1OperationalStatus.Connected;

            incomingE1.Status =
                E1OperationalStatus.Connected;

            outgoingE1.Status =
                E1OperationalStatus.Connected;

            outgoingConnectedE1.Status =
                E1OperationalStatus.Connected;
            /*
             * Existing cross-connection logic continues.
             */
            incomingE1.JoinE1Id = outgoingE1.Id;
            outgoingE1.JoinE1Id = incomingE1.Id;

            incomingE1.CrossConnectionState =
                E1CrossConnectionState.CrossConnected;

            outgoingE1.CrossConnectionState =
                E1CrossConnectionState.CrossConnected;

            var customerSegmentAssignments =
    new List<(CommunicationPathSegment Segment, E1 E1)>();

            Guid pathId;

            if (isMergingExistingPaths &&
                incomingEndpointConnectionId ==
                    outgoingEndpointConnectionId)
            {
                /*
                 * Reconnect two sections of the same customer connection.
                 * Their path segments already exist.
                 */
                pathId =
                    existingCommunicationPathId!.Value;
            }

            else if (isMergingExistingPaths)
            {
                /*
                 * Two separate CustomerConnections belonging
                 * to the same customer are being joined.
                 */
                pathId =
                    existingCommunicationPathId!.Value;

                var outgoingConnectionToMerge =
                    await _context.CustomerConnections
                        .FirstAsync(connection =>
                            connection.Id ==
                                outgoingEndpointConnectionId!.Value);

                var outgoingSegmentsToMerge =
                    await _context.CustomerConnectionSegments
                        .Where(segment =>
                            segment.CustomerConnectionId ==
                                outgoingEndpointConnectionId.Value)
                        .Include(segment =>
                            segment.CommunicationPathSegment)
                        .Include(segment =>
                            segment.E1)
                            .ThenInclude(e1 =>
                                e1.ConnectedE1)
                        .OrderBy(segment =>
                            segment.CommunicationPathSegment.Order)
                        .ToListAsync();

                int minimumOutgoingOrder =
                    outgoingSegmentsToMerge.Min(segment =>
                        segment.CommunicationPathSegment.Order);

                bool mergeInOriginalDirection =
                    outgoingSegmentsToMerge.Any(segment =>
                        segment.CommunicationPathSegment.Order ==
                            minimumOutgoingOrder &&

                        segment.E1Id ==
                            outgoingE1.Id);

                IEnumerable<CustomerConnectionSegment>
                    orderedOutgoingSegments =
                        mergeInOriginalDirection
                            ? outgoingSegmentsToMerge
                                .OrderBy(segment =>
                                    segment.CommunicationPathSegment.Order)

                            : outgoingSegmentsToMerge
                                .OrderByDescending(segment =>
                                    segment.CommunicationPathSegment.Order);

                int nextOrder =
                    await _context.CommunicationPathSegments
                        .Where(segment =>
                            segment.CommunicationPathId == pathId)
                        .MaxAsync(segment =>
                            (int?)segment.Order)
                    ?? 0;

                foreach (var outgoingSegment in
                         orderedOutgoingSegments)
                {
                    E1 mergedE1;

                    if (mergeInOriginalDirection)
                    {
                        mergedE1 =
                            outgoingSegment.E1;
                    }
                    else
                    {
                        mergedE1 =
                            outgoingSegment.E1.ConnectedE1
                            ?? throw new InvalidOperationException(
                                "The reverse E1 endpoint could not be found.");
                    }

                    nextOrder++;

                    var mergedPathSegment =
                        new CommunicationPathSegment
                        {
                            Id = Guid.NewGuid(),
                            CommunicationPathId = pathId,
                            CommunicationLinkId = mergedE1.LinkId,
                            Order = nextOrder
                        };

                    _context.CommunicationPathSegments.Add(
                        mergedPathSegment);

                    outgoingSegment.CustomerConnectionId =
                        incomingEndpointConnectionId!.Value;

                    outgoingSegment.CommunicationPathSegmentId =
                        mergedPathSegment.Id;

                    outgoingSegment.CommunicationPathSegment =
                        mergedPathSegment;

                    outgoingSegment.E1Id =
                        mergedE1.Id;

                    outgoingSegment.E1 =
                        mergedE1;
                }

                /*
                 * Move every E1 from the outgoing connection group
                 * into the incoming connection group.
                 */
                var outgoingGroupE1s =
                    await _context.E1s
                        .Where(e1 =>
                            e1.ConnectionGroupId ==
                                outgoingEndpointGroupId!.Value)
                        .ToListAsync();

                foreach (var groupE1 in outgoingGroupE1s)
                {
                    groupE1.ConnectionGroupId =
                        connectionGroupId;
                }

                _context.CustomerConnections.Remove(
                    outgoingConnectionToMerge);
            }


            else if (isExtendingExistingPath)
            {
                /*
                 * Only an existing path has a PathId.
                 */
                pathId = existingCommunicationPathId!.Value;

                int? incomingSegmentOrder =
    await _context.CustomerConnectionSegments
        .Where(customerSegment =>
            customerSegment.CustomerConnectionId ==
                existingCustomerConnectionId!.Value &&

            customerSegment.E1.ConnectedE1Id ==
                incomingE1.Id)
        .Select(customerSegment =>
            (int?)customerSegment
                .CommunicationPathSegment
                .Order)
        .FirstOrDefaultAsync();

                CommunicationPathSegment? extendedSegment = null;

                /*
                 * If the next segment already exists in the path but was
                 * released by this customer, reuse it in its original order.
                 */
                if (incomingSegmentOrder.HasValue)
                {
                    extendedSegment =
                        await _context.CommunicationPathSegments
                            .FirstOrDefaultAsync(segment =>
                                segment.CommunicationPathId == pathId &&

                                segment.Order ==
                                    incomingSegmentOrder.Value + 1 &&

                                segment.CommunicationLinkId ==
                                    outgoingE1.LinkId &&

                                !_context.CustomerConnectionSegments
                                    .Any(customerSegment =>
                                        customerSegment.CustomerConnectionId ==
                                            existingCustomerConnectionId.Value &&

                                        customerSegment.CommunicationPathSegmentId ==
                                            segment.Id));
                }

                /*
                 * No released segment was found, so this is a normal
                 * extension at the end of the path.
                 */
                if (extendedSegment == null)
                {
                    int maximumSegmentOrder =
                        await _context.CommunicationPathSegments
                            .Where(segment =>
                                segment.CommunicationPathId == pathId)
                            .MaxAsync(segment =>
                                (int?)segment.Order)
                        ?? 0;

                    extendedSegment =
                        new CommunicationPathSegment
                        {
                            Id = Guid.NewGuid(),
                            CommunicationPathId = pathId,
                            CommunicationLinkId =
                                outgoingE1.LinkId,
                            Order = maximumSegmentOrder + 1
                        };

                    _context.CommunicationPathSegments.Add(
                        extendedSegment);
                }

                customerSegmentAssignments.Add(
                    (extendedSegment, outgoingE1));


                outgoingConnectedE1.CrossConnectionState =
                    E1CrossConnectionState.ExtendExistingPath;


            }
            else
            {
                /*
                 * Available Incoming E1:
                 * create a new PathId.
                 */
                pathId = Guid.NewGuid();

                _context.CommunicationPaths.Add(
    new CommunicationPath
    {
        Id = pathId,
        Name = $"PATH-{pathId}",
        IsActive = true,
        CreatedAt = DateTime.Now
    });

                var firstSegment =
                    new CommunicationPathSegment
                    {
                        Id = Guid.NewGuid(),
                        CommunicationPathId = pathId,
                        CommunicationLinkId =
                            incomingConnectedE1.LinkId,
                        Order = 1
                    };

                var secondSegment =
                    new CommunicationPathSegment
                    {
                        Id = Guid.NewGuid(),
                        CommunicationPathId = pathId,
                        CommunicationLinkId =
                            outgoingE1.LinkId,
                        Order = 2
                    };

                _context.CommunicationPathSegments.AddRange(
                    firstSegment,
                    secondSegment);

                customerSegmentAssignments.Add(
                    (firstSegment, incomingConnectedE1));

                customerSegmentAssignments.Add(
                    (secondSegment, outgoingE1));

                incomingConnectedE1.CrossConnectionState =
                    E1CrossConnectionState.ExtendExistingPath;

                outgoingConnectedE1.CrossConnectionState =
                    E1CrossConnectionState.ExtendExistingPath;


            }


            var customerConnection =
    await _context.CustomerConnections
        .FirstOrDefaultAsync(connection =>
            connection.ConnectionGroupId ==
            connectionGroupId);

            if (customerConnection == null)
            {
                customerConnection =
                    new CustomerConnection
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customer.Id,
                        CommunicationPathId = pathId,
                        ConnectionGroupId = connectionGroupId,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                _context.CustomerConnections.Add(
                    customerConnection);
            }
            foreach (var assignment in customerSegmentAssignments)
            {
                _context.CustomerConnectionSegments.Add(
                    new CustomerConnectionSegment
                    {
                        Id = Guid.NewGuid(),
                        CustomerConnectionId =
                            customerConnection.Id,

                        CommunicationPathSegmentId =
                            assignment.Segment.Id,

                        E1Id =
                            assignment.E1.Id
                    });
            }
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                isExtendingExistingPath
                    ? $"Path extended successfully for customer {customerName}. " +
                      $"Path ID: {pathId}."
                    : $"New path created successfully for customer {customerName}. " +
                      $"Path ID: {pathId}.";

            return RedirectToAction(nameof(Create));
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync();

            _logger.LogError(
                exception,
                "Database error while creating cross connection " +
                "from E1 {IncomingE1Id} to E1 {OutgoingE1Id}.",
                model.IncomingE1Id,
                model.OutgoingE1Id);

            ModelState.AddModelError(
                string.Empty,
                "The cross connection could not be saved. " +
                "One of the selected E1 channels may already be in use.");
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();

            _logger.LogError(
                exception,
                "Unexpected error while creating cross connection.");

            ModelState.AddModelError(
                string.Empty,
                "An unexpected error occurred. " +
                "The cross connection was not created.");
        }

        await LoadSiteOptionsAsync(model);

        return View(model);
    }
    private static void NormalizeModel(
        CreateCrossConnectionViewModel model)
    {
        model.SiteId =
            model.SiteId?.Trim() ?? string.Empty;

        model.PreviousSiteId =
            model.PreviousSiteId?.Trim() ?? string.Empty;

        model.NextSiteId =
            model.NextSiteId?.Trim() ?? string.Empty;
    }

    private void ValidateBasicSelections(
        CreateCrossConnectionViewModel model)
    {
        if (string.Equals(
                model.SiteId,
                model.PreviousSiteId,
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(model.PreviousSiteId),
                "The previous site cannot be the cross-connection site.");
        }

        if (string.Equals(
                model.SiteId,
                model.NextSiteId,
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(model.NextSiteId),
                "The next site cannot be the cross-connection site.");
        }

        if (!string.IsNullOrWhiteSpace(model.PreviousSiteId) &&
            string.Equals(
                model.PreviousSiteId,
                model.NextSiteId,
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(model.NextSiteId),
                "The previous site and next site must be different.");
        }

        if (model.IncomingStmId.HasValue &&
            model.OutgoingStmId.HasValue &&
            model.IncomingStmId.Value ==
            model.OutgoingStmId.Value)
        {
            ModelState.AddModelError(
                nameof(model.OutgoingStmId),
                "Incoming STM and outgoing STM must be different.");
        }

        if (model.IncomingE1Id.HasValue &&
            model.OutgoingE1Id.HasValue &&
            model.IncomingE1Id.Value ==
            model.OutgoingE1Id.Value)
        {
            ModelState.AddModelError(
                nameof(model.OutgoingE1Id),
                "Incoming E1 and outgoing E1 must be different.");
        }
        if (model.IncomingLinkId.HasValue &&
    model.OutgoingLinkId.HasValue &&
    model.IncomingLinkId.Value ==
    model.OutgoingLinkId.Value)
        {
            ModelState.AddModelError(
                nameof(model.OutgoingLinkId),
                "Incoming and outgoing links must be different.");
        }
    }

    private void ValidateIncomingE1(
    CreateCrossConnectionViewModel model,
    E1? incomingE1)
    {
        if (incomingE1 is null)
        {
            ModelState.AddModelError(
                nameof(model.IncomingE1Id),
                "The selected incoming E1 does not exist.");

            return;
        }

        if (incomingE1.LinkId !=
            model.IncomingLinkId)
        {
            ModelState.AddModelError(
                nameof(model.IncomingE1Id),
                "The selected incoming E1 does not belong " +
                "to the selected incoming link.");
        }

        if (incomingE1.StmId !=
            model.IncomingStmId)
        {
            ModelState.AddModelError(
                nameof(model.IncomingE1Id),
                "The selected incoming E1 does not belong " +
                "to the selected STM or direct PDH link.");
        }

        if (!string.Equals(
                incomingE1.Link.SiteFromId,
                model.SiteId,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                incomingE1.Link.SiteToId,
                model.PreviousSiteId,
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(model.IncomingE1Id),
                "The incoming E1 does not belong to the connection " +
                "between the selected site and previous site.");
        }
        if (incomingE1.ConnectionType ==
        E1ConnectionType.Logical ||
    incomingE1.ConnectedE1?.ConnectionType ==
        E1ConnectionType.Logical)
        {
            ModelState.AddModelError(
                nameof(model.IncomingE1Id),
                "Logical E1s cannot be used in cross connections.");
        }
    }

    private void ValidateOutgoingE1(
        CreateCrossConnectionViewModel model,
        E1? outgoingE1)
    {
        if (outgoingE1 is null)
        {
            ModelState.AddModelError(
                nameof(model.OutgoingE1Id),
                "The selected outgoing E1 does not exist.");

            return;
        }

        if (outgoingE1.LinkId !=
            model.OutgoingLinkId)
        {
            ModelState.AddModelError(
                nameof(model.OutgoingE1Id),
                "The selected outgoing E1 does not belong " +
                "to the selected outgoing link.");
        }


        if (outgoingE1.StmId !=
            model.OutgoingStmId)
        {
            ModelState.AddModelError(
                nameof(model.OutgoingE1Id),
                "The selected outgoing E1 does not belong " +
                "to the selected STM or direct PDH link.");
        }

        if (!string.Equals(
                outgoingE1.Link.SiteFromId,
                model.SiteId,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                outgoingE1.Link.SiteToId,
                model.NextSiteId,
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(model.OutgoingE1Id),
                "The outgoing E1 does not belong to the connection " +
                "between the selected site and next site.");
        }

    }

    private async Task ValidateCrossConnectionAvailabilityAsync(
        E1 incomingE1,
        E1 outgoingE1)
    {
        if (incomingE1.Id == outgoingE1.Id)
        {
            ModelState.AddModelError(
                string.Empty,
                "An E1 channel cannot be joined to itself.");

            return;
        }

        if (incomingE1.JoinE1Id.HasValue)
        {
            ModelState.AddModelError(
                nameof(CreateCrossConnectionViewModel.IncomingE1Id),
                $"Incoming E1 '{incomingE1.E1Number}' " +
                "already has a cross connection.");
        }

        if (outgoingE1.JoinE1Id.HasValue)
        {
            ModelState.AddModelError(
                nameof(CreateCrossConnectionViewModel.OutgoingE1Id),
                $"Outgoing E1 '{outgoingE1.E1Number}' " +
                "already has a cross connection.");
        }

        bool incomingIsAlreadyTarget =
            await _context.E1s.AnyAsync(e1 =>
                e1.JoinE1Id == incomingE1.Id);

        if (incomingIsAlreadyTarget)
        {
            ModelState.AddModelError(
                nameof(CreateCrossConnectionViewModel.IncomingE1Id),
                $"Incoming E1 '{incomingE1.E1Number}' " +
                "is already used by another cross connection.");
        }

        bool outgoingIsAlreadyTarget =
            await _context.E1s.AnyAsync(e1 =>
                e1.JoinE1Id == outgoingE1.Id);

        if (outgoingIsAlreadyTarget)
        {
            ModelState.AddModelError(
                nameof(CreateCrossConnectionViewModel.OutgoingE1Id),
                $"Outgoing E1 '{outgoingE1.E1Number}' " +
                "is already used by another cross connection.");
        }
    }

    private async Task LoadSiteOptionsAsync(
        CreateCrossConnectionViewModel model)
    {
        var sites = await _context.Sites
            .AsNoTracking()
            .OrderBy(site => site.Name)
            .Select(site => new SelectListItem
            {
                Value = site.Id,
                Text = site.Id + " - " + site.Name
            })
            .ToListAsync();

        model.SiteOptions = sites;
    }
    private async Task ValidatePathAvailabilityAsync(
     E1 incomingE1,
     E1 outgoingE1)
    {
        var incomingConnectedE1 =
            incomingE1.ConnectedE1;

        var outgoingConnectedE1 =
            outgoingE1.ConnectedE1;

        /*
         * Every selected E1 must already have its physical
         * connection across the communication link.
         */
        if (incomingConnectedE1 == null)
        {
            ModelState.AddModelError(
                nameof(CreateCrossConnectionViewModel.IncomingE1Id),
                "The incoming E1 does not have a connected E1.");

            return;
        }

        if (outgoingConnectedE1 == null)
        {
            ModelState.AddModelError(
                nameof(CreateCrossConnectionViewModel.OutgoingE1Id),
                "The outgoing E1 does not have a connected E1.");

            return;
        }

        /*
         * ConnectedE1 must be reciprocal.
         */
        if (incomingConnectedE1.ConnectedE1Id != incomingE1.Id)
        {
            ModelState.AddModelError(
                nameof(CreateCrossConnectionViewModel.IncomingE1Id),
                "The incoming E1 physical connection is not reciprocal.");

            return;
        }

        if (outgoingConnectedE1.ConnectedE1Id != outgoingE1.Id)
        {
            ModelState.AddModelError(
                nameof(CreateCrossConnectionViewModel.OutgoingE1Id),
                "The outgoing E1 physical connection is not reciprocal.");

            return;
        }

        /*
         * The operation must involve four different E1 records:
         *
         * 1. Previous site's remote E1.
         * 2. Incoming E1 at the central site.
         * 3. Outgoing E1 at the central site.
         * 4. Next site's remote E1.
         */
        var allIds = new[]
        {
        incomingE1.Id,
        incomingConnectedE1.Id,
        outgoingE1.Id,
        outgoingConnectedE1.Id
    };

        if (allIds.Distinct().Count() != 4)
        {
            ModelState.AddModelError(
                string.Empty,
                "The selected E1 records do not form a valid connection.");

            return;
        }

        /*
         * The outgoing physical E1 pair must always be available.
         *
         * This applies when creating a new path and when
         * extending an existing path.
         */
        bool outgoingPairHasStoredUsage =
            outgoingE1.CrossConnectionState !=
                E1CrossConnectionState.Available ||

            outgoingConnectedE1.CrossConnectionState !=
                E1CrossConnectionState.Available ||


            outgoingE1.JoinE1Id.HasValue ||

            outgoingConnectedE1.JoinE1Id.HasValue;

        bool outgoingPairIsJoinTarget =
            await _context.E1s.AnyAsync(e1 =>
                e1.JoinE1Id == outgoingE1.Id ||
                e1.JoinE1Id == outgoingConnectedE1.Id);

        if (outgoingE1.CrossConnectionState ==
                E1CrossConnectionState.Available &&
            (outgoingPairHasStoredUsage ||
             outgoingPairIsJoinTarget))
        {
            ModelState.AddModelError(
                nameof(CreateCrossConnectionViewModel.OutgoingE1Id),
                "The outgoing E1 or its connected E1 is already in use.");
        }

        /*
         * Case 1:
         * Incoming E1 is Available, so create a new path.
         */
        if (incomingE1.CrossConnectionState ==
            E1CrossConnectionState.Available)
        {
            bool incomingPairHasStoredUsage =
                incomingConnectedE1.CrossConnectionState !=
                    E1CrossConnectionState.Available ||


                incomingE1.JoinE1Id.HasValue ||

                incomingConnectedE1.JoinE1Id.HasValue;

            bool incomingPairIsJoinTarget =
                await _context.E1s.AnyAsync(e1 =>
                    e1.JoinE1Id == incomingE1.Id ||
                    e1.JoinE1Id == incomingConnectedE1.Id);

            if (incomingPairHasStoredUsage ||
                incomingPairIsJoinTarget)
            {
                ModelState.AddModelError(
                    nameof(CreateCrossConnectionViewModel.IncomingE1Id),
                    "The incoming E1 or its connected E1 is already in use.");
            }

            /*
             * No more existing-path validation is needed.
             */
            return;
        }

        /*
         * Case 2:
         * Incoming E1 is an endpoint of an existing path.
         */
        if (incomingE1.CrossConnectionState ==
            E1CrossConnectionState.ExtendExistingPath)
        {
            if (!incomingE1.ConnectionGroupId.HasValue)
            {
                ModelState.AddModelError(
                    nameof(CreateCrossConnectionViewModel.IncomingE1Id),
                    "The selected path endpoint does not have a connection group.");

                return;
            }

            /*
             * An endpoint must not already have a cross connection.
             */
            if (incomingE1.JoinE1Id.HasValue)
            {
                ModelState.AddModelError(
                    nameof(CreateCrossConnectionViewModel.IncomingE1Id),
                    "The selected path endpoint already has a cross connection.");
            }

            bool incomingIsJoinTarget =
                await _context.E1s.AnyAsync(e1 =>
                    e1.JoinE1Id == incomingE1.Id);

            if (incomingIsJoinTarget)
            {
                ModelState.AddModelError(
                    nameof(CreateCrossConnectionViewModel.IncomingE1Id),
                    "The selected path endpoint is already used by another cross connection.");
            }

            /*
             * The endpoint must belong to the final segment
             * of the reusable CommunicationPath.
             */
            /*
             * The E1 must be an open endpoint belonging
             * to this customer connection.
             */
            bool isReusablePathEndpoint =
                await _context.CustomerConnectionSegments
                    .AnyAsync(customerSegment =>
                        customerSegment.CustomerConnection.ConnectionGroupId ==
                            incomingE1.ConnectionGroupId.Value &&

                        (
                            customerSegment.E1Id ==
                                incomingE1.Id ||

                            customerSegment.E1.ConnectedE1Id ==
                                incomingE1.Id
                        ));

            if (!isReusablePathEndpoint)
            {
                ModelState.AddModelError(
                    nameof(CreateCrossConnectionViewModel.IncomingE1Id),
                    "The selected incoming E1 is not an open endpoint of the path.");
            }

            return;
        }

        /*
         * CrossConnected and any unexpected state cannot
         * be selected as Incoming.
         */
        ModelState.AddModelError(
            nameof(CreateCrossConnectionViewModel.IncomingE1Id),
            "The incoming E1 must be available or an endpoint of an existing path.");
    }
}