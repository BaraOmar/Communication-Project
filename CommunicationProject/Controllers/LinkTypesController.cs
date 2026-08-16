using CommunicationProject.Data;
using CommunicationProject.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace CommunicationProject.Controllers;

public class LinkTypesController : Controller
{
    private readonly CommunicationDbContext _context;
    private readonly ILogger<LinkTypesController> _logger;

    public LinkTypesController(
        CommunicationDbContext context,
        ILogger<LinkTypesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: LinkTypes
    public async Task<IActionResult> Index()
    {
        var linkTypes = await _context.LinkTypes
            .AsNoTracking()
            .OrderBy(type => type.Name)
            .ToListAsync();

        return View(linkTypes);
    }

    // GET: LinkTypes/Details/{id}
    public async Task<IActionResult> Details(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var linkType = await _context.LinkTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(type => type.Id == id);

        if (linkType == null)
        {
            return NotFound();
        }

        return View(linkType);
    }

    // GET: LinkTypes/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: LinkTypes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("Name")] LinkType linkType)
    {
        linkType.Name = linkType.Name?.Trim() ?? string.Empty;

        ModelState.Clear();

        if (!TryValidateModel(linkType))
        {
            return View(linkType);
        }

        var duplicateExists = await _context.LinkTypes
            .AsNoTracking()
            .AnyAsync(type => type.Name == linkType.Name);

        if (duplicateExists)
        {
            ModelState.AddModelError(
                nameof(LinkType.Name),
                $"Link type '{linkType.Name}' already exists.");

            return View(linkType);
        }

        try
        {
            // Leave Id empty. EF Core will generate the Guid.
            _context.LinkTypes.Add(linkType);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Link type '{linkType.Name}' was created successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException exception)
            when (IsDuplicateException(exception))
        {
            ModelState.AddModelError(
                nameof(LinkType.Name),
                $"Link type '{linkType.Name}' already exists.");
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(
                exception,
                "Database error while creating link type {Name}.",
                linkType.Name);

            ModelState.AddModelError(
                string.Empty,
                "The link type could not be saved because of a database error.");
        }

        return View(linkType);
    }

    // GET: LinkTypes/Edit/{id}
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var linkType = await _context.LinkTypes.FindAsync(id);

        if (linkType == null)
        {
            return NotFound();
        }

        return View(linkType);
    }

    // POST: LinkTypes/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        [Bind("Id,Name")] LinkType linkType)
    {
        if (id != linkType.Id)
        {
            return NotFound();
        }

        linkType.Name = linkType.Name?.Trim() ?? string.Empty;

        ModelState.Clear();

        if (!TryValidateModel(linkType))
        {
            return View(linkType);
        }

        var duplicateExists = await _context.LinkTypes
            .AsNoTracking()
            .AnyAsync(type =>
                type.Id != linkType.Id &&
                type.Name == linkType.Name);

        if (duplicateExists)
        {
            ModelState.AddModelError(
                nameof(LinkType.Name),
                $"Link type '{linkType.Name}' already exists.");

            return View(linkType);
        }

        try
        {
            _context.Update(linkType);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Link type '{linkType.Name}' was updated successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException exception)
        {
            if (!await LinkTypeExists(linkType.Id))
            {
                return NotFound();
            }

            _logger.LogWarning(
                exception,
                "Concurrency error while updating link type {Id}.",
                linkType.Id);

            ModelState.AddModelError(
                string.Empty,
                "This record was changed by another user. Reload the page and try again.");
        }
        catch (DbUpdateException exception)
            when (IsDuplicateException(exception))
        {
            ModelState.AddModelError(
                nameof(LinkType.Name),
                $"Link type '{linkType.Name}' already exists.");
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(
                exception,
                "Database error while updating link type {Id}.",
                linkType.Id);

            ModelState.AddModelError(
                string.Empty,
                "The link type could not be updated because of a database error.");
        }

        return View(linkType);
    }

    // GET: LinkTypes/Delete/{id}
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var linkType = await _context.LinkTypes
            .AsNoTracking()
            .FirstOrDefaultAsync(type => type.Id == id);

        if (linkType == null)
        {
            return NotFound();
        }

        return View(linkType);
    }

    // POST: LinkTypes/Delete/{id}
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var linkType = await _context.LinkTypes.FindAsync(id);

        if (linkType == null)
        {
            return NotFound();
        }

        var isUsed = await _context.CommunicationLinks
            .AsNoTracking()
            .AnyAsync(link => link.LinkTypeId == id);

        if (isUsed)
        {
            ModelState.AddModelError(
                string.Empty,
                $"Link type '{linkType.Name}' cannot be deleted because it is used by one or more communication links.");

            return View("Delete", linkType);
        }

        try
        {
            _context.LinkTypes.Remove(linkType);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                $"Link type '{linkType.Name}' was deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException exception)
            when (exception.InnerException is SqlException sqlException &&
                  sqlException.Number == 547)
        {
            ModelState.AddModelError(
                string.Empty,
                "This link type cannot be deleted because another record depends on it.");
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(
                exception,
                "Database error while deleting link type {Id}.",
                id);

            ModelState.AddModelError(
                string.Empty,
                "A database error occurred while deleting the link type.");
        }

        return View("Delete", linkType);
    }

    private async Task<bool> LinkTypeExists(Guid id)
    {
        return await _context.LinkTypes
            .AsNoTracking()
            .AnyAsync(type => type.Id == id);
    }

    private static bool IsDuplicateException(DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException &&
               (sqlException.Number == 2601 ||
                sqlException.Number == 2627);
    }
}