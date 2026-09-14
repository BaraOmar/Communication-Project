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
    public async Task<IActionResult> Index()
    {
        var muxes = await _context.Muxes
            .AsNoTracking()

            .Include(mux => mux.Site)

            .Include(mux => mux.CommunicationLink)
                .ThenInclude(link => link.SiteFrom)

            .Include(mux => mux.CommunicationLink)
                .ThenInclude(link => link.SiteTo)

            .OrderBy(mux => mux.Site.Name)
            .ThenBy(mux => mux.Name)

            .ToListAsync();

        return View(muxes);
    }
    // GET: Muxes/Create
    [HttpGet]
    [Authorize(
        Roles =
            AppRoles.Admin + "," +
            AppRoles.Operator)]
    public async Task<IActionResult> Create()
    {
        var model = new CreateMuxViewModel();

        model.CommunicationLinkOptions =
            await _context.CommunicationLinks
                .AsNoTracking()
                .Where(link => link.IsPrimary)
                .OrderBy(link => link.Name)
                .Select(link => new SelectListItem
                {
                    Value = link.Id.ToString(),

                    Text =
                        link.Name + " — " +
                        link.SiteFrom.Name + " ↔ " +
                        link.SiteTo.Name
                })
                .ToListAsync();

        return View(model);
    }
    [HttpGet]
    [Authorize(
    Roles =
        AppRoles.Admin + "," +
        AppRoles.Operator)]
    public async Task<IActionResult> GetLinkSites(Guid? communicationLinkId)
    {
        if (!communicationLinkId.HasValue ||
            communicationLinkId.Value == Guid.Empty)
        {
            return Json(Array.Empty<object>());
        }

        var link = await _context.CommunicationLinks
            .AsNoTracking()
            .Where(item =>
                item.Id == communicationLinkId.Value)
            .Select(item => new
            {
                siteFromId = item.SiteFromId,
                siteFromName = item.SiteFrom.Name,

                siteToId = item.SiteToId,
                siteToName = item.SiteTo.Name
            })
            .FirstOrDefaultAsync();

        if (link == null)
        {
            return Json(Array.Empty<object>());
        }

        return Json(new[]
        {
        new
        {
            value = link.siteFromId,
            text = link.siteFromId + " - " + link.siteFromName
        },

        new
        {
            value = link.siteToId,
            text = link.siteToId + " - " + link.siteToName
        }
    });
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


        CommunicationLink? link = null;

        if (model.CommunicationLinkId.HasValue)
        {
            link = await _context.CommunicationLinks
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.Id ==
                        model.CommunicationLinkId.Value &&
                    item.IsPrimary);
        }


        if (link == null)
        {
            ModelState.AddModelError(
                nameof(model.CommunicationLinkId),
                "The selected communication link is invalid.");
        }
        else
        {
            /*
             * A MUX can only belong to one of the
             * two endpoint sites of its link.
             */
            bool siteIsEndpoint =
                string.Equals(
                    model.SiteId,
                    link.SiteFromId,
                    StringComparison.OrdinalIgnoreCase)
                ||
                string.Equals(
                    model.SiteId,
                    link.SiteToId,
                    StringComparison.OrdinalIgnoreCase);

            if (!siteIsEndpoint)
            {
                ModelState.AddModelError(
                    nameof(model.SiteId),
                    "The selected site must be an endpoint " +
                    "of the communication link.");
            }
        }


        if (link != null)
        {
            bool duplicateExists =
                await _context.Muxes
                    .AnyAsync(mux =>
                        mux.CommunicationLinkId ==
                            link.Id &&
                        mux.SiteId ==
                            model.SiteId &&
                        mux.Name ==
                            model.Name);

            if (duplicateExists)
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "A MUX with this name already exists " +
                    "at this site for the selected link.");
            }
        }


        if (!ModelState.IsValid)
        {
            model.CommunicationLinkOptions =
                await _context.CommunicationLinks
                    .AsNoTracking()
                    .Where(item => item.IsPrimary)
                    .OrderBy(item => item.Name)
                    .Select(item =>
                        new SelectListItem
                        {
                            Value =
                                item.Id.ToString(),

                            Text =
                                item.Name + " — " +
                                item.SiteFrom.Name + " ↔ " +
                                item.SiteTo.Name
                        })
                    .ToListAsync();

            return View(model);
        }


        var mux = new Mux
        {
            Id = Guid.NewGuid(),

            Name = model.Name,

            SiteId = model.SiteId,

            CommunicationLinkId =
                model.CommunicationLinkId!.Value
        };


        _context.Muxes.Add(mux);

        await _context.SaveChangesAsync();


        TempData["SuccessMessage"] =
            $"MUX '{mux.Name}' was created successfully.";


        return RedirectToAction(
            nameof(Index));
    }
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

            .Include(item => item.CommunicationLink)
                .ThenInclude(link => link.SiteFrom)

            .Include(item => item.CommunicationLink)
                .ThenInclude(link => link.SiteTo)

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