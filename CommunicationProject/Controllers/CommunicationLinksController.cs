using CommunicationProject.Data;
using CommunicationProject.Models;
using CommunicationProject.Security;
using CommunicationProject.Strategies.Capacity;
using CommunicationProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Data.OleDb;
using CommunicationProject.Services.CommunicationLinks.Import;

namespace CommunicationProject.Controllers;

[Authorize(Roles = AppRoles.Admin)]


public class CommunicationLinksController : Controller
{
    private readonly CommunicationDbContext _context;
    private readonly ILogger<CommunicationLinksController> _logger;
    private readonly ICapacityStrategyResolver _capacityStrategyResolver;
    private readonly ICommunicationLinkImportService _communicationLinkImportService;

    public CommunicationLinksController(
        CommunicationDbContext context,
        ILogger<CommunicationLinksController> logger,
        ICapacityStrategyResolver capacityStrategyResolver,
        ICommunicationLinkImportService communicationLinkImportService)
    {
        _context = context;
        _logger = logger;
        _capacityStrategyResolver = capacityStrategyResolver;
        _communicationLinkImportService = communicationLinkImportService;
    }

    public async Task<IActionResult> Index(
        string? search,
        Guid? linkTypeId,
        string? siteId,
        string? inventoryStatus,
        int pageNumber = 1)
    {
        const int pageSize = 15;

        if (pageNumber < 1)
        {
            pageNumber = 1;
        }


        var query = _context.CommunicationLinks
            .AsNoTracking()
            .Where(link => link.IsPrimary)
            .AsQueryable();


        // Search
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();

            query = query.Where(link =>
                link.Name.Contains(search) ||

                link.LinkType.Name.Contains(search) ||

                link.SiteFromId.Contains(search) ||
                link.SiteFrom.Name.Contains(search) ||

                link.SiteToId.Contains(search) ||
                link.SiteTo.Name.Contains(search) ||

                (link.Capacity != null &&
                 link.Capacity.Contains(search)));
        }


        // Link type
        if (linkTypeId.HasValue &&
            linkTypeId.Value != Guid.Empty)
        {
            query = query.Where(link =>
                link.LinkTypeId == linkTypeId.Value);
        }


        // Site
        if (!string.IsNullOrWhiteSpace(siteId))
        {
            query = query.Where(link =>
                link.SiteFromId == siteId ||
                link.SiteToId == siteId);
        }


