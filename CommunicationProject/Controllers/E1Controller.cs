using CommunicationProject.Data;
using CommunicationProject.Models;
using CommunicationProject.Security;
using CommunicationProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static CommunicationProject.Models.E1;

namespace CommunicationProject.Controllers;

/*
 * E1 records are generated and connected automatically when a logical
 * CommunicationLink is created. This controller is intentionally read-only.
 */

[Authorize]

public class E1sController : Controller
{
    private readonly CommunicationDbContext _context;

    public E1sController(CommunicationDbContext context)
    {
        _context = context;
    }
    [Authorize(
       Roles =
           AppRoles.Admin + "," +
           AppRoles.Operator + "," +
           AppRoles.Viewer)]
    // GET: E1s
    public async Task<IActionResult> Index(
    string? search,
    string? siteFromId,
    string? siteToId,
    string? description,
    string? connectionStatus,
    string? state,
    string? operationalStatus,
    string? connectionType,
    int pageNumber = 1)
    {

        const int pageSize = 20;

        if (pageNumber < 1)
        {
            pageNumber = 1;
        }

        var query = _context.E1s
            .AsNoTracking()
            .AsQueryable();


        // Search
        // Search
        // Search
        // Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            var normalizedSearch = search;

            // Allow searches such as "E1 1"
            if (normalizedSearch.StartsWith(
                "E1 ",
                StringComparison.OrdinalIgnoreCase))
            {
                normalizedSearch =
                    normalizedSearch[3..].Trim();

                query = query.Where(e1 =>
                    e1.E1Number.StartsWith(normalizedSearch));
            }

            // Allow searches such as "STM 1"
            else if (normalizedSearch.StartsWith(
                "STM ",
                StringComparison.OrdinalIgnoreCase))
            {
                normalizedSearch =
                    normalizedSearch[4..].Trim();

                query = query.Where(e1 =>
                    e1.Stm != null &&
                    e1.Stm.Number.StartsWith(normalizedSearch));
            }

            else
            {
                query = query.Where(e1 =>
                    e1.E1Number.StartsWith(normalizedSearch) ||
                    (
                        e1.Stm != null &&
                        e1.Stm.Number.StartsWith(normalizedSearch)
                    ));
            }
        }


        // Site filter
        // Site From
        if (!string.IsNullOrWhiteSpace(siteFromId))
        {
            query = query.Where(e1 =>
                e1.Link.SiteFromId == siteFromId);
        }

        if (!string.IsNullOrWhiteSpace(siteToId))
        {
            query = query.Where(e1 =>
                e1.Link.SiteToId == siteToId);
        }


        // Description
        if (!string.IsNullOrWhiteSpace(description))
        {
            description = description.Trim();

            query = query.Where(e1 =>
                e1.Description != null &&
                e1.Description.Contains(description));
        }


        // Connection filter
        if (connectionStatus == "connected")
        {
            query = query.Where(e1 =>
                e1.ConnectedE1Id != null);
        }
        else if (connectionStatus == "not-connected")
        {
            query = query.Where(e1 =>
                e1.ConnectedE1Id == null);
        }


        // Cross-connection state
        if (!string.IsNullOrWhiteSpace(state) &&
            Enum.TryParse<E1CrossConnectionState>(
                state,
                true,
                out var parsedState))
        {
            query = query.Where(e1 =>
                e1.CrossConnectionState == parsedState);
        }
        // Operational status
        if (!string.IsNullOrWhiteSpace(operationalStatus) &&
            Enum.TryParse<E1OperationalStatus>(
                operationalStatus,
                true,
                out var parsedOperationalStatus))
        {
            query = query.Where(e1 =>
                e1.Status == parsedOperationalStatus);
        }
        if (!string.IsNullOrWhiteSpace(connectionType) &&
    Enum.TryParse<E1ConnectionType>(
        connectionType,
        true,
        out var parsedConnectionType))
        {
            query = query.Where(e1 =>
                e1.ConnectionType == parsedConnectionType);
        }

        int totalItems =
            await query.CountAsync();

        int totalPages =
            (int)Math.Ceiling(
                totalItems / (double)pageSize);


        if (totalPages > 0 &&
            pageNumber > totalPages)
        {
            pageNumber = totalPages;
        }


        /*
         * First query:
         * Get only IDs for this page.
         */
        var pageIds = await query
            .OrderBy(e1 => e1.StmId)
            .ThenBy(e1 => e1.E1Number)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(e1 => e1.Id)
            .ToListAsync();


        /*
         * Second query:
         * Load relationships only for the
         * E1 records shown on this page.
         */
        var e1Channels = await _context.E1s
            .AsNoTracking()

            .Where(e1 =>
                pageIds.Contains(e1.Id))

