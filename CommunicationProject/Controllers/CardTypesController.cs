using CommunicationProject.Data;
using CommunicationProject.Models;
using CommunicationProject.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunicationProject.Controllers;

[Authorize(
    Roles =
        AppRoles.Admin + "," +
        AppRoles.Operator)]
public class CardTypesController : Controller
{
    private readonly CommunicationDbContext _context;

    public CardTypesController(
        CommunicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var cardTypes = await _context.CardTypes
            .AsNoTracking()
            .OrderBy(cardType => cardType.Category)
            .ThenBy(cardType => cardType.Name)
            .ToListAsync();

        return View(cardTypes);
    }
    [HttpGet]
    public IActionResult Create()
    {
        return View(new CardType());
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CardType model)
    {
        model.Name =
            model.Name?.Trim() ?? string.Empty;

        if (await _context.CardTypes
            .AnyAsync(cardType =>
                cardType.Name == model.Name))
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "A card type with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.Id = Guid.NewGuid();

        _context.CardTypes.Add(model);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"Card type '{model.Name}' was created successfully.";

        return RedirectToAction(nameof(Index));
    }
    [HttpGet]
    public async Task<IActionResult> Edit(Guid? id)
    {
        if (!id.HasValue ||
            id.Value == Guid.Empty)
        {
            return NotFound();
        }

        var cardType = await _context.CardTypes
            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (cardType == null)
        {
            return NotFound();
        }

        return View(cardType);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        CardType model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }

        model.Name =
            model.Name?.Trim() ?? string.Empty;

        bool duplicateExists =
            await _context.CardTypes
                .AnyAsync(cardType =>
                    cardType.Id != id &&
                    cardType.Name == model.Name);

        if (duplicateExists)
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "A card type with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existingCardType =
            await _context.CardTypes
                .FirstOrDefaultAsync(item =>
                    item.Id == id);

        if (existingCardType == null)
        {
            return NotFound();
        }

        existingCardType.Name =
            model.Name;

        existingCardType.Category =
            model.Category;
        bool cardTypeIsInUse =
    await _context.MuxCards
        .AnyAsync(card =>
            card.CardTypeId == id);

        if (cardTypeIsInUse &&
            existingCardType.PortCount != model.PortCount)
        {
            ModelState.AddModelError(
                nameof(model.PortCount),
                "Port count cannot be changed because this card type is already installed in a MUX.");

            return View(model);
        }
        if (cardTypeIsInUse &&
    existingCardType.Category != model.Category)
        {
            ModelState.AddModelError(
                nameof(model.Category),
                "Category cannot be changed because this card type is already installed in a MUX.");

            return View(model);
        }
        existingCardType.PortCount =
            model.PortCount;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"Card type '{existingCardType.Name}' was updated successfully.";

        return RedirectToAction(nameof(Index));
    }



}