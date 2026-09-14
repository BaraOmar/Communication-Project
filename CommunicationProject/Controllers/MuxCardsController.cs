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
public class MuxCardsController : Controller
{
    private readonly CommunicationDbContext _context;

    public MuxCardsController(
        CommunicationDbContext context)
    {
        _context = context;
    }

    // GET: MuxCards/Details/{id}
    public async Task<IActionResult> Details(Guid? id)
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

            .Include(item => item.CardType)

            .Include(item => item.Ports)
                .ThenInclude(port => port.E1)
                    .ThenInclude(e1 => e1!.Stm)

            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (card == null)
        {
            return NotFound();
        }

        return View(card);
    }
    // GET: MuxCards/Create
    [HttpGet]
    [Authorize(
        Roles =
            AppRoles.Admin + "," +
            AppRoles.Operator)]
    public async Task<IActionResult> Create(Guid? muxId)
    {
        if (!muxId.HasValue ||
            muxId.Value == Guid.Empty)
        {
            return NotFound();
        }

        var mux = await _context.Muxes
            .AsNoTracking()
            .FirstOrDefaultAsync(item =>
                item.Id == muxId.Value);

        if (mux == null)
        {
            return NotFound();
        }

        var model = new CreateMuxCardViewModel
        {
            MuxId = mux.Id,
            MuxName = mux.Name,

            CardTypeOptions =
                await _context.CardTypes
                    .AsNoTracking()
                    .OrderBy(cardType => cardType.Category)
                    .ThenBy(cardType => cardType.Name)
                    .Select(cardType =>
                        new SelectListItem
                        {
                            Value = cardType.Id.ToString(),

                            Text =
                                cardType.Name +
                                " — " +
                                cardType.Category +
                                " — " +
                                cardType.PortCount +
                                " ports"
                        })
                    .ToListAsync()
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
    CreateMuxCardViewModel model)
    {
        var mux = await _context.Muxes
            .FirstOrDefaultAsync(item =>
                item.Id == model.MuxId);

        if (mux == null)
        {
            return NotFound();
        }

        var cardType = await _context.CardTypes
            .FirstOrDefaultAsync(item =>
                item.Id == model.CardTypeId);

        if (cardType == null)
        {
            ModelState.AddModelError(
                nameof(model.CardTypeId),
                "The selected card type is invalid.");
        }

        bool slotAlreadyUsed =
            await _context.MuxCards
                .AnyAsync(card =>
                    card.MuxId == model.MuxId &&
                    card.SlotNumber == model.SlotNumber);

        if (slotAlreadyUsed)
        {
            ModelState.AddModelError(
                nameof(model.SlotNumber),
                "This slot is already used in the selected MUX.");
        }

        if (!ModelState.IsValid)
        {
            model.MuxName = mux.Name;

            model.CardTypeOptions =
                await _context.CardTypes
                    .AsNoTracking()
                    .OrderBy(cardType => cardType.Category)
                    .ThenBy(cardType => cardType.Name)
                    .Select(cardType =>
                        new SelectListItem
                        {
                            Value = cardType.Id.ToString(),
                            Text =
                                cardType.Name +
                                " — " +
                                cardType.Category +
                                " — " +
                                cardType.PortCount +
                                " ports"
                        })
                    .ToListAsync();

            return View(model);
        }

        var card = new MuxCard
        {
            Id = Guid.NewGuid(),
            MuxId = model.MuxId,
            CardTypeId = model.CardTypeId!.Value,
            SlotNumber = model.SlotNumber
        };

        _context.MuxCards.Add(card);

        /*
         * Automatically create the exact number
         * of ports defined by the CardType.
         */
        for (int portNumber = 1;
             portNumber <= cardType!.PortCount;
             portNumber++)
        {
            _context.MuxPorts.Add(
                new MuxPort
                {
                    Id = Guid.NewGuid(),
                    MuxCardId = card.Id,
                    PortNumber = portNumber,
                    Status = MuxPortStatus.Available
                });
        }

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"Card '{cardType.Name}' was added to slot " +
            $"{card.SlotNumber} successfully.";

        return RedirectToAction(
            "Details",
            "Muxes",
            new { id = model.MuxId });
    }



}