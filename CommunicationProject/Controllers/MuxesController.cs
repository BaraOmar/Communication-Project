using CommunicationProject.Data;
using CommunicationProject.Models;
using CommunicationProject.Security;
using CommunicationProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace CommunicationProject.Controllers;

[Authorize(
    Roles =
        AppRoles.Admin + "," +
        AppRoles.Operator + "," +
        AppRoles.Viewer)]
public class MuxesController : Controller
{
    private readonly CommunicationDbContext _context;

    public MuxesController(
        CommunicationDbContext context)
    {
        _context = context;
    }

    // GET: Muxes
    // GET: Muxes
    public async Task<IActionResult> Index()
    {
        var muxes = await _context.Muxes
            .AsNoTracking()

            .Include(mux => mux.Site)
            .Include(mux => mux.MuxType)

            .OrderBy(mux => mux.Site.Name)
            .ThenBy(mux => mux.Name)

            .ToListAsync();

        return View(muxes);
    }
    // GET: Muxes/Create
    // GET: Muxes/Create
    // GET: Muxes/Create
    [HttpGet]
    [Authorize(
        Roles =
            AppRoles.Admin + "," +
            AppRoles.Operator)]
    public async Task<IActionResult> Create()
    {
        var muxTypes = await _context.MuxTypes
            .AsNoTracking()
            .OrderBy(type => type.Name)
            .ToListAsync();

        var model = new CreateMuxViewModel
        {
            SiteOptions = await _context.Sites
                .AsNoTracking()
                .OrderBy(site => site.Name)
                .Select(site => new SelectListItem
                {
                    Value = site.Id,
                    Text = site.Id + " - " + site.Name
                })
                .ToListAsync(),

            MuxTypeOptions = muxTypes
                .Select(type => new SelectListItem
                {
                    Value = type.Id.ToString(),
                    Text = type.Name
                })
                .ToList(),

            MuxTypeShelfSupport = muxTypes
                .ToDictionary(
                    type => type.Id,
                    type => type.HasShelves)
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(
    Roles =
        AppRoles.Admin + "," +
        AppRoles.Operator)]
    public async Task<IActionResult> Create(
    CreateMuxViewModel model)
    {
        model.Name =
            model.Name?.Trim() ?? string.Empty;

        model.SiteId =
            model.SiteId?.Trim() ?? string.Empty;

        bool siteExists =
            await _context.Sites
                .AsNoTracking()
                .AnyAsync(site =>
                    site.Id == model.SiteId);

        if (!siteExists)
        {
            ModelState.AddModelError(
                nameof(model.SiteId),
                "The selected site is invalid.");
        }

        MuxType? muxType = null;

        if (model.MuxTypeId.HasValue)
        {
            muxType = await _context.MuxTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(type =>
                    type.Id == model.MuxTypeId.Value);
        }

        if (muxType == null)
        {
            ModelState.AddModelError(
                nameof(model.MuxTypeId),
                "The selected MUX type is invalid.");
        }
        else
        {
            if (muxType.HasShelves)
            {
                if (!model.ShelfCount.HasValue)
                {
                    ModelState.AddModelError(
                        nameof(model.ShelfCount),
                        "Number of shelves is required for this MUX type.");
                }
            }
            else
            {
                // MUX types without shelves must not store a shelf count.
                model.ShelfCount = null;
            }
        }

        if (!model.CardSlotCount.HasValue)
        {
            ModelState.AddModelError(
                nameof(model.CardSlotCount),
                "Card slot count is required.");
        }

        bool duplicateExists =
            await _context.Muxes
                .AnyAsync(mux =>
                    mux.SiteId == model.SiteId &&
                    mux.Name == model.Name);

        if (duplicateExists)
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "A MUX with this name already exists at this site.");
        }

        if (!ModelState.IsValid)
        {
            var muxTypes = await _context.MuxTypes
                .AsNoTracking()
                .OrderBy(type => type.Name)
                .ToListAsync();

            model.SiteOptions = await _context.Sites
                .AsNoTracking()
                .OrderBy(site => site.Name)
                .Select(site => new SelectListItem
                {
                    Value = site.Id,
                    Text = site.Id + " - " + site.Name
                })
                .ToListAsync();

            model.MuxTypeOptions = muxTypes
                .Select(type => new SelectListItem
                {
                    Value = type.Id.ToString(),
                    Text = type.Name
                })
                .ToList();

            model.MuxTypeShelfSupport = muxTypes
                .ToDictionary(
                    type => type.Id,
                    type => type.HasShelves);

            return View(model);
        }

        var mux = new Mux
        {
            Id = Guid.NewGuid(),
            Name = model.Name,
            SiteId = model.SiteId,
            MuxTypeId = model.MuxTypeId,
            ShelfCount = model.ShelfCount,
            CardSlotCount = model.CardSlotCount!.Value
        };

        _context.Muxes.Add(mux);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"MUX '{mux.Name}' was created successfully.";

        return RedirectToAction(
            nameof(Details),
            new { id = mux.Id });
    }
    // GET: Muxes/Details/{id}
    // GET: Muxes/Details/{id}
    public async Task<IActionResult> Details(Guid? id)
    {
        if (!id.HasValue ||
            id.Value == Guid.Empty)
        {
            return NotFound();
        }

        var mux = await _context.Muxes
            .AsNoTracking()

            .Include(item => item.Site)

            .Include(item => item.MuxType)

            .Include(item => item.Cards)
                .ThenInclude(card => card.CardType)

            .Include(item => item.Cards)
                .ThenInclude(card => card.Ports)

            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (mux == null)
        {
            return NotFound();
        }

        return View(mux);
    }




}