        // STM inventory
        if (inventoryStatus == "generated")
        {
            query = query.Where(link =>
                link.Stms.Any());
        }
        else if (inventoryStatus == "not-generated")
        {
            query = query.Where(link =>
                !link.Stms.Any());
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
         * Project directly into the list VM.
         *
         * This avoids loading complete entity graphs for
         * records that are only being displayed in a table.
         */
        var links = await query
            .OrderBy(link => link.Name)
            .ThenBy(link => link.SiteFrom.Name)
            .ThenBy(link => link.SiteTo.Name)

            .Skip(
                (pageNumber - 1) *
                pageSize)

            .Take(pageSize)

            .Select(link =>
                new CommunicationLinkListItemViewModel
                {
                    Id = link.Id,

                    Name = link.Name,

                    LinkTypeName =
                        link.LinkType.Name,

                    SiteFromId =
                        link.SiteFromId,

                    SiteFromName =
                        link.SiteFrom.Name,

                    SiteToId =
                        link.SiteToId,

                    SiteToName =
                        link.SiteTo.Name,

                    Capacity =
                        link.Capacity,

                    StmCount =
                        link.Stms.Count,

                    ReverseStmCount =
                        link.ConnectedLink != null
                            ? link.ConnectedLink.Stms.Count
                            : 0
                })

            .ToListAsync();


        var linkTypes = await _context.LinkTypes
            .AsNoTracking()
            .OrderBy(type => type.Name)
            .ToListAsync();


        var sites = await _context.Sites
            .AsNoTracking()
            .OrderBy(site => site.Name)
            .ToListAsync();


        var model =
            new CommunicationLinkIndexViewModel
            {
                Links = links,

                Search = search,

                LinkTypeId = linkTypeId,

                SiteId = siteId,

                InventoryStatus =
                    inventoryStatus,

                LinkTypes = linkTypes,

                Sites = sites,

                PageNumber = pageNumber,

                PageSize = pageSize,

                TotalPages = totalPages,

                TotalItems = totalItems
            };


        return View(model);
    }
    [HttpGet]
    public async Task<IActionResult> GetLinksWithStmCapacity()
    {

        var links = await _context.CommunicationLinks
            .AsNoTracking()
            .Where(link =>
                link.IsPrimary &&
                link.Capacity != null &&
                EF.Functions.Like(link.Capacity, "%STM%"))
            .Select(link => new
            {
                link.Id,
                link.Name,
                link.SiteFromId,
                link.SiteToId,
                link.Capacity
            })
            .OrderBy(link => link.Name)
            .ToListAsync();

        var result = links
            .Select(link => new
            {
                link.Id,
                link.Name,
                link.SiteFromId,
                link.SiteToId,
                link.Capacity,

                StmCount =
    StmCapacityParser.ExtractCount(
        link.Capacity)
            })
            .Where(link =>
                link.StmCount.HasValue)
            .ToList();

        return Json(result);
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult>
    GenerateStmsAndE1sFromCapacity(
        CancellationToken cancellationToken)
    {
        var candidates = await _context.CommunicationLinks
            .AsNoTracking()
            .Where(link =>
                link.IsPrimary &&
                link.ConnectedLinkId != null &&
                link.Capacity != null &&
                link.Capacity != string.Empty)
            .Select(link => new
            {
                PrimaryLinkId = link.Id,

                ReverseLinkId =
                    link.ConnectedLinkId!.Value,

                LinkName = link.Name,

                link.Capacity
            })
            .OrderBy(link => link.LinkName)
            .ToListAsync(cancellationToken);

        int processedLinks = 0;
        int skippedLinks = 0;
        int failedLinks = 0;
        int createdStms = 0;
        int createdE1s = 0;

        foreach (var candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var request =
                new CapacityProcessingRequest(
                    candidate.PrimaryLinkId,
                    candidate.ReverseLinkId,
                    candidate.LinkName,
                    candidate.Capacity);

            ICapacityStrategy strategy =
                _capacityStrategyResolver.Resolve(
                    candidate.Capacity);

            CapacityProcessingResult result =
                await strategy.ProcessAsync(
                    request,
                    cancellationToken);

            createdStms += result.CreatedStms;
            createdE1s += result.CreatedE1s;

            if (result.Succeeded)
            {
                processedLinks++;
            }
            else if (result.Skipped)
            {
                skippedLinks++;
            }
            else
            {
                failedLinks++;
            }
        }

        TempData["SuccessMessage"] =
            $"Capacity processing completed. " +
            $"{processedLinks} links configured, " +
            $"{createdStms} STM records created, " +
            $"{createdE1s} E1 records created, " +
            $"{skippedLinks} links skipped, and " +
            $"{failedLinks} links failed.";

        return RedirectToAction(
            nameof(Index));
    }
    // GET: CommunicationLinks/Details/{id}
    public async Task<IActionResult> Details(Guid? id)
    {
        if (!id.HasValue || id.Value == Guid.Empty)
        {
            return NotFound();
        }

        var link = await GetPrimaryLinkAsync(id.Value, tracking: false);

        if (link == null)
        {
            return NotFound();
        }

        return View(link);
    }

    // GET: CommunicationLinks/Create
    public async Task<IActionResult> Create()
    {
        var model = new CommunicationLinkCreateViewModel
        {
            StmCount = 1
        };

        await LoadCreateDropdownsAsync(model);
        return View(model);
    }

    // POST: CommunicationLinks/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CommunicationLinkCreateViewModel model)
    {
        NormalizeCreateModel(model);

        ModelState.Clear();
        TryValidateModel(model);

        if (!model.LinkTypeId.HasValue ||
            model.LinkTypeId.Value == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(model.LinkTypeId),
                "Link type is required.");
        }

        if (!string.IsNullOrWhiteSpace(model.SiteFromId) &&
            string.Equals(
                model.SiteFromId,
                model.SiteToId,
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(model.SiteToId),
                "A communication link cannot connect a site to itself.");
        }

        if (model.StmCount < 1 || model.StmCount > 100)
        {
            ModelState.AddModelError(
                nameof(model.StmCount),
                "STM count must be between 1 and 100.");
        }

        if (!ModelState.IsValid)
        {
            await LoadCreateDropdownsAsync(model);
            return View(model);
        }

        Guid linkTypeId = model.LinkTypeId!.Value;

        await ValidateCreateReferencesAsync(model, linkTypeId);
        await ValidateLogicalDuplicateAsync(
            model.Name,
            linkTypeId,
            model.SiteFromId,
            model.SiteToId);

        if (!ModelState.IsValid)
        {
            await LoadCreateDropdownsAsync(model);
            return View(model);
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var primaryLink = new CommunicationLink
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                LinkTypeId = linkTypeId,
                SiteFromId = model.SiteFromId,
                SiteToId = model.SiteToId,
                IsPrimary = true,
                ConnectedLinkId = null
            };

            var reverseLink = new CommunicationLink
            {
                Id = Guid.NewGuid(),
                Name = model.Name,
                LinkTypeId = linkTypeId,
                SiteFromId = model.SiteToId,
                SiteToId = model.SiteFromId,
                IsPrimary = false,
                ConnectedLinkId = null
            };

            // Insert both directional link records before assigning their
            // reciprocal self-referencing foreign keys.
            _context.CommunicationLinks.AddRange(
                primaryLink,
                reverseLink);

            await _context.SaveChangesAsync();

            primaryLink.ConnectedLinkId = reverseLink.Id;
            reverseLink.ConnectedLinkId = primaryLink.Id;

            var stmPairs =
                new List<(Stm Primary, Stm Reverse,
                    List<(E1 Primary, E1 Reverse)> E1Pairs)>();

            for (int stmNumber = 1;
                 stmNumber <= model.StmCount;
                 stmNumber++)
            {
                string number = stmNumber.ToString();

                var primaryStm = new Stm
                {
                    Id = Guid.NewGuid(),
                    Number = number,
                    LinkId = primaryLink.Id,
                    ConnectedStmId = null
                };

                var reverseStm = new Stm
                {
                    Id = Guid.NewGuid(),
                    Number = number,
                    LinkId = reverseLink.Id,
                    ConnectedStmId = null
                };

                var e1Pairs = new List<(E1 Primary, E1 Reverse)>(63);

                for (int firstPart = 1; firstPart <= 3; firstPart++)
                {
                    for (int secondPart = 1; secondPart <= 7; secondPart++)
                    {
                        for (int thirdPart = 1; thirdPart <= 3; thirdPart++)
                        {
                            string e1Number =
                                $"{firstPart}.{secondPart}.{thirdPart}";

                            var primaryE1 = new E1
                            {
                                Id = Guid.NewGuid(),
                                E1Number = e1Number,
                                StmId = primaryStm.Id,
                                ConnectedE1Id = null,
                                Description = null
                            };

                            var reverseE1 = new E1
                            {
                                Id = Guid.NewGuid(),
                                E1Number = e1Number,
                                StmId = reverseStm.Id,
                                ConnectedE1Id = null,
                                Description = null
                            };

                            primaryStm.E1Channels.Add(primaryE1);
                            reverseStm.E1Channels.Add(reverseE1);
                            e1Pairs.Add((primaryE1, reverseE1));
                        }
                    }
                }

                _context.Stms.AddRange(primaryStm, reverseStm);
                stmPairs.Add((primaryStm, reverseStm, e1Pairs));
            }

            await _context.SaveChangesAsync();

            foreach (var pair in stmPairs)
            {
                pair.Primary.ConnectedStmId = pair.Reverse.Id;
                pair.Reverse.ConnectedStmId = pair.Primary.Id;

                foreach (var e1Pair in pair.E1Pairs)
                {
                    e1Pair.Primary.ConnectedE1Id = e1Pair.Reverse.Id;
                    e1Pair.Reverse.ConnectedE1Id = e1Pair.Primary.Id;
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            int totalLinkRecords = 2;
            int totalStmRecords = model.StmCount * 2;
            int totalE1Records = totalStmRecords * 63;

            TempData["SuccessMessage"] =
                $"Communication link '{primaryLink.Name}' was created as " +
                $"{totalLinkRecords} reciprocal link records, " +
                $"{totalStmRecords} STM records, and " +
                $"{totalE1Records} automatically connected E1 records.";

            return RedirectToAction(
                nameof(Details),
                new { id = primaryLink.Id });
        }
        catch (DbUpdateException exception)
            when (IsDuplicateException(exception))
        {
            await transaction.RollbackAsync();

            _logger.LogWarning(
                exception,
                "Duplicate data while creating logical communication link {Name}.",
                model.Name);

            ModelState.AddModelError(
                string.Empty,
                "The link name, route, STM numbers, or E1 numbers conflict with existing data.");
        }
        catch (DbUpdateException exception)
            when (IsForeignKeyException(exception))
        {
            await transaction.RollbackAsync();

            _logger.LogWarning(
                exception,
                "Foreign-key error while creating logical communication link {Name}.",
                model.Name);

            ModelState.AddModelError(
                string.Empty,
                "The selected Link Type or Site no longer exists.");
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync();

            _logger.LogError(
                exception,
                "Database error while creating logical communication link {Name}.",
                model.Name);

            ModelState.AddModelError(
                string.Empty,
                "The communication link and its automatic STM/E1 records could not be saved.");
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();

            _logger.LogError(
                exception,
                "Unexpected error while creating logical communication link {Name}.",
                model.Name);

            ModelState.AddModelError(
                string.Empty,
                "An unexpected error occurred. No records were created.");
        }

        await LoadCreateDropdownsAsync(model);
        return View(model);
    }

    [HttpGet]
    public IActionResult ConfigureStms(Guid? id)
    {
        TempData["ErrorMessage"] =
            "STMs and E1 channels are created automatically with the communication link.";

        return id.HasValue
            ? RedirectToAction(nameof(Details), new { id })
            : RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult CreateE1Inventory(Guid? id)
    {
        TempData["ErrorMessage"] =
            "E1 inventory is generated automatically for every STM.";

        return id.HasValue
            ? RedirectToAction(nameof(Details), new { id })
            : RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult CreateE1Connection(Guid? id)
    {
        TempData["ErrorMessage"] =
            "E1 channels are connected automatically to their matching reverse channels.";

        return id.HasValue
            ? RedirectToAction(nameof(Details), new { id })
            : RedirectToAction(nameof(Index));
    }

    // GET: CommunicationLinks/Edit/{id}
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (!id.HasValue || id.Value == Guid.Empty)
        {
            return NotFound();
        }

        var link = await GetPrimaryLinkAsync(id.Value, tracking: false);

        if (link == null)
        {
            return NotFound();
        }

        await LoadDropdownsAsync(link);
        return View(link);
    }

    // POST: CommunicationLinks/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        [Bind("Id,Name,LinkTypeId,SiteFromId,SiteToId")]
        CommunicationLink submittedLink)
    {
        if (id != submittedLink.Id)
        {
            return NotFound();
        }

        NormalizeLink(submittedLink);

        ModelState.Clear();
        TryValidateModel(submittedLink);

        // Navigation properties are loaded by EF and are not posted by the form.
        ModelState.Remove(nameof(CommunicationLink.LinkType));
        ModelState.Remove(nameof(CommunicationLink.SiteFrom));
        ModelState.Remove(nameof(CommunicationLink.SiteTo));
        ModelState.Remove(nameof(CommunicationLink.ConnectedLink));
        ModelState.Remove(nameof(CommunicationLink.Stms));

        var existingRecord = await _context.CommunicationLinks
            .Include(link => link.ConnectedLink)
            .FirstOrDefaultAsync(link => link.Id == id);

        if (existingRecord == null)
        {
            return NotFound();
        }

        CommunicationLink primaryLink;
        CommunicationLink? reverseLink;

        if (existingRecord.IsPrimary)
        {
            primaryLink = existingRecord;
            reverseLink = existingRecord.ConnectedLink;
        }
        else
        {
            if (!existingRecord.ConnectedLinkId.HasValue)
            {
                return NotFound();
            }

            primaryLink = await _context.CommunicationLinks
                .Include(link => link.ConnectedLink)
                .FirstOrDefaultAsync(link =>
                    link.Id == existingRecord.ConnectedLinkId.Value)
                ?? throw new InvalidOperationException(
                    "The primary communication link record does not exist.");

            reverseLink = primaryLink.ConnectedLink;
        }

        if (reverseLink == null)
        {
            ModelState.AddModelError(
                string.Empty,
                "The reverse communication link record is missing.");
        }

        await ValidateEditReferencesAsync(submittedLink);

        if (reverseLink != null)
        {
            await ValidateLogicalDuplicateAsync(
                submittedLink.Name,
                submittedLink.LinkTypeId,
                submittedLink.SiteFromId,
                submittedLink.SiteToId,
                primaryLink.Id,
                reverseLink.Id);
        }

        if (!ModelState.IsValid)
        {
            submittedLink.Id = primaryLink.Id;
            await LoadDropdownsAsync(submittedLink);
            return View(submittedLink);
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            primaryLink.Name = submittedLink.Name;
            primaryLink.LinkTypeId = submittedLink.LinkTypeId;
            primaryLink.SiteFromId = submittedLink.SiteFromId;
            primaryLink.SiteToId = submittedLink.SiteToId;
            primaryLink.IsPrimary = true;

            reverseLink!.Name = submittedLink.Name;
            reverseLink.LinkTypeId = submittedLink.LinkTypeId;
            reverseLink.SiteFromId = submittedLink.SiteToId;
            reverseLink.SiteToId = submittedLink.SiteFromId;
            reverseLink.IsPrimary = false;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                $"Communication link '{primaryLink.Name}' was updated in both directions.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException exception)
        {
            await transaction.RollbackAsync();

            bool stillExists = await _context.CommunicationLinks
                .AsNoTracking()
                .AnyAsync(link => link.Id == primaryLink.Id);

            if (!stillExists)
            {
                return NotFound();
            }

            _logger.LogWarning(
                exception,
                "Concurrency error while updating logical communication link {Id}.",
                primaryLink.Id);

            ModelState.AddModelError(
                string.Empty,
                "This link was changed by another user. Reload the page and try again.");
        }
        catch (DbUpdateException exception)
            when (IsDuplicateException(exception))
        {
            await transaction.RollbackAsync();

            ModelState.AddModelError(
                string.Empty,
                "A communication link with the same logical name or route already exists.");
        }
        catch (DbUpdateException exception)
            when (IsForeignKeyException(exception))
        {
            await transaction.RollbackAsync();

            ModelState.AddModelError(
                string.Empty,
                "The selected Link Type or Site no longer exists.");
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync();

            _logger.LogError(
                exception,
                "Database error while updating logical communication link {Id}.",
                primaryLink.Id);

            ModelState.AddModelError(
                string.Empty,
                "The communication link could not be updated.");
        }

        submittedLink.Id = primaryLink.Id;
        await LoadDropdownsAsync(submittedLink);
        return View(submittedLink);
    }

    // GET: CommunicationLinks/Delete/{id}
    public async Task<IActionResult> Delete(Guid? id)
    {
        if (!id.HasValue || id.Value == Guid.Empty)
        {
            return NotFound();
        }

        var link = await GetPrimaryLinkAsync(id.Value, tracking: false);

        if (link == null)
        {
            return NotFound();
        }

        return View(link);
    }

    // POST: CommunicationLinks/Delete/{id}
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(Guid id)
    {
        var requestedLink = await _context.CommunicationLinks
            .FirstOrDefaultAsync(link => link.Id == id);

        if (requestedLink == null)
        {
            return NotFound();
        }

        Guid primaryId;
        Guid reverseId;

        if (requestedLink.IsPrimary)
        {
            if (!requestedLink.ConnectedLinkId.HasValue)
            {
                return NotFound();
            }

            primaryId = requestedLink.Id;
            reverseId = requestedLink.ConnectedLinkId.Value;
        }
        else
        {
            if (!requestedLink.ConnectedLinkId.HasValue)
            {
                return NotFound();
            }

            primaryId = requestedLink.ConnectedLinkId.Value;
            reverseId = requestedLink.Id;
        }

        var links = await _context.CommunicationLinks
            .Where(link =>
                link.Id == primaryId ||
                link.Id == reverseId)
            .ToListAsync();

        if (links.Count != 2)
        {
            return NotFound();
        }

        string linkName =
            links.First(link => link.Id == primaryId).Name;

        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var stms = await _context.Stms
                .Where(stm =>
                    stm.LinkId == primaryId ||
                    stm.LinkId == reverseId)
                .ToListAsync();

            var stmIds = stms
                .Select(stm => stm.Id)
                .ToList();

            var e1Channels = await _context.E1s
                .Where(e1 => stmIds.Contains(e1.StmId))
                .ToListAsync();

            // Break circular E1 relationships before deleting E1 rows.
            foreach (var e1 in e1Channels)
            {
                e1.ConnectedE1Id = null;
            }

            await _context.SaveChangesAsync();

            _context.E1s.RemoveRange(e1Channels);
            await _context.SaveChangesAsync();

            // Break circular STM relationships before deleting STM rows.
            foreach (var stm in stms)
            {
                stm.ConnectedStmId = null;
            }

            await _context.SaveChangesAsync();

            _context.Stms.RemoveRange(stms);
            await _context.SaveChangesAsync();

            // Break circular link relationships before deleting both rows.
            foreach (var link in links)
            {
                link.ConnectedLinkId = null;
            }

            await _context.SaveChangesAsync();

            _context.CommunicationLinks.RemoveRange(links);
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                $"Communication link '{linkName}' and both directional records were deleted successfully.";

            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync();

            _logger.LogError(
                exception,
                "Database error while deleting logical communication link {Id}.",
                primaryId);

            ModelState.AddModelError(
                string.Empty,
                "The communication link and its related STM/E1 records could not be deleted.");
        }

        var linkForView = await GetPrimaryLinkAsync(primaryId, tracking: false);

        if (linkForView == null)
        {
            return RedirectToAction(nameof(Index));
        }

        return View("Delete", linkForView);
    }

    private async Task<CommunicationLink?> GetPrimaryLinkAsync(
        Guid id,
        bool tracking)
    {
        IQueryable<CommunicationLink> query =
            _context.CommunicationLinks;

        if (!tracking)
        {
            query = query.AsNoTracking();
        }

        var requested = await query
            .Include(link => link.LinkType)
            .Include(link => link.SiteFrom)
            .Include(link => link.SiteTo)
            .Include(link => link.Stms)
            .Include(link => link.ConnectedLink)
                .ThenInclude(reverse => reverse!.Stms)
            .FirstOrDefaultAsync(link => link.Id == id);

        if (requested == null)
        {
            return null;
        }

        if (requested.IsPrimary)
        {
            return requested;
        }

        if (!requested.ConnectedLinkId.HasValue)
        {
            return null;
        }

        IQueryable<CommunicationLink> primaryQuery =
            _context.CommunicationLinks;

        if (!tracking)
        {
            primaryQuery = primaryQuery.AsNoTracking();
        }

        return await primaryQuery
            .Include(link => link.LinkType)
            .Include(link => link.SiteFrom)
            .Include(link => link.SiteTo)
            .Include(link => link.Stms)
            .Include(link => link.ConnectedLink)
                .ThenInclude(reverse => reverse!.Stms)
            .FirstOrDefaultAsync(link =>
                link.Id == requested.ConnectedLinkId.Value &&
                link.IsPrimary);
    }

    private static void NormalizeCreateModel(
        CommunicationLinkCreateViewModel model)
    {
        model.Name = model.Name?.Trim() ?? string.Empty;
        model.SiteFromId = model.SiteFromId?.Trim() ?? string.Empty;
        model.SiteToId = model.SiteToId?.Trim() ?? string.Empty;
    }

    private static void NormalizeLink(CommunicationLink link)
    {
        link.Name = link.Name?.Trim() ?? string.Empty;
        link.SiteFromId = link.SiteFromId?.Trim() ?? string.Empty;
        link.SiteToId = link.SiteToId?.Trim() ?? string.Empty;
    }

    private async Task ValidateCreateReferencesAsync(
        CommunicationLinkCreateViewModel model,
        Guid linkTypeId)
    {
        bool typeExists = await _context.LinkTypes
            .AsNoTracking()
            .AnyAsync(type => type.Id == linkTypeId);

        if (!typeExists)
        {
            ModelState.AddModelError(
                nameof(model.LinkTypeId),
                "The selected link type does not exist.");
        }

        bool sourceSiteExists = await _context.Sites
            .AsNoTracking()
            .AnyAsync(site => site.Id == model.SiteFromId);

        if (!sourceSiteExists)
        {
            ModelState.AddModelError(
                nameof(model.SiteFromId),
                "The selected source site does not exist.");
        }

        bool destinationSiteExists = await _context.Sites
            .AsNoTracking()
            .AnyAsync(site => site.Id == model.SiteToId);

        if (!destinationSiteExists)
        {
            ModelState.AddModelError(
                nameof(model.SiteToId),
                "The selected destination site does not exist.");
        }
    }

    private async Task ValidateEditReferencesAsync(
        CommunicationLink link)
    {
        if (link.LinkTypeId == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(CommunicationLink.LinkTypeId),
                "Link type is required.");
        }
        else
        {
            bool typeExists = await _context.LinkTypes
                .AsNoTracking()
                .AnyAsync(type => type.Id == link.LinkTypeId);

            if (!typeExists)
            {
                ModelState.AddModelError(
                    nameof(CommunicationLink.LinkTypeId),
                    "The selected Link Type does not exist.");
            }
        }

        if (string.IsNullOrWhiteSpace(link.SiteFromId))
        {
            ModelState.AddModelError(
                nameof(CommunicationLink.SiteFromId),
                "Source site is required.");
        }
        else
        {
            bool sourceExists = await _context.Sites
                .AsNoTracking()
                .AnyAsync(site => site.Id == link.SiteFromId);

            if (!sourceExists)
            {
                ModelState.AddModelError(
                    nameof(CommunicationLink.SiteFromId),
                    "The selected source site does not exist.");
            }
        }

        if (string.IsNullOrWhiteSpace(link.SiteToId))
        {
            ModelState.AddModelError(
                nameof(CommunicationLink.SiteToId),
                "Destination site is required.");
        }
        else
        {
            bool destinationExists = await _context.Sites
                .AsNoTracking()
                .AnyAsync(site => site.Id == link.SiteToId);

            if (!destinationExists)
            {
                ModelState.AddModelError(
                    nameof(CommunicationLink.SiteToId),
                    "The selected destination site does not exist.");
            }
        }

        if (!string.IsNullOrWhiteSpace(link.SiteFromId) &&
            string.Equals(
                link.SiteFromId,
                link.SiteToId,
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(CommunicationLink.SiteToId),
                "Source site and destination site cannot be the same.");
        }
    }

