using CommunicationProject.Data;
using CommunicationProject.Models;
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
        int page = 1)
    {
        const int pageSize = 50;

        if (page < 1)
        {
            page = 1;
        }

        int totalItems = await _context.Stms
            .AsNoTracking()
            .CountAsync();

        int totalPages = (int)Math.Ceiling(
            totalItems / (double)pageSize);

        if (totalPages > 0 &&
            page > totalPages)
        {
            page = totalPages;
        }

        var stms = await _context.Stms
            .AsNoTracking()

            .Include(stm => stm.Link)
                .ThenInclude(link => link.SiteFrom)

            .Include(stm => stm.Link)
                .ThenInclude(link => link.SiteTo)

            /*
             * The Index page usually needs only the connected
             * STM record—not all of its Link/Site relationships.
             */
            .Include(stm => stm.ConnectedStm)

            /*
             * Ordering must be fully unique before Skip/Take.
             */
            .OrderBy(stm => stm.Link.Name)
            .ThenBy(stm => stm.Link.SiteFromId)
            .ThenBy(stm => stm.Number)
            .ThenBy(stm => stm.Id)

            .Skip((page - 1) * pageSize)
            .Take(pageSize)

            .ToListAsync();

        ViewBag.CurrentPage = page;
        ViewBag.TotalPages = totalPages;
        ViewBag.PageSize = pageSize;
        ViewBag.TotalItems = totalItems;

        return View(stms);
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