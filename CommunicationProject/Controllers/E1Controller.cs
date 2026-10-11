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
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Operator)]
    public IActionResult ChangeStatus(Guid? id)
    {
        if (!id.HasValue || id.Value == Guid.Empty)
        {
            return NotFound();
        }

        return RedirectToAction(
            "FocusE1Status",
            "SearchPath",
            new { id = id.Value });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Operator)]
    public IActionResult ChangeStatus(ChangeE1StatusViewModel model)
    {
        TempData["ErrorMessage"] =
            "Status changes are now made from Search Path. " +
            "Select the E1 endpoints there.";

        return RedirectToAction(
            "FocusE1Status",
            "SearchPath",
            new { id = model.E1Id });
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