    private async Task ValidateLogicalDuplicateAsync(
        string name,
        Guid linkTypeId,
        string siteFromId,
        string siteToId,
        Guid? excludedPrimaryId = null,
        Guid? excludedReverseId = null)
    {
        bool duplicateName = await _context.CommunicationLinks
            .AsNoTracking()
            .AnyAsync(existing =>
                existing.IsPrimary &&
                existing.Name == name &&
                (!excludedPrimaryId.HasValue ||
                 existing.Id != excludedPrimaryId.Value));

        if (duplicateName)
        {
            ModelState.AddModelError(
                nameof(CommunicationLink.Name),
                $"Communication link '{name}' already exists.");
        }

        bool duplicateRoute = await _context.CommunicationLinks
            .AsNoTracking()
            .AnyAsync(existing =>
                existing.LinkTypeId == linkTypeId &&
                (!excludedPrimaryId.HasValue ||
                 existing.Id != excludedPrimaryId.Value) &&
                (!excludedReverseId.HasValue ||
                 existing.Id != excludedReverseId.Value) &&
                (
                    (
                        existing.SiteFromId == siteFromId &&
                        existing.SiteToId == siteToId
                    )
                    ||
                    (
                        existing.SiteFromId == siteToId &&
                        existing.SiteToId == siteFromId
                    )
                ));

        if (duplicateRoute)
        {
            ModelState.AddModelError(
                string.Empty,
                "A communication link with this type and these two sites already exists.");
        }
    }

