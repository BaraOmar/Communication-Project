using CommunicationProject.Data;
using CommunicationProject.Models;
using CommunicationProject.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    // GET: E1s
    public async Task<IActionResult> Index(
    int page = 1)
    {
        const int pageSize = 20;

        if (page < 1)
        {
            page = 1;
        }

        int totalItems = await _context.E1s
            .AsNoTracking()
            .CountAsync();

        int totalPages = (int)Math.Ceiling(
            totalItems / (double)pageSize);

        if (totalPages > 0 &&
            page > totalPages)
        {
            page = totalPages;
        }

        /*
         * First query:
         * Select only 20 IDs from the E1 table.
         *
         * Do not load Links, Sites, STMs or ConnectedE1s yet.
         */
        var pageIds = await _context.E1s
            .AsNoTracking()
            .OrderBy(e1 => e1.StmId)
            .ThenBy(e1 => e1.E1Number)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e1 => e1.Id)
            .ToListAsync();

        /*
         * Second query:
         * Load relationships for only those 20 E1 records.
         */
        var e1Channels = await _context.E1s
            .AsNoTracking()
            .Where(e1 => pageIds.Contains(e1.Id))

            .Include(e1 => e1.Stm)
                .ThenInclude(stm => stm.Link)
                    .ThenInclude(link => link.SiteFrom)

            .Include(e1 => e1.Stm)
                .ThenInclude(stm => stm.Link)
                    .ThenInclude(link => link.SiteTo)

            .Include(e1 => e1.ConnectedE1)
                .ThenInclude(connectedE1 =>
                    connectedE1!.Stm)

            .ToListAsync();

        /*
         * SQL does not guarantee that the second query returns
         * records in the same order as pageIds.
         */
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
            .OrderBy(e1 => orderLookup[e1.Id])
            .ToList();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalItems = totalItems;

        return View(e1Channels);
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

            .Include(item => item.Stm)
                .ThenInclude(stm => stm.Link)
                    .ThenInclude(link => link.SiteFrom)

            .Include(item => item.Stm)
                .ThenInclude(stm => stm.Link)
                    .ThenInclude(link => link.SiteTo)

            .Include(item => item.ConnectedE1)
                .ThenInclude(connected => connected!.Stm)
                    .ThenInclude(stm => stm.Link)
                        .ThenInclude(link => link.SiteFrom)

            .Include(item => item.ConnectedE1)
                .ThenInclude(connected => connected!.Stm)
                    .ThenInclude(stm => stm.Link)
                        .ThenInclude(link => link.SiteTo)

            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (e1 == null)
        {
            return NotFound();
        }

        return View(e1);
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