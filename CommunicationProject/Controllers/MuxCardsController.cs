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

            .Include(item => item.Mux)
                .ThenInclude(mux => mux.MuxType)

            .Include(item => item.CardType)

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


    // GET: MuxCards/Create
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
            .Include(item => item.MuxType)
            .FirstOrDefaultAsync(item =>
                item.Id == muxId.Value);

        if (mux == null)
        {
            return NotFound();
        }
        var existingCards = await _context.MuxCards
    .AsNoTracking()
    .Where(card => card.MuxId == mux.Id)
    .ToListAsync();
        var model = new CreateMuxCardViewModel
        {
            MuxId = mux.Id,
            MuxName = mux.Name,

            HasShelves = mux.MuxType.HasShelves,

            ShelfCount = mux.ShelfCount,
            CardSlotCount = mux.CardSlotCount,

            OccupiedSlotNumbers = existingCards
    .Where(card => !card.ShelfNumber.HasValue)
    .Select(card => card.SlotNumber)
    .Distinct()
    .ToList(),

            OccupiedSlotsByShelf = existingCards
    .Where(card => card.ShelfNumber.HasValue)
    .GroupBy(card => card.ShelfNumber!.Value)
    .ToDictionary(
        group => group.Key,
        group => group
            .Select(card => card.SlotNumber)
            .Distinct()
            .ToList()),

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
            .Include(item => item.MuxType)
            .FirstOrDefaultAsync(item =>
                item.Id == model.MuxId);

        if (mux == null)
        {
            return NotFound();
        }

        /*
         * Legacy MUXes may not have a type assigned.
         * Cards cannot be added until the MUX has
         * a valid physical definition.
         */
        if (mux.MuxType == null)
        {
            TempData["ErrorMessage"] =
                "This MUX does not have a MUX type.";

            return RedirectToAction(
                "Details",
                "Muxes",
                new { id = mux.Id });
        }

        model.HasShelves =
            mux.MuxType.HasShelves;

        model.ShelfCount =
            mux.ShelfCount;

        model.CardSlotCount =
            mux.CardSlotCount;


        /*
         * Validate shelf capacity.
         */
        if (mux.MuxType.HasShelves)
        {
            if (!mux.ShelfCount.HasValue)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This MUX does not have a declared shelf capacity yet.");
            }
            else if (!model.ShelfNumber.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.ShelfNumber),
                    "Shelf number is required for this MUX type.");
            }
            else if (model.ShelfNumber.Value < 1 ||
                     model.ShelfNumber.Value >
                     mux.ShelfCount.Value)
            {
                ModelState.AddModelError(
                    nameof(model.ShelfNumber),
                    $"Shelf number must be between 1 and {mux.ShelfCount.Value}.");
            }
        }
        else
        {
            model.ShelfNumber = null;
        }


        /*
         * Validate card-slot capacity.
         */
        if (!mux.CardSlotCount.HasValue)
        {
            ModelState.AddModelError(
                string.Empty,
                "This MUX does not have a declared card capacity yet.");
        }
        else if (model.SlotNumber < 1 ||
                 model.SlotNumber >
                 mux.CardSlotCount.Value)
        {
            ModelState.AddModelError(
                nameof(model.SlotNumber),
                mux.MuxType.HasShelves
                    ? $"Card number must be between 1 and {mux.CardSlotCount.Value} for each shelf."
                    : $"Card number must be between 1 and {mux.CardSlotCount.Value}.");
        }


        /*
         * Validate card type.
         */
        var cardType = await _context.CardTypes
            .FirstOrDefaultAsync(item =>
                item.Id == model.CardTypeId);

        if (cardType == null)
        {
            ModelState.AddModelError(
                nameof(model.CardTypeId),
                "The selected card type is invalid.");
        }


        /*
         * Prevent duplicate physical positions.
         *
         * Shelf MUX:
         * Shelf 1 / Card 2 can exist once.
         *
         * Non-shelf MUX:
         * Card 2 can exist once.
         */
        bool slotAlreadyUsed =
            await _context.MuxCards
                .AnyAsync(card =>
                    card.MuxId == model.MuxId &&
                    card.ShelfNumber == model.ShelfNumber &&
                    card.SlotNumber == model.SlotNumber);

        if (slotAlreadyUsed)
        {
            ModelState.AddModelError(
                nameof(model.SlotNumber),
                mux.MuxType.HasShelves
                    ? "This card number is already used in the selected shelf."
                    : "This card number is already used in this MUX.");
        }


        if (!ModelState.IsValid)
        {
            model.MuxName = mux.Name;

            model.HasShelves =
                mux.MuxType.HasShelves;

            model.ShelfCount =
                mux.ShelfCount;

            model.CardSlotCount =
                mux.CardSlotCount;

            var existingCards = await _context.MuxCards
    .AsNoTracking()
    .Where(card =>
        card.MuxId == mux.Id)
    .ToListAsync();

            model.OccupiedSlotNumbers =
                existingCards
                    .Where(card =>
                        !card.ShelfNumber.HasValue)
                    .Select(card =>
                        card.SlotNumber)
                    .Distinct()
                    .ToList();

            model.OccupiedSlotsByShelf =
                existingCards
                    .Where(card =>
                        card.ShelfNumber.HasValue)
                    .GroupBy(card =>
                        card.ShelfNumber!.Value)
                    .ToDictionary(
                        group => group.Key,
                        group => group
                            .Select(card =>
                                card.SlotNumber)
                            .Distinct()
                            .ToList());

            model.CardTypeOptions =
                await _context.CardTypes
                    .AsNoTracking()
                    .OrderBy(cardType =>
                        cardType.Category)
                    .ThenBy(cardType =>
                        cardType.Name)
                    .Select(cardType =>
                        new SelectListItem
                        {
                            Value =
                                cardType.Id.ToString(),

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
            ShelfNumber = model.ShelfNumber,
            SlotNumber = model.SlotNumber
        };

        _context.MuxCards.Add(card);


        /*
         * Automatically create all physical
         * ports defined by the card type.
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


        if (mux.MuxType.HasShelves)
        {
            TempData["SuccessMessage"] =
                $"Card '{cardType.Name}' was added to " +
                $"Shelf {card.ShelfNumber}, " +
                $"Card {card.SlotNumber} successfully.";
        }
        else
        {
            TempData["SuccessMessage"] =
                $"Card '{cardType.Name}' was added as " +
                $"Card {card.SlotNumber} successfully.";
        }


        return RedirectToAction(
            "Details",
            "Muxes",
            new { id = model.MuxId });
    }


}