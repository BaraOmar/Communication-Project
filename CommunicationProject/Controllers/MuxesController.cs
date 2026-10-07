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
.Include(mux => mux.MuxType)
.Include(mux => mux.Cards)
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

        await PopulateOptionsAsync(model);

        return View(model);
    }


    // POST: Muxes/Create
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
            int totalCards =
                muxType.PowerCardCount +
                muxType.StmCardCount +
                muxType.E1CardCount;

            bool invalidCapacity =
                (muxType.PowerCardCount > 0 &&
                 muxType.PowerPortsPerCard <= 0) ||

                (muxType.StmCardCount > 0 &&
                 muxType.StmPortsPerCard <= 0) ||

                (muxType.E1CardCount > 0 &&
                 muxType.E1PortsPerCard <= 0);

            if (totalCards == 0 ||
                invalidCapacity)
            {
                ModelState.AddModelError(
                    nameof(model.MuxTypeId),
                    "The selected MUX type does not have a valid card and port configuration.");
            }
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
            await PopulateOptionsAsync(model);

            return View(model);
        }


        var mux = new Mux
        {
            Id = Guid.NewGuid(),
            Name = model.Name,
            SiteId = model.SiteId,
            MuxTypeId = muxType!.Id
        };


        int nextSlotNumber = 1;

        AddCards(
            mux,
            CardCategory.Power,
            muxType.PowerCardCount,
            muxType.PowerPortsPerCard,
            ref nextSlotNumber);

        AddCards(
            mux,
            CardCategory.STM,
            muxType.StmCardCount,
            muxType.StmPortsPerCard,
            ref nextSlotNumber);

        AddCards(
            mux,
            CardCategory.E1,
            muxType.E1CardCount,
            muxType.E1PortsPerCard,
            ref nextSlotNumber);


        _context.Muxes.Add(mux);

        await _context.SaveChangesAsync();


        TempData["SuccessMessage"] =
            $"MUX '{mux.Name}' was installed with " +
            $"{mux.Cards.Count} cards and " +
            $"{mux.Cards.Sum(card => card.Ports.Count)} ports.";

        return RedirectToAction(
            nameof(Details),
            new { id = mux.Id });
    }
    // GET: Muxes/InstallForSite
    [HttpGet]
    [Authorize(
        Roles =
            AppRoles.Admin + "," +
            AppRoles.Operator)]
    public async Task<IActionResult> InstallForSite(
        string? siteId)
    {
        siteId =
            siteId?.Trim() ?? string.Empty;


        if (string.IsNullOrWhiteSpace(siteId))
        {
            return NotFound();
        }


        /*
         * Make sure the site exists.
         */
        var site =
            await _context.Sites
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.Id == siteId);


        if (site is null)
        {
            return NotFound();
        }


        /*
         * Only allow this workflow when the site
         * actually has at least one SDH link.
         *
         * Because CommunicationLink is directional,
         * the resources physically belonging to this
         * site are stored on the record where:
         *
         * SiteFromId == siteId
         */
        bool hasSdhLink =
            await _context.CommunicationLinks
                .AsNoTracking()
                .AnyAsync(link =>
                    link.SiteFromId == siteId &&

                    link.LinkType.Name == "SDH");


        if (!hasSdhLink)
        {
            TempData["ErrorMessage"] =
                "This site does not have any SDH links.";

            return RedirectToAction(
                "Details",
                "Sites",
                new { id = siteId });
        }


        /*
         * Load all SDH STMs physically belonging
         * to this site.
         *
         * IMPORTANT:
         * Exclude STMs that are already installed
         * on another MUX port.
         */
        var stmAssignments =
            await _context.Stms
                .AsNoTracking()

                .Where(stm =>
                    stm.Link.SiteFromId == siteId &&

                    stm.Link.LinkType.Name == "SDH" &&

                    !_context.MuxPorts.Any(port =>
                        port.StmId == stm.Id))

                .OrderBy(stm =>
                    stm.Link.SiteToId)

                .ThenBy(stm =>
                    stm.Link.Name)

                .ThenBy(stm =>
                    stm.Number)

                .Select(stm =>
                    new SiteSdhStmPortAssignmentViewModel
                    {
                        StmId =
                            stm.Id,

                        StmNumber =
                            stm.Number,

                        LinkName =
                            stm.Link.Name,

                        RemoteSiteId =
                            stm.Link.SiteToId
                    })

                .ToListAsync();


        /*
         * Generate a sensible default MUX name.
         *
         * Example:
         * 001 MUX 1
         * 001 MUX 2
         */
        int existingMuxCount =
            await _context.Muxes
                .AsNoTracking()
                .CountAsync(mux =>
                    mux.SiteId == siteId);


        var model =
            new InstallSiteMuxViewModel
            {
                SiteId =
                    site.Id,

                SiteName =
                    site.Name,

                MuxName =
                    $"{site.Id} MUX {existingMuxCount + 1}",

                StmAssignments =
                    stmAssignments
            };


        await PopulateInstallMuxTypeOptionsAsync(
            model);


        return View(
            "InstallForSite",
            model);
    }
    // POST: Muxes/InstallForSite
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(
        Roles =
            AppRoles.Admin + "," +
            AppRoles.Operator)]
    public async Task<IActionResult> InstallForSite(
        InstallSiteMuxViewModel model)
    {
        model.SiteId =
            model.SiteId?.Trim() ?? string.Empty;

        model.MuxName =
            model.MuxName?.Trim() ?? string.Empty;


        /* =====================================================
           Validate Site
           ===================================================== */

        var site =
            await _context.Sites
                .AsNoTracking()
                .FirstOrDefaultAsync(item =>
                    item.Id == model.SiteId);


        if (site is null)
        {
            return NotFound();
        }


        model.SiteName =
            site.Name;


        bool hasSdhLink =
            await _context.CommunicationLinks
                .AsNoTracking()
                .AnyAsync(link =>
                    link.SiteFromId == model.SiteId &&
                    link.LinkType.Name == "SDH");


        if (!hasSdhLink)
        {
            ModelState.AddModelError(
                string.Empty,
                "This site does not have any SDH links.");
        }


        /* =====================================================
           Validate MUX Type
           ===================================================== */

        MuxType? muxType = null;


        if (model.MuxTypeId.HasValue)
        {
            muxType =
                await _context.MuxTypes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(type =>
                        type.Id ==
                        model.MuxTypeId.Value);
        }


        if (muxType is null)
        {
            ModelState.AddModelError(
                nameof(model.MuxTypeId),
                "The selected MUX type is invalid.");
        }
        else if (
            muxType.StmCardCount <= 0 ||
            muxType.StmPortsPerCard <= 0)
        {
            ModelState.AddModelError(
                nameof(model.MuxTypeId),
                "The selected MUX type does not contain valid STM ports.");
        }


        /* =====================================================
           Validate MUX Name
           ===================================================== */

        bool duplicateMuxName =
            await _context.Muxes
                .AnyAsync(mux =>
                    mux.SiteId == model.SiteId &&
                    mux.Name == model.MuxName);


        if (duplicateMuxName)
        {
            ModelState.AddModelError(
                nameof(model.MuxName),
                "A MUX with this name already exists at this site.");
        }


        /* =====================================================
           Load trusted available STMs from database
           ===================================================== */

        var availableStms =
            await _context.Stms
                .AsNoTracking()

                .Where(stm =>
                    stm.Link.SiteFromId ==
                        model.SiteId &&

                    stm.Link.LinkType.Name ==
                        "SDH" &&

                    !_context.MuxPorts.Any(port =>
                        port.StmId == stm.Id))

                .Select(stm => new
                {
                    stm.Id,
                    stm.Number,

                    LinkName =
                        stm.Link.Name,

                    RemoteSiteId =
                        stm.Link.SiteToId
                })

                .ToListAsync();


        var availableStmIds =
            availableStms
                .Select(stm =>
                    stm.Id)
                .ToHashSet();


        /* =====================================================
           Validate Posted STM IDs
           ===================================================== */

        bool containsInvalidStm =
            model.StmAssignments
                .Any(assignment =>
                    !availableStmIds.Contains(
                        assignment.StmId));


        if (containsInvalidStm)
        {
            ModelState.AddModelError(
                string.Empty,
                "One of the selected STMs is invalid or is already assigned to another MUX.");
        }


        bool containsDuplicateStm =
            model.StmAssignments
                .GroupBy(assignment =>
                    assignment.StmId)
                .Any(group =>
                    group.Count() > 1);


        if (containsDuplicateStm)
        {
            ModelState.AddModelError(
                string.Empty,
                "An STM cannot appear more than once.");
        }


        /* =====================================================
           Selected Assignments
           ===================================================== */

        var selectedAssignments =
            model.StmAssignments
                .Where(assignment =>
                    !string.IsNullOrWhiteSpace(
                        assignment.PortKey))
                .ToList();


        if (selectedAssignments.Count == 0)
        {
            ModelState.AddModelError(
                string.Empty,
                "Assign at least one STM to a MUX port.");
        }


        /* =====================================================
           Prevent Same MUX Port Being Used Twice
           ===================================================== */

        bool duplicatePort =
            selectedAssignments
                .GroupBy(
                    assignment =>
                        assignment.PortKey!.Trim(),
                    StringComparer.OrdinalIgnoreCase)

                .Any(group =>
                    group.Count() > 1);


        if (duplicatePort)
        {
            ModelState.AddModelError(
                string.Empty,
                "A MUX port cannot be assigned to more than one STM.");
        }


        /* =====================================================
           Validate MUX Capacity / Port Positions
           ===================================================== */

        if (muxType is not null)
        {
            int totalStmPorts =
                muxType.StmCardCount *
                muxType.StmPortsPerCard;


            if (selectedAssignments.Count >
                totalStmPorts)
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"The selected MUX contains only " +
                    $"{totalStmPorts} STM ports.");
            }


            int firstStmSlot =
                muxType.PowerCardCount + 1;


            int lastStmSlot =
                muxType.PowerCardCount +
                muxType.StmCardCount;


            foreach (var assignment
                     in selectedAssignments)
            {
                if (!TryParsePortKey(
                        assignment.PortKey,
                        out int slotNumber,
                        out int portNumber) ||

                    slotNumber <
                        firstStmSlot ||

                    slotNumber >
                        lastStmSlot ||

                    portNumber < 1 ||

                    portNumber >
                        muxType.StmPortsPerCard)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "One of the selected STM ports is invalid.");
                }
            }
        }


        /* =====================================================
           Rebuild ViewModel from trusted DB data
           ===================================================== */

        var postedPortLookup =
            model.StmAssignments

                .GroupBy(assignment =>
                    assignment.StmId)

                .ToDictionary(
                    group =>
                        group.Key,

                    group =>
                        group.First().PortKey);


        model.StmAssignments =
            availableStms

                .OrderBy(stm =>
                    stm.RemoteSiteId)

                .ThenBy(stm =>
                    stm.LinkName)

                .ThenBy(stm =>
                    stm.Number)

                .Select(stm =>
                    new SiteSdhStmPortAssignmentViewModel
                    {
                        StmId =
                            stm.Id,

                        StmNumber =
                            stm.Number,

                        LinkName =
                            stm.LinkName,

                        RemoteSiteId =
                            stm.RemoteSiteId,

                        PortKey =
                            postedPortLookup
                                .GetValueOrDefault(
                                    stm.Id)
                    })

                .ToList();


        /* =====================================================
           Invalid Form
           ===================================================== */

        if (!ModelState.IsValid)
        {
            await PopulateInstallMuxTypeOptionsAsync(
                model);

            return View(
                "InstallForSite",
                model);
        }


        /* =====================================================
           Re-check Selected STMs
           ===================================================== */

        var selectedStmIds =
            selectedAssignments
                .Select(assignment =>
                    assignment.StmId)
                .ToList();


        bool stmAlreadyAssigned =
            await _context.MuxPorts
                .AnyAsync(port =>
                    port.StmId.HasValue &&

                    selectedStmIds.Contains(
                        port.StmId.Value));


        if (stmAlreadyAssigned)
        {
            ModelState.AddModelError(
                string.Empty,
                "One of the selected STMs was assigned to another MUX before this request was completed.");

            await PopulateInstallMuxTypeOptionsAsync(
                model);

            return View(
                "InstallForSite",
                model);
        }


        /* =====================================================
           Create MUX
           ===================================================== */

        var mux =
            new Mux
            {
                Id =
                    Guid.NewGuid(),

                Name =
                    model.MuxName,

                SiteId =
                    model.SiteId,

                MuxTypeId =
                    muxType!.Id
            };


        int nextSlotNumber = 1;


        AddCards(
            mux,
            CardCategory.Power,
            muxType.PowerCardCount,
            muxType.PowerPortsPerCard,
            ref nextSlotNumber);


        AddCards(
            mux,
            CardCategory.STM,
            muxType.StmCardCount,
            muxType.StmPortsPerCard,
            ref nextSlotNumber);


        AddCards(
            mux,
            CardCategory.E1,
            muxType.E1CardCount,
            muxType.E1PortsPerCard,
            ref nextSlotNumber);


        /* =====================================================
           Connect STMs to Selected Ports
           ===================================================== */

        foreach (var assignment
                 in selectedAssignments)
        {
            TryParsePortKey(
                assignment.PortKey,
                out int slotNumber,
                out int portNumber);


            var port =
                mux.Cards

                    .Where(card =>
                        card.Category ==
                            CardCategory.STM &&

                        card.SlotNumber ==
                            slotNumber)

                    .SelectMany(card =>
                        card.Ports)

                    .Single(item =>
                        item.PortNumber ==
                            portNumber);


            port.StmId =
                assignment.StmId;


            port.Status =
                MuxPortStatus.Connected;
        }


        /* =====================================================
           Save
           ===================================================== */

        _context.Muxes.Add(
            mux);


        await _context.SaveChangesAsync();


        TempData["SuccessMessage"] =
            $"MUX '{mux.Name}' was installed at site " +
            $"'{model.SiteId}' and " +
            $"{selectedAssignments.Count} STM(s) were connected.";


        return RedirectToAction(
            "Details",
            "Sites",
            new
            {
                id = model.SiteId
            });
    }

    // GET: Muxes/InstallForSdhLink
    [HttpGet]
    [Authorize(
        Roles =
            AppRoles.Admin + "," +
            AppRoles.Operator)]
    public async Task<IActionResult> InstallForSdhLink(
        Guid linkId,
        string? siteId)
    {
        siteId =
            siteId?.Trim() ?? string.Empty;

        if (linkId == Guid.Empty ||
            string.IsNullOrWhiteSpace(siteId))
        {
            return NotFound();
        }

        /*
         * Select the directional link whose resources
         * physically belong to the selected site.
         */
        var link =
            await _context.CommunicationLinks
                .AsNoTracking()
                .Include(item => item.LinkType)
                .Include(item => item.Stms)
                .FirstOrDefaultAsync(item =>
                    (
                        item.Id == linkId ||
                        item.ConnectedLinkId == linkId
                    ) &&
                    item.SiteFromId == siteId);

        if (link is null)
        {
            return NotFound();
        }

        if (!string.Equals(
                link.LinkType.Name,
                "SDH",
                StringComparison.OrdinalIgnoreCase))
        {
            TempData["ErrorMessage"] =
                "Only SDH links can be connected to a MUX.";

            return RedirectToAction(
                "Details",
                "CommunicationLinks",
                new { id = linkId });
        }

        var model =
            new InstallSdhLinkMuxViewModel
            {
                LinkId = link.Id,
                SiteId = siteId,
                LinkName = link.Name,

                MuxName =
                    $"{link.Name} MUX",

                StmAssignments =
                    link.Stms
                        .OrderBy(stm =>
                            int.TryParse(
                                stm.Number,
                                out int number)
                                    ? number
                                    : int.MaxValue)
                        .Select(stm =>
                            new SdhStmPortAssignmentViewModel
                            {
                                StmId = stm.Id,
                                StmNumber = stm.Number
                            })
                        .ToList()
            };

        await PopulateInstallMuxTypeOptionsAsync(
            model);

        return View(model);
    }


    // GET: Muxes/GetMuxTypeStmPorts
    [HttpGet]
    public async Task<IActionResult> GetMuxTypeStmPorts(
        Guid? muxTypeId)
    {
        if (!muxTypeId.HasValue ||
            muxTypeId.Value == Guid.Empty)
        {
            return Json(Array.Empty<object>());
        }

        var muxType =
            await _context.MuxTypes
                .AsNoTracking()
                .FirstOrDefaultAsync(type =>
                    type.Id == muxTypeId.Value);

        if (muxType is null ||
            muxType.StmCardCount <= 0 ||
            muxType.StmPortsPerCard <= 0)
        {
            return Json(Array.Empty<object>());
        }

        var ports =
            new List<object>();

        int firstStmSlot =
            muxType.PowerCardCount + 1;

        for (int cardNumber = 1;
             cardNumber <= muxType.StmCardCount;
             cardNumber++)
        {
            int slotNumber =
                firstStmSlot +
                cardNumber -
                1;

            for (int portNumber = 1;
                 portNumber <= muxType.StmPortsPerCard;
                 portNumber++)
            {
                ports.Add(
                    new
                    {
                        value =
                            $"{slotNumber}:{portNumber}",

                        text =
                            $"STM Card {cardNumber} " +
                            $"(Slot {slotNumber}) / " +
                            $"Port {portNumber}"
                    });
            }
        }

        return Json(ports);
    }
    // GET: Muxes/Details/{id}
    public async Task<IActionResult> Details(
        Guid? id)
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
                .ThenInclude(card => card.Ports)
            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (mux == null)
        {
            return NotFound();
        }

        return View(mux);
    }


    private async Task PopulateOptionsAsync(
        CreateMuxViewModel model)
    {
        model.SiteOptions =
            await _context.Sites
                .AsNoTracking()
                .OrderBy(site => site.Name)
                .Select(site =>
                    new SelectListItem
                    {
                        Value = site.Id,
                        Text =
                            site.Id +
                            " - " +
                            site.Name
                    })
                .ToListAsync();


        var muxTypes = await _context.MuxTypes
            .AsNoTracking()
            .OrderBy(type => type.Name)
            .ToListAsync();


        model.MuxTypeOptions =
            muxTypes
                .Select(type =>
                    new SelectListItem
                    {
                        Value =
                            type.Id.ToString(),

                        Text =
                            type.Name +
                            " — " +
                            (
                                type.PowerCardCount +
                                type.StmCardCount +
                                type.E1CardCount
                            ) +
                            " cards"
                    })
                .ToList();
    }


    private static void AddCards(
        Mux mux,
        CardCategory category,
        int cardCount,
        int portsPerCard,
        ref int nextSlotNumber)
    {
        for (int cardNumber = 1;
             cardNumber <= cardCount;
             cardNumber++)
        {
            var card = new MuxCard
            {
                Id = Guid.NewGuid(),
                MuxId = mux.Id,
                Category = category,
                SlotNumber = nextSlotNumber
            };

            for (int portNumber = 1;
                 portNumber <= portsPerCard;
                 portNumber++)
            {
                card.Ports.Add(
                    new MuxPort
                    {
                        Id = Guid.NewGuid(),
                        MuxCardId = card.Id,
                        PortNumber = portNumber,
                        Status = MuxPortStatus.Available
                    });
            }

            mux.Cards.Add(card);

            nextSlotNumber++;
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(
    Roles =
        AppRoles.Admin + "," +
        AppRoles.Operator)]
    public async Task<IActionResult> InstallForSdhLink(
    InstallSdhLinkMuxViewModel model)
    {
        model.SiteId =
            model.SiteId?.Trim() ?? string.Empty;

        model.MuxName =
            model.MuxName?.Trim() ?? string.Empty;


        var link =
            await _context.CommunicationLinks
                .AsNoTracking()
                .Include(item => item.LinkType)
                .Include(item => item.Stms)
                .FirstOrDefaultAsync(item =>
                    item.Id == model.LinkId &&
                    item.SiteFromId == model.SiteId);

        if (link is null)
        {
            return NotFound();
        }

        model.LinkName =
            link.Name;


        if (!string.Equals(
                link.LinkType.Name,
                "SDH",
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                string.Empty,
                "Only SDH links can be connected to a MUX.");
        }


        MuxType? muxType = null;

        if (model.MuxTypeId.HasValue)
        {
            muxType =
                await _context.MuxTypes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(type =>
                        type.Id ==
                        model.MuxTypeId.Value);
        }

        if (muxType is null)
        {
            ModelState.AddModelError(
                nameof(model.MuxTypeId),
                "The selected MUX type is invalid.");
        }
        else if (muxType.StmCardCount <= 0 ||
                 muxType.StmPortsPerCard <= 0)
        {
            ModelState.AddModelError(
                nameof(model.MuxTypeId),
                "The selected MUX type does not contain valid STM ports.");
        }


        bool duplicateMuxName =
            await _context.Muxes
                .AnyAsync(mux =>
                    mux.SiteId == model.SiteId &&
                    mux.Name == model.MuxName);

        if (duplicateMuxName)
        {
            ModelState.AddModelError(
                nameof(model.MuxName),
                "A MUX with this name already exists at this site.");
        }


        var validStmIds =
            link.Stms
                .Select(stm => stm.Id)
                .ToHashSet();

        bool containsInvalidStm =
            model.StmAssignments
                .Any(assignment =>
                    !validStmIds.Contains(
                        assignment.StmId));

        if (containsInvalidStm)
        {
            ModelState.AddModelError(
                string.Empty,
                "One of the selected STMs does not belong to this link.");
        }


        bool containsDuplicateStm =
            model.StmAssignments
                .GroupBy(assignment =>
                    assignment.StmId)
                .Any(group =>
                    group.Count() > 1);

        if (containsDuplicateStm)
        {
            ModelState.AddModelError(
                string.Empty,
                "An STM cannot appear more than once.");
        }


        var selectedAssignments =
            model.StmAssignments
                .Where(assignment =>
                    !string.IsNullOrWhiteSpace(
                        assignment.PortKey))
                .ToList();

        if (selectedAssignments.Count == 0)
        {
            ModelState.AddModelError(
                string.Empty,
                "Assign at least one STM to a MUX port.");
        }


        bool duplicatePort =
            selectedAssignments
                .GroupBy(
                    assignment =>
                        assignment.PortKey!.Trim(),
                    StringComparer.OrdinalIgnoreCase)
                .Any(group =>
                    group.Count() > 1);

        if (duplicatePort)
        {
            ModelState.AddModelError(
                string.Empty,
                "A MUX port cannot be assigned to more than one STM.");
        }


        if (muxType is not null)
        {
            int firstStmSlot =
                muxType.PowerCardCount + 1;

            int lastStmSlot =
                muxType.PowerCardCount +
                muxType.StmCardCount;

            foreach (var assignment
                     in selectedAssignments)
            {
                if (!TryParsePortKey(
                        assignment.PortKey,
                        out int slotNumber,
                        out int portNumber) ||
                    slotNumber < firstStmSlot ||
                    slotNumber > lastStmSlot ||
                    portNumber < 1 ||
                    portNumber >
                        muxType.StmPortsPerCard)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "One of the selected STM ports is invalid.");
                }
            }
        }


        var selectedStmIds =
            selectedAssignments
                .Select(assignment =>
                    assignment.StmId)
                .ToList();

        bool stmAlreadyAssigned =
            await _context.MuxPorts
                .AnyAsync(port =>
                    port.StmId.HasValue &&
                    selectedStmIds.Contains(
                        port.StmId.Value));

        if (stmAlreadyAssigned)
        {
            ModelState.AddModelError(
                string.Empty,
                "One of the selected STMs is already assigned to a MUX port.");
        }


        var postedPortLookup =
            model.StmAssignments
                .GroupBy(assignment =>
                    assignment.StmId)
                .ToDictionary(
                    group => group.Key,
                    group => group
                        .First()
                        .PortKey);

        model.StmAssignments =
            link.Stms
                .OrderBy(stm =>
                    int.TryParse(
                        stm.Number,
                        out int number)
                            ? number
                            : int.MaxValue)
                .Select(stm =>
                    new SdhStmPortAssignmentViewModel
                    {
                        StmId = stm.Id,
                        StmNumber = stm.Number,

                        PortKey =
                            postedPortLookup
                                .GetValueOrDefault(
                                    stm.Id)
                    })
                .ToList();


        if (!ModelState.IsValid)
        {
            await PopulateInstallMuxTypeOptionsAsync(
                model);

            return View(model);
        }


        var mux = new Mux
        {
            Id = Guid.NewGuid(),
            Name = model.MuxName,
            SiteId = model.SiteId,
            MuxTypeId = muxType!.Id
        };


        int nextSlotNumber = 1;

        AddCards(
            mux,
            CardCategory.Power,
            muxType.PowerCardCount,
            muxType.PowerPortsPerCard,
            ref nextSlotNumber);

        AddCards(
            mux,
            CardCategory.STM,
            muxType.StmCardCount,
            muxType.StmPortsPerCard,
            ref nextSlotNumber);

        AddCards(
            mux,
            CardCategory.E1,
            muxType.E1CardCount,
            muxType.E1PortsPerCard,
            ref nextSlotNumber);


        foreach (var assignment
                 in selectedAssignments)
        {
            TryParsePortKey(
                assignment.PortKey,
                out int slotNumber,
                out int portNumber);

            var port =
                mux.Cards
                    .Where(card =>
                        card.Category ==
                            CardCategory.STM &&
                        card.SlotNumber ==
                            slotNumber)
                    .SelectMany(card =>
                        card.Ports)
                    .Single(item =>
                        item.PortNumber ==
                            portNumber);

            port.StmId =
                assignment.StmId;

            port.Status =
                MuxPortStatus.Connected;
        }


        _context.Muxes.Add(mux);

        await _context.SaveChangesAsync();


        TempData["SuccessMessage"] =
            $"MUX '{mux.Name}' was installed at site " +
            $"'{mux.SiteId}', and " +
            $"{selectedAssignments.Count} STM(s) were connected.";

        return RedirectToAction(
            nameof(Details),
            new { id = mux.Id });
    }

    private async Task PopulateInstallMuxTypeOptionsAsync(
    InstallSdhLinkMuxViewModel model)
    {
        model.MuxTypeOptions =
            await _context.MuxTypes
                .AsNoTracking()
                .Where(type =>
                    type.StmCardCount > 0 &&
                    type.StmPortsPerCard > 0)
                .OrderBy(type => type.Name)
                .Select(type =>
                    new SelectListItem
                    {
                        Value =
                            type.Id.ToString(),

                        Text =
                            type.Name +
                            " — " +
                            type.StmCardCount +
                            " STM cards × " +
                            type.StmPortsPerCard +
                            " ports"
                    })
                .ToListAsync();
    }
    private async Task PopulateInstallMuxTypeOptionsAsync(
    InstallSiteMuxViewModel model)
    {
        model.MuxTypeOptions =
            await _context.MuxTypes
                .AsNoTracking()

                .Where(type =>
                    type.StmCardCount > 0 &&
                    type.StmPortsPerCard > 0)

                .OrderBy(type =>
                    type.Name)

                .Select(type =>
                    new SelectListItem
                    {
                        Value =
                            type.Id.ToString(),

                        Text =
                            type.Name +
                            " — " +
                            type.StmCardCount +
                            " STM cards × " +
                            type.StmPortsPerCard +
                            " ports"
                    })

                .ToListAsync();
    }

    private static bool TryParsePortKey(
    string? portKey,
    out int slotNumber,
    out int portNumber)
    {
        slotNumber = 0;
        portNumber = 0;

        if (string.IsNullOrWhiteSpace(
                portKey))
        {
            return false;
        }

        string[] parts =
            portKey.Split(
                ':',
                StringSplitOptions.TrimEntries);

        return
            parts.Length == 2 &&
            int.TryParse(
                parts[0],
                out slotNumber) &&
            int.TryParse(
                parts[1],
                out portNumber);
    }
}