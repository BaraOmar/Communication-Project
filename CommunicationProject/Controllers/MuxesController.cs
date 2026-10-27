using CommunicationProject.Data;
using CommunicationProject.Models;
using CommunicationProject.Security;
using CommunicationProject.Services.Muxes;
using CommunicationProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CommunicationProject.Controllers;

[Authorize(Roles =
    AppRoles.Admin + "," +
    AppRoles.Operator + "," +
    AppRoles.Viewer)]
public class MuxesController : Controller
{
    private readonly CommunicationDbContext _context;

    public MuxesController(CommunicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var muxes = await _context.Muxes
            .AsNoTracking()
            .Include(mux => mux.Site)
            .Include(mux => mux.MuxType)
            .Include(mux => mux.Cards)
            .OrderBy(mux => mux.Site.Name)
            .ThenBy(mux => mux.Name)
            .ToListAsync();

        return View(muxes);
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Operator)]
    public async Task<IActionResult> Create(string? siteId)
    {
        if (!string.IsNullOrWhiteSpace(siteId))
            return RedirectToAction(nameof(InstallForSite), new { siteId });

        var model = new CreateMuxViewModel
        {
            SiteOptions = await _context.Sites
                .AsNoTracking()
                .OrderBy(site => site.Id)
                .Select(site => new SelectListItem
                {
                    Value = site.Id,
                    Text = site.Id + " - " + site.Name
                })
                .ToListAsync()
        };

        return View(model);
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Operator)]
    public async Task<IActionResult> InstallForSdhLink(
        Guid linkId,
        string? siteId)
    {
        siteId = siteId?.Trim();

        var link = await _context.CommunicationLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                (item.Id == linkId || item.ConnectedLinkId == linkId) &&
                item.SiteFromId == siteId &&
                item.LinkType.Name == "SDH");

        if (link == null)
            return NotFound();

        return RedirectToAction(
            nameof(InstallForSite),
            new { siteId = link.SiteFromId });
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Operator)]
    public async Task<IActionResult> InstallForSite(string? siteId)
    {
        siteId = siteId?.Trim();

        var site = await _context.Sites
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == siteId);

        if (site == null)
            return NotFound();

        int number = await _context.Muxes
            .CountAsync(mux => mux.SiteId == site.Id) + 1;

        string name;

        do
        {
            name = $"{site.Id} MUX {number++}";
        }
        while (await _context.Muxes.AnyAsync(mux =>
            mux.SiteId == site.Id && mux.Name == name));

        var model = new InstallSiteMuxViewModel
        {
            SiteId = site.Id,
            SiteName = site.Name,
            MuxName = name
        };

        await PopulateInstallationAsync(model);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> GetInstallationTemplate(Guid muxTypeId)
    {
        var type = await _context.MuxTypes
            .AsNoTracking()
            .Include(item => item.DefaultCards)
            .FirstOrDefaultAsync(item => item.Id == muxTypeId);

        if (type == null ||
            !MuxStructureRules.IsSupported(type.Model))
            return NotFound();

        return Json(new
        {
            model = (int)type.Model,
            cards = type.DefaultCards
                .OrderBy(card => card.Section)
                .ThenBy(card => card.ShelfNumber)
                .ThenBy(card => card.SlotNumber)
                .Select(card => new
                {
                    category = (int)card.Category,
                    section = (int)card.Section,
                    shelfNumber = card.ShelfNumber,
                    slotNumber = card.SlotNumber,
                    portCount = card.PortCount,
                    isRequired = card.IsRequired
                })
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Operator)]
    public async Task<IActionResult> InstallForSite(
        InstallSiteMuxViewModel model)
    {
        model.SiteId = model.SiteId?.Trim() ?? string.Empty;
        model.MuxName = model.MuxName?.Trim() ?? string.Empty;
        model.Cards ??= [];
        model.StmAssignments ??= [];

        var site = await _context.Sites
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == model.SiteId);

        if (site == null)
            return NotFound();

        model.SiteName = site.Name;

        var type = await _context.MuxTypes
            .AsNoTracking()
            .Include(item => item.DefaultCards)
            .FirstOrDefaultAsync(item => item.Id == model.MuxTypeId);

        var definitions = model.Cards.Select(card => new MuxTypeCard
        {
            Category = card.Category,
            Section = card.Section,
            ShelfNumber = card.ShelfNumber,
            SlotNumber = card.SlotNumber,
            PortCount = card.PortCount,
            IsRequired = card.IsRequired
        }).ToList();

        if (type == null)
        {
            ModelState.AddModelError(
                nameof(model.MuxTypeId),
                "Select a valid MUX type.");
        }
        else
        {
            model.Model = type.Model;

            foreach (string error in MuxStructureRules.Validate(
                type.Model, definitions))
            {
                ModelState.AddModelError(string.Empty, error);
            }

            foreach (var required in type.DefaultCards.Where(c => c.IsRequired))
            {
                bool exists = definitions.Any(card =>
                    card.Category == required.Category &&
                    card.Section == required.Section &&
                    card.ShelfNumber == required.ShelfNumber &&
                    card.SlotNumber == required.SlotNumber &&
                    card.PortCount == required.PortCount &&
                    card.IsRequired);

                if (!exists)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        $"Required card {required.SlotNumber} " +
                        $"in {required.Section}, shelf {required.ShelfNumber}, " +
                        "must retain its default configuration.");
                }
            }

            if (type.Model == MuxModel.Tellab)
            {
                if (!model.FirstPhysicalStmNumber.HasValue ||
                    !model.SecondPhysicalStmNumber.HasValue)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Select both physical STM numbers for Tellab.");
                }
                else if (model.FirstPhysicalStmNumber ==
                         model.SecondPhysicalStmNumber)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Tellab physical STM numbers must be different.");
                }
            }
            else
            {
                model.FirstPhysicalStmNumber = null;
                model.SecondPhysicalStmNumber = null;
            }
        }

        if (await _context.Muxes.AnyAsync(mux =>
            mux.SiteId == model.SiteId && mux.Name == model.MuxName))
        {
            ModelState.AddModelError(
                nameof(model.MuxName),
                "A MUX with this name already exists at this site.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateInstallationAsync(model);
            return View(model);
        }

        var mux = new Mux
        {
            Id = Guid.NewGuid(),
            SiteId = model.SiteId,
            Name = model.MuxName,
            MuxTypeId = type!.Id,
            Model = type.Model,
            FirstPhysicalStmNumber = model.FirstPhysicalStmNumber,
            SecondPhysicalStmNumber = model.SecondPhysicalStmNumber
        };

        foreach (var definition in definitions)
        {
            var card = new MuxCard
            {
                Id = Guid.NewGuid(),
                MuxId = mux.Id,
                Category = definition.Category,
                Section = definition.Section,
                ShelfNumber = definition.ShelfNumber,
                SlotNumber = definition.SlotNumber,
                IsRequired = definition.IsRequired
            };

            for (int number = 1; number <= definition.PortCount; number++)
            {
                card.Ports.Add(new MuxPort
                {
                    Id = Guid.NewGuid(),
                    MuxCardId = card.Id,
                    PortNumber = number,
                    Status = MuxPortStatus.Available
                });
            }

            mux.Cards.Add(card);
        }

        var ports = mux.Cards
            .Where(card => card.Category == CardCategory.STM)
            .SelectMany(card => card.Ports.Select(port => new
            {
                Key = $"{(int)card.Section}:{card.ShelfNumber}:" +
                      $"{card.SlotNumber}:{port.PortNumber}",
                Port = port
            }))
            .ToDictionary(item => item.Key, item => item.Port);

        var assignments = model.StmAssignments
            .Where(item => !string.IsNullOrWhiteSpace(item.PortKey))
            .ToList();

        if (assignments.GroupBy(item => item.StmId)
            .Any(group => group.Count() > 1))
        {
            ModelState.AddModelError(
                string.Empty, "An STM cannot be assigned twice.");
        }

        if (assignments.GroupBy(item => item.PortKey!.Trim())
            .Any(group => group.Count() > 1))
        {
            ModelState.AddModelError(
                string.Empty, "A MUX port cannot be assigned twice.");
        }

        foreach (var assignment in assignments)
        {
            if (!ports.ContainsKey(assignment.PortKey!.Trim()))
            {
                ModelState.AddModelError(
                    string.Empty, "A selected STM port is invalid.");
            }
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var ids = assignments.Select(item => item.StmId).Distinct().ToList();

        int availableCount = await _context.Stms.CountAsync(stm =>
            ids.Contains(stm.Id) &&
            stm.Link.SiteFromId == model.SiteId &&
            stm.Link.LinkType.Name == "SDH" &&
            !_context.MuxPorts.Any(port => port.StmId == stm.Id));

        if (availableCount != ids.Count)
        {
            ModelState.AddModelError(
                string.Empty,
                "A selected STM is unavailable or belongs to another site.");
        }

        if (!ModelState.IsValid)
        {
            await transaction.RollbackAsync();
            await PopulateInstallationAsync(model);
            return View(model);
        }

        foreach (var assignment in assignments)
        {
            var port = ports[assignment.PortKey!.Trim()];
            port.StmId = assignment.StmId;
            port.Status = MuxPortStatus.Connected;
        }

        try
        {
            _context.Muxes.Add(mux);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            _context.ChangeTracker.Clear();

            ModelState.AddModelError(
                string.Empty,
                "Installation could not be saved. Check the MUX name " +
                "and whether another user assigned one of the selected STMs.");

            await PopulateInstallationAsync(model);
            return View(model);
        }

        TempData["SuccessMessage"] =
            $"MUX '{mux.Name}' installed with {mux.Cards.Count} cards.";

        return RedirectToAction(nameof(Details), new { id = mux.Id });
    }

    public async Task<IActionResult> Details(Guid? id)
    {
        var mux = await _context.Muxes
            .AsNoTracking()
            .Include(item => item.Site)
            .Include(item => item.MuxType)
            .Include(item => item.Cards)
                .ThenInclude(card => card.Ports)
                    .ThenInclude(port => port.Stm)
                        .ThenInclude(stm => stm!.Link)
            .Include(item => item.Cards)
                .ThenInclude(card => card.Ports)
                    .ThenInclude(port => port.E1)
            .FirstOrDefaultAsync(item => item.Id == id);

        return mux == null ? NotFound() : View(mux);
    }

    private async Task PopulateInstallationAsync(
        InstallSiteMuxViewModel model)
    {
        model.MuxTypeOptions = await _context.MuxTypes
            .AsNoTracking()
            .Where(type =>
                type.Model != MuxModel.Undefined &&
                type.DefaultCards.Any(card => card.Category == CardCategory.STM))
            .OrderBy(type => type.Name)
            .Select(type => new SelectListItem
            {
                Value = type.Id.ToString(),
                Text = type.Name
            })
            .ToListAsync();

        var selected = model.StmAssignments
            .GroupBy(item => item.StmId)
            .ToDictionary(group => group.Key, group => group.First().PortKey);

        // Rebuilt rows must use trusted IDs and labels.
        foreach (string key in ModelState.Keys
            .Where(key => key.StartsWith("StmAssignments[")).ToList())
        {
            ModelState.Remove(key);
        }

        model.StmAssignments = await _context.Stms
            .AsNoTracking()
            .Where(stm =>
                stm.Link.SiteFromId == model.SiteId &&
                stm.Link.LinkType.Name == "SDH" &&
                !_context.MuxPorts.Any(port => port.StmId == stm.Id))
            .OrderBy(stm => stm.Link.SiteToId)
            .ThenBy(stm => stm.Link.Name)
            .ThenBy(stm => stm.Number)
            .Select(stm => new SiteSdhStmPortAssignmentViewModel
            {
                StmId = stm.Id,
                StmNumber = stm.Number,
                LinkName = stm.Link.Name,
                RemoteSiteId = stm.Link.SiteToId
            })
            .ToListAsync();

        foreach (var assignment in model.StmAssignments)
            assignment.PortKey = selected.GetValueOrDefault(assignment.StmId);
    }
}