            .Include(e1 => e1.Link)
                .ThenInclude(link => link.SiteFrom)

            .Include(e1 => e1.Link)
                .ThenInclude(link => link.SiteTo)

            .Include(e1 => e1.Stm)

            .Include(e1 => e1.ConnectedE1)
                .ThenInclude(connected =>
                    connected!.Link)
                        .ThenInclude(link =>
                            link.SiteFrom)

            .Include(e1 => e1.ConnectedE1)
                .ThenInclude(connected =>
                    connected!.Link)
                        .ThenInclude(link =>
                            link.SiteTo)

            .Include(e1 => e1.ConnectedE1)
                .ThenInclude(connected =>
                    connected!.Stm)

            .ToListAsync();


        // Restore page ordering
        var orderLookup = pageIds
            .Select((id, index) => new
            {
                id,
                index
            })
            .ToDictionary(
                item => item.id,
                item => item.index);


        e1Channels = e1Channels
            .OrderBy(e1 =>
                orderLookup[e1.Id])
            .ToList();


        var sites = await _context.Sites
            .AsNoTracking()
            .OrderBy(site => site.Name)
            .ToListAsync();


        var model = new E1IndexViewModel
        {
            E1Channels = e1Channels,

            Search = search,
            SiteFromId = siteFromId,
            SiteToId = siteToId,
            Description = description,
            ConnectionStatus = connectionStatus,
            State = state,
            OperationalStatus = operationalStatus,
            ConnectionType = connectionType,

            Sites = sites,

            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            TotalItems = totalItems
        };


