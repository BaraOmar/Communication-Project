using CommunicationProject.Data;
using CommunicationProject.Models;
using CommunicationProject.Security;
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
        return View();
    }

    // POST: MuxTypes/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(
        Roles =
            AppRoles.Admin + "," +
            AppRoles.Operator)]
    public async Task<IActionResult> Create(
        MuxType muxType)
    {
        muxType.Name =
            muxType.Name?.Trim() ?? string.Empty;

        bool duplicateExists =
            await _context.MuxTypes
                .AnyAsync(type =>
                    type.Name == muxType.Name);

        if (duplicateExists)
        {
            ModelState.AddModelError(
                nameof(muxType.Name),
                "A MUX type with this name already exists.");
        }

        if (!ModelState.IsValid)
        {
            return View(muxType);
        }

        muxType.Id = Guid.NewGuid();

        _context.MuxTypes.Add(muxType);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"MUX type '{muxType.Name}' was created successfully.";

        return RedirectToAction(nameof(Index));
    }
}