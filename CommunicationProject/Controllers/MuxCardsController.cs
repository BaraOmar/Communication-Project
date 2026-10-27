using CommunicationProject.Data;
using CommunicationProject.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CommunicationProject.Models;
using CommunicationProject.Services.Muxes;
using CommunicationProject.ViewModels;
using System.Data;

namespace CommunicationProject.Controllers;

[Authorize(
    Roles =
        AppRoles.Admin + "," +
        AppRoles.Operator + "," +
        AppRoles.Viewer)]
public class MuxCardsController : Controller
{
    private readonly CommunicationDbContext _context;

    public MuxCardsController(
        CommunicationDbContext context)
    {
        _context = context;
    }


    // GET: MuxCards/Details/{id}
    public async Task<IActionResult> Details(
        Guid? id)
    {
        if (!id.HasValue ||
            id.Value == Guid.Empty)
        {
            return NotFound();
        }

        var card = await _context.MuxCards
            .AsNoTracking()

            .Include(item => item.Mux)
                .ThenInclude(mux => mux.Site)

            .Include(item => item.Mux)
                .ThenInclude(mux => mux.MuxType)

            .Include(item => item.Ports)
                .ThenInclude(port => port.E1)

            .Include(item => item.Ports)
                .ThenInclude(port => port.Stm)
                    .ThenInclude(stm => stm!.Link)

            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (card == null)
        {
            return NotFound();
        }

        return View(card);
    }
    [HttpGet]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Operator)]
    public async Task<IActionResult> Create(Guid muxId)
    {
        var mux = await _context.Muxes
            .AsNoTracking()
            .Include(item => item.Cards)
            .FirstOrDefaultAsync(item => item.Id == muxId);

        if (mux == null)
            return NotFound();

        if (!MuxStructureRules.IsSupported(mux.Model) ||
            MuxStructureRules.HasFixedStructure(mux.Model))
        {
            TempData["ErrorMessage"] =
                "This model uses its predefined card structure.";

            return RedirectToAction(
                "Details", "Muxes", new { id = mux.Id });
        }

        int shelf = MuxStructureRules.UsesShelves(mux.Model) ? 1 : 0;

        int lastCard = mux.Cards
            .Where(card => card.ShelfNumber == shelf)
            .Select(card => card.SlotNumber)
            .DefaultIfEmpty(0)
            .Max();

        return View(new CreateMuxCardViewModel
        {
            MuxId = mux.Id,
            MuxName = mux.Name,
            Model = mux.Model,
            ShelfNumber = shelf,
            SlotNumber = lastCard + 1
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Operator)]
    public async Task<IActionResult> Create(CreateMuxCardViewModel model)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var mux = await _context.Muxes
            .Include(item => item.Cards)
                .ThenInclude(card => card.Ports)
            .FirstOrDefaultAsync(item => item.Id == model.MuxId);

        if (mux == null)
            return NotFound();

        model.MuxName = mux.Name;
        model.Model = mux.Model;

        if (!MuxStructureRules.IsSupported(mux.Model) ||
            MuxStructureRules.HasFixedStructure(mux.Model))
        {
            TempData["ErrorMessage"] =
                "This model uses its predefined card structure.";

            return RedirectToAction(
                "Details", "Muxes", new { id = mux.Id });
        }

        var definitions = mux.Cards.Select(card => new MuxTypeCard
        {
            Category = card.Category,
            Section = card.Section,
            ShelfNumber = card.ShelfNumber,
            SlotNumber = card.SlotNumber,
            PortCount = card.Ports.Count,
            IsRequired = card.IsRequired
        }).ToList();

        definitions.Add(new MuxTypeCard
        {
            Category = model.Category,
            Section = MuxSection.Standard,
            ShelfNumber = model.ShelfNumber,
            SlotNumber = model.SlotNumber,
            PortCount = model.PortCount,
            IsRequired = false
        });

        foreach (string error in MuxStructureRules.Validate(
            mux.Model, definitions))
        {
            ModelState.AddModelError(string.Empty, error);
        }

        if (!ModelState.IsValid)
            return View(model);

        var newCard = new MuxCard
        {
            Id = Guid.NewGuid(),
            MuxId = mux.Id,
            Category = model.Category,
            Section = MuxSection.Standard,
            ShelfNumber = model.ShelfNumber,
            SlotNumber = model.SlotNumber,
            IsRequired = false
        };

        for (int number = 1; number <= model.PortCount; number++)
        {
            newCard.Ports.Add(new MuxPort
            {
                Id = Guid.NewGuid(),
                MuxCardId = newCard.Id,
                PortNumber = number,
                Status = MuxPortStatus.Available
            });
        }

        try
        {
            _context.MuxCards.Add(newCard);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            _context.ChangeTracker.Clear();

            ModelState.AddModelError(
                string.Empty,
                "The card could not be added. Check whether its position is already occupied.");

            return View(model);
        }

        TempData["SuccessMessage"] =
            $"Card {newCard.SlotNumber} added to '{mux.Name}'.";

        return RedirectToAction(
            "Details", "Muxes", new { id = mux.Id });
    }
}