        return View(model);
    }
    // GET: E1s/Details/{id}
    public async Task<IActionResult> Details(Guid? id)
    {
        if (!id.HasValue || id.Value == Guid.Empty)
        {
            return NotFound();
        }

        var e1 = await _context.E1s
            .AsNoTracking()

            .Include(item => item.Link)
                .ThenInclude(link => link.SiteFrom)

            .Include(item => item.Link)
                .ThenInclude(link => link.SiteTo)

            .Include(item => item.Stm)

            .Include(item => item.ConnectedE1)
                .ThenInclude(connected =>
                    connected!.Link)
                        .ThenInclude(link =>
                            link.SiteFrom)

            .Include(item => item.ConnectedE1)
                .ThenInclude(connected =>
                    connected!.Link)
                        .ThenInclude(link =>
                            link.SiteTo)

            .Include(item => item.ConnectedE1)
                .ThenInclude(connected =>
                    connected!.Stm)

            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (e1 == null)
        {
            return NotFound();
        }

        var muxPort = await _context.MuxPorts
            .AsNoTracking()
            .Include(port => port.MuxCard)
                .ThenInclude(card => card.Mux)
                    .ThenInclude(mux => mux.MuxType)
            .FirstOrDefaultAsync(port =>
                port.E1Id == e1.Id);

        ViewBag.MuxPort = muxPort;

        return View(e1);
    }
    [HttpGet]
    [Authorize(
    Roles =
        AppRoles.Admin + "," +
        AppRoles.Operator)]
    public async Task<IActionResult> ChangeStatus(Guid? id)
    {
        if (!id.HasValue ||
            id.Value == Guid.Empty)
        {
            return NotFound();
        }

        var e1 = await _context.E1s
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (e1 == null)
        {
            return NotFound();
        }

        /*
         * Status changes apply to an active
         * end-to-end connection.
         */
        if (!e1.ConnectionGroupId.HasValue)
        {
            TempData["ErrorMessage"] =
                "This E1 is not part of an active connection.";

            return RedirectToAction(
                nameof(Details),
                new { id = e1.Id });
        }
        var customerName =
    await _context.CustomerConnections
        .AsNoTracking()
        .Where(connection =>
            connection.ConnectionGroupId ==
            e1.ConnectionGroupId)
        .Select(connection =>
            connection.Customer.Name)
        .FirstOrDefaultAsync()
    ?? string.Empty;
        var model =
            new ChangeE1StatusViewModel
            {
                E1Id = e1.Id,
                E1Number = e1.E1Number,

                ConnectionGroupId =
                    e1.ConnectionGroupId,
                CustomerName = customerName,
                CurrentStatus =
                    e1.Status,

                Status =
                    e1.Status,

                VisitDate =
                    DateTime.Today,

                Description =
                    e1.Description
            };

        return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(
    Roles =
        AppRoles.Admin + "," +
        AppRoles.Operator)]
    public async Task<IActionResult> ChangeStatus(
    ChangeE1StatusViewModel model)
    {
        model.VisitorName =
            model.VisitorName?.Trim()
            ?? string.Empty;

        model.Description =
            model.Description?.Trim();


        /*
         * Never trust ConnectionGroupId,
         * E1Number or CurrentStatus from
         * the submitted form.
         */
        var selectedE1 =
            await _context.E1s
                .AsNoTracking()
                .FirstOrDefaultAsync(e1 =>
                    e1.Id == model.E1Id);

        if (selectedE1 == null)
        {
            return NotFound();
        }

        model.E1Number =
            selectedE1.E1Number;

        model.ConnectionGroupId =
            selectedE1.ConnectionGroupId;

        model.CurrentStatus =
            selectedE1.Status;

        if (selectedE1.ConnectionGroupId.HasValue)
        {
            model.CustomerName =
                await _context.CustomerConnections
                    .AsNoTracking()
                    .Where(connection =>
                        connection.ConnectionGroupId ==
                        selectedE1.ConnectionGroupId.Value)
                    .Select(connection =>
                        connection.Customer.Name)
                    .FirstOrDefaultAsync()
                ?? string.Empty;
        }


        if (model.Status ==
    selectedE1.Status)
        {
            ModelState.AddModelError(
                nameof(model.Status),
                $"The connection is already {selectedE1.Status}.");
        }

        if (!selectedE1.ConnectionGroupId.HasValue)
        {
            ModelState.AddModelError(
                string.Empty,
                "This E1 is not part of an active connection.");
        }


        /*
         * Connected requires a description.
         */
        if (model.Status ==
                E1OperationalStatus.Connected &&
            string.IsNullOrWhiteSpace(
                model.Description))
        {
            ModelState.AddModelError(
                nameof(model.Description),
                "A description is required when " +
                "the connection status is Connected.");
        }


        if (!ModelState.IsValid)
        {
            return View(model);
        }


        await using var transaction =
            await _context.Database
                .BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

        try
        {
            /*
             * Reload inside the transaction.
             */
            var currentE1 =
                await _context.E1s
                    .FirstOrDefaultAsync(e1 =>
                        e1.Id == model.E1Id);

            if (currentE1 == null)
            {
                await transaction.RollbackAsync();
                return NotFound();
            }

            if (!currentE1.ConnectionGroupId.HasValue)
            {
                await transaction.RollbackAsync();

                TempData["ErrorMessage"] =
                    "This connection has already been released.";

                return RedirectToAction(
                    nameof(Details),
                    new { id = currentE1.Id });
            }


            Guid connectionGroupId =
                currentE1.ConnectionGroupId.Value;


            /*
             * IMPORTANT:
             *
             * Load only E1s belonging to this exact
             * end-to-end connection.
             *
             * Other E1s on the same STM or Link
             * are completely unaffected.
             */
            var connectionE1s =
                await _context.E1s
                    .Where(e1 =>
                        e1.ConnectionGroupId ==
                        connectionGroupId)
                    .ToListAsync();


            var physicalConnectionE1Ids =
    connectionE1s
        .Where(e1 =>
            e1.ConnectionType ==
            E1ConnectionType.Physical)
        .Select(e1 => e1.Id)
        .ToList();

            var muxPorts =
                await _context.MuxPorts
                    .Where(port =>
                        port.E1Id.HasValue &&
                        physicalConnectionE1Ids.Contains(
                            port.E1Id.Value))
                    .ToListAsync();

            if (connectionE1s.Count == 0)
            {
                await transaction.RollbackAsync();

                ModelState.AddModelError(
                    string.Empty,
                    "No E1 channels were found for this connection.");

                return View(model);
            }


            if (model.Status ==
                E1OperationalStatus.Available)
            {

                var customerConnection =
    await _context.CustomerConnections
        .Include(connection =>
            connection.Segments)
        .FirstOrDefaultAsync(connection =>
            connection.ConnectionGroupId ==
            connectionGroupId);

                if (customerConnection != null)
                {
                    _context.CustomerConnectionSegments.RemoveRange(
                        customerConnection.Segments);

                    _context.CustomerConnections.Remove(
                        customerConnection);
                }
                /*
                 * AVAILABLE = RELEASE CONNECTION
                 *
                 * Release the WHOLE specific E1 circuit.
                 *
                 * ConnectedE1Id is intentionally kept
                 * because it represents the physical E1
                 * pairing across the CommunicationLink.
                 */
                foreach (var e1 in connectionE1s)
                {
                    e1.Status =
                        E1OperationalStatus.Available;

                    e1.VisitorName =
                        model.VisitorName;

                    e1.VisitDate =
                        model.VisitDate;

                    e1.JoinE1Id =
                        null;


                    e1.ConnectionGroupId =
                        null;

                    e1.CrossConnectionState =
                        E1CrossConnectionState.Available;
                }
                /*
 * Release the physical MUX ports used by
 * this exact connection group.
 */
                foreach (var port in muxPorts)
                {
                    port.E1Id = null;

                    port.Status =
                        MuxPortStatus.Available;
                }
            }
            else
            {
                /*
                 * Connected / Wrong / Damaged
                 *
                 * Keep the complete topology.
                 * Only change operational state.
                 */
                foreach (var e1 in connectionE1s)
                {
                    e1.Status =
                        model.Status;

                    e1.VisitorName =
                        model.VisitorName;

                    e1.VisitDate =
                        model.VisitDate;


                    /*
                     * Description is updated when the
                     * connection is marked Connected.
                     */
                    if (model.Status ==
                        E1OperationalStatus.Connected)
                    {
                        e1.Description =
                            model.Description;
                    }
                }
                foreach (var port in muxPorts)
                {
                    port.Status =
                        model.Status switch
                        {
                            E1OperationalStatus.Connected =>
                                MuxPortStatus.Connected,

                            E1OperationalStatus.Wrong =>
                                MuxPortStatus.Wrong,

                            E1OperationalStatus.Damaged =>
                                MuxPortStatus.Damaged,

                            _ =>
                                port.Status
                        };
                }
            }


            await _context.SaveChangesAsync();

            await transaction.CommitAsync();


            if (model.Status ==
                E1OperationalStatus.Available)
            {
                TempData["SuccessMessage"] =
                    "The complete E1 connection was released successfully.";
            }
            else
            {
                TempData["SuccessMessage"] =
                    $"The complete E1 connection status was changed to " +
                    $"{model.Status}.";
            }


            return RedirectToAction(
                nameof(Details),
                new { id = model.E1Id });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    [HttpGet]
    [Authorize(
    Roles =
        AppRoles.Admin + "," +
        AppRoles.Operator)]
    public async Task<IActionResult> ChangeConnectionType(Guid? id)
    {
        if (!id.HasValue ||
            id.Value == Guid.Empty)
        {
            return NotFound();
        }

        var e1 = await _context.E1s
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (e1 == null)
        {
            return NotFound();
        }

        var model =
            new ChangeE1ConnectionTypeViewModel
            {
                E1Id = e1.Id,
                E1Number = e1.E1Number,
                CurrentType = e1.ConnectionType,
                ConnectionType = e1.ConnectionType
            };

        return View(model);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(
    Roles =
        AppRoles.Admin + "," +
        AppRoles.Operator)]
    public async Task<IActionResult> ChangeConnectionType(
    ChangeE1ConnectionTypeViewModel model)
    {
        var e1 = await _context.E1s
            .FirstOrDefaultAsync(item =>
                item.Id == model.E1Id);

        if (e1 == null)
        {
            return NotFound();
        }

        if (!Enum.IsDefined(model.ConnectionType))
        {
            ModelState.AddModelError(
                nameof(model.ConnectionType),
                "The selected connection type is invalid.");
        }

        if (!ModelState.IsValid)
        {
            model.E1Number = e1.E1Number;
            model.CurrentType = e1.ConnectionType;

            return View(model);
        }
        if (model.ConnectionType ==
        E1ConnectionType.Logical)
        {
            bool assignedToMuxPort =
                await _context.MuxPorts
                    .AnyAsync(port =>
                        port.E1Id == e1.Id);

            if (assignedToMuxPort)
            {
                ModelState.AddModelError(
                    nameof(model.ConnectionType),
                    "This E1 is assigned to a MUX port. " +
                    "Release it from the MUX port before changing it to Logical.");

                model.E1Number =
                    e1.E1Number;

                model.CurrentType =
                    e1.ConnectionType;

                return View(model);
            }
        }

        e1.ConnectionType =
            model.ConnectionType;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"E1 {e1.E1Number} connection type changed to " +
            $"{e1.ConnectionType}.";

        return RedirectToAction(
            nameof(Details),
            new { id = e1.Id });
    }
    // Compatibility routes for old links. Manual E1 operations are disabled.
    [HttpGet]
    public IActionResult Create()
    {
        TempData["ErrorMessage"] =
            "E1 channels are created automatically with their STM records.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]

    public IActionResult Edit(Guid? id)
    {
        TempData["ErrorMessage"] =
            "Automatically generated E1 channels cannot be edited individually.";

        return id.HasValue
            ? RedirectToAction(nameof(Details), new { id })
            : RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin)]

    public IActionResult Delete(Guid? id)
    {
        TempData["ErrorMessage"] =
            "Automatically generated E1 channels cannot be deleted individually.";

        return id.HasValue
            ? RedirectToAction(nameof(Details), new { id })
            : RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult ManualConnect()
    {
        TempData["ErrorMessage"] =
            "E1 channels are connected automatically to their matching reverse channels.";

        return RedirectToAction(nameof(Index));
    }
}