    private async Task LoadCreateDropdownsAsync(
        CommunicationLinkCreateViewModel model)
    {
        model.LinkTypeOptions = await _context.LinkTypes
            .AsNoTracking()
            .OrderBy(type => type.Name)
            .Select(type => new SelectListItem
            {
                Value = type.Id.ToString(),
                Text = type.Name
            })
            .ToListAsync();

        model.SiteOptions = await _context.Sites
            .AsNoTracking()
            .OrderBy(site => site.Id)
            .Select(site => new SelectListItem
            {
                Value = site.Id,
                Text = site.Id + " - " + site.Name
            })
            .ToListAsync();
    }

    private async Task LoadDropdownsAsync(
        CommunicationLink? selectedLink = null)
    {
        var linkTypes = await _context.LinkTypes
            .AsNoTracking()
            .OrderBy(type => type.Name)
            .ToListAsync();

        ViewData["LinkTypeId"] = new SelectList(
            linkTypes,
            nameof(LinkType.Id),
            nameof(LinkType.Name),
            selectedLink?.LinkTypeId);

        var sites = await _context.Sites
            .AsNoTracking()
            .OrderBy(site => site.Id)
            .Select(site => new SelectListItem
            {
                Value = site.Id,
                Text = $"{site.Id} - {site.Name}"
            })
            .ToListAsync();

        ViewData["SiteFromId"] = new SelectList(
            sites,
            nameof(SelectListItem.Value),
            nameof(SelectListItem.Text),
            selectedLink?.SiteFromId);

        ViewData["SiteToId"] = new SelectList(
            sites,
            nameof(SelectListItem.Value),
            nameof(SelectListItem.Text),
            selectedLink?.SiteToId);
    }

