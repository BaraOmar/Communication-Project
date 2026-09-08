using CommunicationProject.Data;
using CommunicationProject.Models;
using CommunicationProject.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunicationProject.Controllers;

/*
 * STM records are generated and connected automatically when a logical
 * CommunicationLink is created. This controller is intentionally read-only.
 */
public class StmsController : Controller
{
    private readonly CommunicationDbContext _context;

    public StmsController(CommunicationDbContext context)
    {
        _context = context;
    }

    // GET: Stms
    // GET: Stms
    public async Task<IActionResult> Index(
        string? search,
        string? siteId,
        string? connectionStatus,
        int pageNumber = 1)
    {
        const int pageSize = 25;

        if (pageNumber < 1)
        {
            pageNumber = 1;
        }

        var query = _context.Stms
            .AsNoTracking()
            .Include(stm => stm.Link)
                .ThenInclude(link => link.SiteFrom)
            .Include(stm => stm.Link)
                .ThenInclude(link => link.SiteTo)
            .Include(stm => stm.ConnectedStm)
                .ThenInclude(stm => stm!.Link)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(stm =>
                stm.Number.Contains(search) ||
                stm.Link.Name.Contains(search) ||
                stm.Link.SiteFromId.Contains(search) ||
                stm.Link.SiteToId.Contains(search) ||
                stm.Link.SiteFrom.Name.Contains(search) ||
                stm.Link.SiteTo.Name.Contains(search) ||
                (stm.ConnectedStm != null &&
                 stm.ConnectedStm.Number.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(siteId))
        {
            query = query.Where(stm =>
                stm.Link.SiteFromId == siteId);
        }

        if (connectionStatus == "connected")
        {
            query = query.Where(stm =>
                stm.ConnectedStmId != null);
        }
        else if (connectionStatus == "not-connected")
        {
            query = query.Where(stm =>
                stm.ConnectedStmId == null);
        }

        int totalItems = await query.CountAsync();

        int totalPages = (int)Math.Ceiling(
            totalItems / (double)pageSize);

        if (totalPages > 0 &&
            pageNumber > totalPages)
        {
            pageNumber = totalPages;
        }

        var stms = await query
            .OrderBy(stm => stm.Link.SiteFrom.Name)
            .ThenBy(stm => stm.Link.Name)
            .ThenBy(stm => stm.Number)
            .ThenBy(stm => stm.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var sites = await _context.Sites
            .AsNoTracking()
            .OrderBy(site => site.Name)
            .ToListAsync();

        var model = new StmIndexViewModel
        {
            Stms = stms,

            Search = search,
            SiteId = siteId,
            ConnectionStatus = connectionStatus,

            Sites = sites,

            PageNumber = pageNumber,
            TotalPages = totalPages,
            TotalItems = totalItems
        };

        return View(model);
    }
    // GET: Stms/Details/{id}
    public async Task<IActionResult> Details(Guid? id)
    {
        if (!id.HasValue || id.Value == Guid.Empty)
        {
            return NotFound();
        }

        var stm = await _context.Stms
            .AsNoTracking()

            .Include(item => item.Link)
                .ThenInclude(link => link.SiteFrom)

            .Include(item => item.Link)
                .ThenInclude(link => link.SiteTo)

            .Include(item => item.ConnectedStm)
                .ThenInclude(connected => connected!.Link)
                    .ThenInclude(link => link.SiteFrom)

            .Include(item => item.ConnectedStm)
                .ThenInclude(connected => connected!.Link)
                    .ThenInclude(link => link.SiteTo)

            .Include(item => item.E1Channels)

            .FirstOrDefaultAsync(item => item.Id == id.Value);

        if (stm == null)
        {
            return NotFound();
        }

        return View(stm);
    }

    // Compatibility routes for old links. Manual STM operations are disabled.
    [HttpGet]
    public IActionResult Create()
    {
        TempData["ErrorMessage"] =
            "STM records are created automatically with a communication link.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Edit(Guid? id)
    {
        TempData["ErrorMessage"] =
            "Automatically generated STM records cannot be edited individually.";

        return id.HasValue
            ? RedirectToAction(nameof(Details), new { id })
            : RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Delete(Guid? id)
    {
        TempData["ErrorMessage"] =
            "Automatically generated STM records cannot be deleted individually.";

        return id.HasValue
            ? RedirectToAction(nameof(Details), new { id })
            : RedirectToAction(nameof(Index));
    }
}