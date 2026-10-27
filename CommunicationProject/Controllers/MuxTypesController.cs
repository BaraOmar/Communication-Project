using CommunicationProject.Data;
using CommunicationProject.Models;
using CommunicationProject.Security;
using CommunicationProject.Services.Muxes;
using CommunicationProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunicationProject.Controllers;

[Authorize]
public class MuxTypesController : Controller
{
    private readonly CommunicationDbContext _context;

    public MuxTypesController(CommunicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var types = await _context.MuxTypes
            .AsNoTracking()
            .Include(type => type.DefaultCards)
            .OrderBy(type => type.Name)
            .ToListAsync();

        return View(types);
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Operator)]
    public IActionResult Create()
    {
        return View(new CreateMuxTypeViewModel());
    }

    [HttpGet]
    public IActionResult GetDefaultCards(MuxModel model)
    {
        if (!MuxStructureRules.IsSupported(model))
            return BadRequest();

        return Json(new
        {
            cards = MuxStructureRules.GetDefaultCards(model)
                .Select(card => new
                {
                    category = (int)card.Category,
                    section = (int)card.Section,
                    card.ShelfNumber,
                    card.SlotNumber,
                    card.PortCount,
                    card.IsRequired
                })
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Operator)]
    public async Task<IActionResult> Create(
    [Bind(Prefix = "")] CreateMuxTypeViewModel model)
    {
        model.Cards ??= [];

        var selectedModel = model.Model ?? MuxModel.Undefined;

        var cards = model.Cards.Select(card => new MuxTypeCard
        {
            Id = Guid.NewGuid(),
            Category = card.Category,
            Section = card.Section,
            ShelfNumber = card.ShelfNumber,
            SlotNumber = card.SlotNumber,
            PortCount = card.PortCount,
            IsRequired = card.IsRequired
        }).ToList();

        foreach (string error in MuxStructureRules.Validate(selectedModel, cards))
            ModelState.AddModelError(string.Empty, error);

        if (MuxStructureRules.IsSupported(selectedModel))
        {
            string name = MuxStructureRules.GetName(selectedModel);

            if (await _context.MuxTypes.AnyAsync(type =>
                type.Model == selectedModel || type.Name == name))
            {
                ModelState.AddModelError(
                    nameof(model.Model),
                    "This MUX model is already configured.");
            }
        }

        if (!ModelState.IsValid)
            return View(model);

        var muxType = new MuxType
        {
            Id = Guid.NewGuid(),
            Name = MuxStructureRules.GetName(selectedModel),
            Model = selectedModel,
            DefaultCards = cards
        };

        foreach (var card in cards)
            card.MuxTypeId = muxType.Id;

        _context.MuxTypes.Add(muxType);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"MUX type '{muxType.Name}' was created successfully.";

        return RedirectToAction(nameof(Index));
    }
}