    private static bool IsDuplicateException(
        DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException &&
               (sqlException.Number == 2601 ||
                sqlException.Number == 2627);
    }

    private static bool IsForeignKeyException(
        DbUpdateException exception)
    {
        return exception.InnerException is SqlException sqlException &&
               sqlException.Number == 547;
    }

    // GET: CommunicationLinks/Import
    [HttpGet]
    public async Task<IActionResult> Import()
    {
        var model = new ImportCommunicationLinksViewModel();

        await LoadImportLinkTypesAsync(model);

        return View(model);
    }

    // POST: CommunicationLinks/Import
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestFormLimits(
        MultipartBodyLengthLimit = 20 * 1024 * 1024)]
    public async Task<IActionResult> Import(
        ImportCommunicationLinksViewModel model,
        CancellationToken cancellationToken)
    {
        await LoadImportLinkTypesAsync(model);

        ValidateImportRequest(model);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        string temporaryFilePath =
            Path.Combine(
                Path.GetTempPath(),
                $"communication-links-{Guid.NewGuid():N}.accdb");

        try
        {
            await SaveUploadedFileAsync(
                model.AccessFile!,
                temporaryFilePath,
                cancellationToken);

            CommunicationLinkImportResult result =
                await _communicationLinkImportService.ImportAsync(
                    temporaryFilePath,
                    model.LinkTypeId!.Value,
                    cancellationToken);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(
                    string.Empty,
                    result.Message);

                return View(model);
            }

            TempData["ImportMessage"] =
                result.Message;

            return RedirectToAction(
                nameof(Import));
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OleDbException exception)
        {
            _logger.LogError(
                exception,
                "Failed to read the uploaded Access database.");

            ModelState.AddModelError(
                nameof(model.AccessFile),
                "The Access file could not be read. " +
                "Confirm that it contains a table named LINKS " +
                "and that the ACE OLE DB provider is installed.");
        }
        catch (InvalidDataException exception)
        {
            ModelState.AddModelError(
                nameof(model.AccessFile),
                exception.Message);
        }
        catch (DbUpdateException exception)
        {
            _logger.LogError(
                exception,
                "Database error while importing communication links.");

            ModelState.AddModelError(
                string.Empty,
                exception.InnerException?.Message ??
                "The links could not be inserted into the database.");
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "An unexpected error occurred while importing " +
                "communication links.");

            ModelState.AddModelError(
                string.Empty,
                $"Import failed: " +
                $"{exception.GetBaseException().Message}");
        }
        finally
        {
            DeleteTemporaryFile(
                temporaryFilePath);
        }

        return View(model);
    }
    private void ValidateImportRequest(
    ImportCommunicationLinksViewModel model)
    {
        if (model.AccessFile == null ||
            model.AccessFile.Length == 0)
        {
            ModelState.AddModelError(
                nameof(model.AccessFile),
                "Select a non-empty Access database file.");
        }
        else
        {
            ValidateAccessFile(model);
        }

        if (!model.LinkTypeId.HasValue ||
            model.LinkTypeId.Value == Guid.Empty)
        {
            ModelState.AddModelError(
                nameof(model.LinkTypeId),
                "Select a link type.");
        }
    }
    private void ValidateAccessFile(
    ImportCommunicationLinksViewModel model)
    {
        const long maximumFileSize =
            20L * 1024L * 1024L;

        string extension =
            Path.GetExtension(
                model.AccessFile!.FileName);

        if (!string.Equals(
                extension,
                ".accdb",
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(model.AccessFile),
                "Only .accdb files are allowed.");
        }

        if (model.AccessFile.Length >
            maximumFileSize)
        {
            ModelState.AddModelError(
                nameof(model.AccessFile),
                "The file must not exceed 20 MB.");
        }
    }
    private static async Task SaveUploadedFileAsync(
    IFormFile accessFile,
    string temporaryFilePath,
    CancellationToken cancellationToken)
    {
        await using var fileStream =
            new FileStream(
                temporaryFilePath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 81920,
                useAsync: true);

        await accessFile.CopyToAsync(
            fileStream,
            cancellationToken);
    }

    private static void DeleteTemporaryFile(
    string temporaryFilePath)
    {
        try
        {
            if (System.IO.File.Exists(
                    temporaryFilePath))
            {
                System.IO.File.Delete(
                    temporaryFilePath);
            }
        }
        catch (IOException)
        {
            /*
             * Import has already completed or failed.
             * A temporary-file cleanup problem should not
             * replace the original result shown to the user.
             */
        }
        catch (UnauthorizedAccessException)
        {
            /*
             * The operating system may still hold the file.
             * Do not hide the original import result.
             */
        }
    }
    private async Task LoadImportLinkTypesAsync(
    ImportCommunicationLinksViewModel model)
    {
        model.LinkTypeOptions =
            await _context.LinkTypes
                .AsNoTracking()
                .OrderBy(linkType => linkType.Name)
                .Select(linkType =>
                    new SelectListItem
                    {
                        Value = linkType.Id.ToString(),
                        Text = linkType.Name
                    })
                .ToListAsync();
    }
}