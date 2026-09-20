using CommunicationProject.Data;
using CommunicationProject.Models;
using CommunicationProject.Security;
using CommunicationProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunicationProject.Controllers;

[Authorize]
public class MuxTypesController : Controller
{
    private readonly CommunicationDbContext _context;

    public MuxTypesController(
        CommunicationDbContext context)
    {
        _context = context;
    }


    // GET: MuxTypes
    public async Task<IActionResult> Index()
    {
        var muxTypes = await _context.MuxTypes
            .AsNoTracking()
            .OrderBy(type => type.Name)
            .ToListAsync();

        return View(muxTypes);
    }


    // GET: MuxTypes/Create
    [HttpGet]
    [Authorize(
        Roles =
            AppRoles.Admin + "," +
            AppRoles.Operator)]
    public IActionResult Create()
    {
        return View(
            new CreateMuxTypeViewModel());
    }


    // POST: MuxTypes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(
        Roles =
            AppRoles.Admin + "," +
            AppRoles.Operator)]
    public async Task<IActionResult> Create(
        CreateMuxTypeViewModel model)
    {
        model.Name =
            model.Name?.Trim() ?? string.Empty;

        bool duplicateExists =
            await _context.MuxTypes
                .AnyAsync(type =>
                    type.Name == model.Name);

        if (duplicateExists)
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "A MUX type with this name already exists.");
        }


        if (model.PowerCardCount == 0 &&
            model.StmCardCount == 0 &&
            model.E1CardCount == 0)
        {
            ModelState.AddModelError(
                string.Empty,
                "The MUX type must contain at least one card.");
        }


        ValidateCardCapacity(
            model.PowerCardCount,
            model.PowerPortsPerCard,
            nameof(model.PowerPortsPerCard),
            "Power");

        ValidateCardCapacity(
            model.StmCardCount,
            model.StmPortsPerCard,
            nameof(model.StmPortsPerCard),
            "STM");

        ValidateCardCapacity(
            model.E1CardCount,
            model.E1PortsPerCard,
            nameof(model.E1PortsPerCard),
            "E1");


        if (!ModelState.IsValid)
        {
            return View(model);
        }


        var muxType = new MuxType
        {
            Id = Guid.NewGuid(),
            Name = model.Name,

            PowerCardCount =
                model.PowerCardCount,

            PowerPortsPerCard =
                model.PowerPortsPerCard,

            StmCardCount =
                model.StmCardCount,

            StmPortsPerCard =
                model.StmPortsPerCard,

            E1CardCount =
                model.E1CardCount,

            E1PortsPerCard =
                model.E1PortsPerCard,

            // Temporary until the old shelf workflow is removed.

        };


        _context.MuxTypes.Add(muxType);

        await _context.SaveChangesAsync();


        TempData["SuccessMessage"] =
            $"MUX type '{muxType.Name}' was created successfully.";

        return RedirectToAction(nameof(Index));
    }


    private void ValidateCardCapacity(
        int cardCount,
        int portsPerCard,
        string portsPropertyName,
        string categoryName)
    {
        if (cardCount > 0 &&
            portsPerCard <= 0)
        {
            ModelState.AddModelError(
                portsPropertyName,
                $"Enter the number of ports for each {categoryName} card.");
        }

        if (cardCount == 0 &&
            portsPerCard > 0)
        {
            ModelState.AddModelError(
                portsPropertyName,
                $"{categoryName} ports must be 0 when there are no {categoryName} cards.");
        }
    }
}