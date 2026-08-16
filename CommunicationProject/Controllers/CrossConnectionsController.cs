using CommunicationProject.Data;
using CommunicationProject.Models;
using CommunicationProject.Security;
using CommunicationProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CommunicationProject.Controllers;


[Authorize(
    Roles = AppRoles.Admin + "," + AppRoles.Operator)]
public class CrossConnectionsController : Controller
{
    private readonly CommunicationDbContext _context;
    private readonly ILogger<CrossConnectionsController> _logger;

    public CrossConnectionsController(
        CommunicationDbContext context,
        ILogger<CrossConnectionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: CrossConnections/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CreateCrossConnectionViewModel();

        await LoadSiteOptionsAsync(model);

        return View(model);
    }

    /*
     * Returns only the sites directly connected to the selected
     * cross-connection site.
     *
     * Example:
     * selected site = B
     *
     * Returns all SiteTo values from:
     * B -> A
     * B -> C
     * B -> D
     */
    [HttpGet]
    public async Task<IActionResult> GetConnectedSites(string? siteId)
    {
        siteId = siteId?.Trim();

        if (string.IsNullOrWhiteSpace(siteId))
        {
            return Json(Array.Empty<object>());
        }

        bool siteExists = await _context.Sites
            .AsNoTracking()
            .AnyAsync(site => site.Id == siteId);

        if (!siteExists)
        {
            return Json(Array.Empty<object>());
        }

        var connectedSites = await _context.CommunicationLinks
            .AsNoTracking()
            .Where(link =>
                link.SiteFromId == siteId &&
                link.SiteToId != siteId)
            .Select(link => new
            {
                value = link.SiteToId,
                text = link.SiteTo.Id + " - " + link.SiteTo.Name
            })
            .Distinct()
            .OrderBy(option => option.text)
            .ToListAsync();

        return Json(connectedSites);
    }

    /*
     * Returns STMs physically located in the selected central site
     * and belonging to the directional link toward the connected site.
     *
     * Example:
     * central site = B
     * connected site = A
     *
     * Uses the directional record:
     * B -> A
     */
    [HttpGet]
    public async Task<IActionResult> GetStms(
        string? siteId,
        string? connectedSiteId)
    {
        siteId = siteId?.Trim();
        connectedSiteId = connectedSiteId?.Trim();

        if (string.IsNullOrWhiteSpace(siteId) ||
            string.IsNullOrWhiteSpace(connectedSiteId) ||
            string.Equals(
                siteId,
                connectedSiteId,
                StringComparison.OrdinalIgnoreCase))
        {
            return Json(Array.Empty<object>());
        }

        var stms = await _context.Stms
            .AsNoTracking()
            .Where(stm =>
                stm.Link.SiteFromId == siteId &&
                stm.Link.SiteToId == connectedSiteId)
            .OrderBy(stm => stm.Number)
            .Select(stm => new
            {
                value = stm.Id,
                text = "STM " + stm.Number
            })
            .ToListAsync();

        return Json(stms);
    }

    /*
     * Returns E1 channels that are not already involved
     * in another cross connection.
     *
     * An E1 is unavailable when:
     *
     * 1. It already points to another E1 through JoinE1Id.
     * 2. Another E1 already points to it through JoinE1Id.
     */
    [HttpGet]
    public async Task<IActionResult> GetAvailableE1s(
        Guid? stmId,
        bool isIncoming = false)
    {
        if (!stmId.HasValue || stmId.Value == Guid.Empty)
        {
            return Json(Array.Empty<object>());
        }

        var baseQuery = _context.E1s
            .AsNoTracking()
            .Where(e1 =>
                e1.StmId == stmId.Value &&
                e1.ConnectedE1Id != null);

        IQueryable<E1> query;

        if (isIncoming)
        {
            /*
             * Incoming:
             *
             * Available             -> creates a new path.
             * ExtendExistingPath    -> extends an existing path.
             * CrossConnected        -> never displayed.
             */
            query = baseQuery.Where(e1 =>
                (
                    e1.CrossConnectionState ==
                        E1CrossConnectionState.Available &&

                    e1.PathId == null &&
                    e1.PathOrder == null &&
                    e1.JoinE1Id == null
                )
                ||
                (
                    e1.CrossConnectionState ==
                        E1CrossConnectionState.ExtendExistingPath &&

                    e1.PathId != null &&
                    e1.PathOrder != null &&
                    e1.JoinE1Id == null &&

                    !_context.E1s.Any(pathE1 =>
                        pathE1.PathId == e1.PathId &&
                        pathE1.PathOrder > e1.PathOrder)
                )
            );
        }
        else
        {
            /*
             * Outgoing:
             * only a completely available E1 pair.
             */
            query = baseQuery.Where(e1 =>
                e1.CrossConnectionState ==
                    E1CrossConnectionState.Available &&

                e1.PathId == null &&
                e1.PathOrder == null &&
                e1.JoinE1Id == null &&

                e1.ConnectedE1!.CrossConnectionState ==
                    E1CrossConnectionState.Available &&

                e1.ConnectedE1.PathId == null &&
                e1.ConnectedE1.PathOrder == null &&
                e1.ConnectedE1.JoinE1Id == null
            );
        }

        var options = await query
            .OrderBy(e1 => e1.E1Number)
            .Select(e1 => new
            {
                value = e1.Id,

                text =
                    e1.CrossConnectionState ==
                        E1CrossConnectionState.ExtendExistingPath

                        ? $"E1 {e1.E1Number} — Extend existing path"
                        : $"E1 {e1.E1Number} — Available",

                state = e1.CrossConnectionState.ToString(),

                description = e1.Description ?? string.Empty,

                pathId = e1.PathId
            })
            .ToListAsync();

        return Json(options);
    }
    // POST: CrossConnections/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateCrossConnectionViewModel model)
    {
        NormalizeModel(model);

        ModelState.Clear();
        TryValidateModel(model);

        ValidateBasicSelections(model);

        if (!ModelState.IsValid)
        {
            await LoadSiteOptionsAsync(model);
            return View(model);
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var incomingE1 = await _context.E1s
                .Include(e1 => e1.Stm)
                    .ThenInclude(stm => stm.Link)
                .Include(e1 => e1.ConnectedE1)
                .FirstOrDefaultAsync(e1 =>
                    e1.Id == model.IncomingE1Id!.Value);

            var outgoingE1 = await _context.E1s
                .Include(e1 => e1.Stm)
                    .ThenInclude(stm => stm.Link)
                .Include(e1 => e1.ConnectedE1)
                .FirstOrDefaultAsync(e1 =>
                    e1.Id == model.OutgoingE1Id!.Value);

            ValidateIncomingE1(model, incomingE1);
            ValidateOutgoingE1(model, outgoingE1);
            if (incomingE1 != null &&
                incomingE1.CrossConnectionState !=
                    E1CrossConnectionState.Available &&
                incomingE1.CrossConnectionState !=
                    E1CrossConnectionState.ExtendExistingPath)
            {
                ModelState.AddModelError(
                    nameof(model.IncomingE1Id),
                    "The incoming E1 must be available or an existing path endpoint.");
            }

            if (outgoingE1 != null &&
                outgoingE1.CrossConnectionState !=
                    E1CrossConnectionState.Available)
            {
                ModelState.AddModelError(
                    nameof(model.OutgoingE1Id),
                    "The outgoing E1 must be available.");
            }

            if (outgoingE1?.ConnectedE1 != null &&
                outgoingE1.ConnectedE1.CrossConnectionState !=
                    E1CrossConnectionState.Available)
            {
                ModelState.AddModelError(
                    nameof(model.OutgoingE1Id),
                    "The connected endpoint of the outgoing E1 is not available.");
            }

            if (incomingE1?.ConnectedE1 == null)
            {
                ModelState.AddModelError(
                    nameof(model.IncomingE1Id),
                    "The incoming E1 does not have a connected endpoint at the previous site.");
            }

            if (outgoingE1?.ConnectedE1 == null)
            {
                ModelState.AddModelError(
                    nameof(model.OutgoingE1Id),
                    "The outgoing E1 does not have a connected endpoint at the next site.");
            }

            if (incomingE1 != null &&
                outgoingE1 != null &&
                incomingE1.ConnectedE1 != null &&
                outgoingE1.ConnectedE1 != null)
            {
                await ValidateCrossConnectionAvailabilityAsync(
                    incomingE1,
                    outgoingE1);

                await ValidatePathAvailabilityAsync(
                    incomingE1,
                    outgoingE1);
            }
            bool isExtendingExistingPath =
    incomingE1 != null &&
    incomingE1.CrossConnectionState ==
        E1CrossConnectionState.ExtendExistingPath &&
    incomingE1.PathId.HasValue;

            string pathDescription =
                model.Description?.Trim() ?? string.Empty;

            if (incomingE1 != null)
            {
                if (isExtendingExistingPath)
                {
                    /*
                     * Never trust the posted description when extending.
                     * Read the original description from the database.
                     */
                    pathDescription = await _context.E1s
                        .AsNoTracking()
                        .Where(e1 =>
                            e1.PathId == incomingE1.PathId &&
                            e1.Description != null &&
                            e1.Description != "")
                        .OrderBy(e1 => e1.PathOrder)
                        .Select(e1 => e1.Description!)
                        .FirstOrDefaultAsync()
                        ?? string.Empty;

                    if (string.IsNullOrWhiteSpace(pathDescription))
                    {
                        ModelState.AddModelError(
                            nameof(model.Description),
                            "The existing path does not have a description.");
                    }
                }
                else if (string.IsNullOrWhiteSpace(pathDescription))
                {
                    ModelState.AddModelError(
                        nameof(model.Description),
                        "Enter a description for the new path.");
                }
            }

            if (!ModelState.IsValid)
            {
                await transaction.RollbackAsync();
                await LoadSiteOptionsAsync(model);

                return View(model);
            }


            var incomingConnectedE1 =
                incomingE1!.ConnectedE1!;

            var outgoingConnectedE1 =
                outgoingE1!.ConnectedE1!;

            incomingE1.JoinE1Id = outgoingE1.Id;
            outgoingE1.JoinE1Id = incomingE1.Id;

            incomingE1.CrossConnectionState =
                E1CrossConnectionState.CrossConnected;

            outgoingE1.CrossConnectionState =
                E1CrossConnectionState.CrossConnected;

            Guid pathId;

            if (isExtendingExistingPath)
            {
                /*
                 * Only an existing path has a PathId.
                 */
                pathId = incomingE1.PathId!.Value;

                int maximumOrder = await _context.E1s
                    .Where(e1 => e1.PathId == pathId)
                    .MaxAsync(e1 => e1.PathOrder ?? 0);

                outgoingE1.PathId = pathId;
                outgoingE1.PathOrder = maximumOrder + 1;

                outgoingConnectedE1.PathId = pathId;
                outgoingConnectedE1.PathOrder =
                    maximumOrder + 2;

                outgoingConnectedE1.CrossConnectionState =
                    E1CrossConnectionState.ExtendExistingPath;

                incomingE1.Description = pathDescription;
                outgoingE1.Description = pathDescription;
                outgoingConnectedE1.Description = pathDescription;
            }
            else
            {
                /*
                 * Available Incoming E1:
                 * create a new PathId.
                 */
                pathId = Guid.NewGuid();

                incomingConnectedE1.PathId = pathId;
                incomingConnectedE1.PathOrder = 1;
                incomingConnectedE1.CrossConnectionState =
                    E1CrossConnectionState.ExtendExistingPath;

                incomingE1.PathId = pathId;
                incomingE1.PathOrder = 2;

                outgoingE1.PathId = pathId;
                outgoingE1.PathOrder = 3;

                outgoingConnectedE1.PathId = pathId;
                outgoingConnectedE1.PathOrder = 4;
                outgoingConnectedE1.CrossConnectionState =
                    E1CrossConnectionState.ExtendExistingPath;

                incomingConnectedE1.Description = pathDescription;
                incomingE1.Description = pathDescription;
                outgoingE1.Description = pathDescription;
                outgoingConnectedE1.Description = pathDescription;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                isExtendingExistingPath
                    ? $"Path extended successfully. " +
                      $"Path ID: {pathId}. " +
                      $"Description: {pathDescription}."
                    : $"New path created successfully. " +
                      $"Path ID: {pathId}. " +
                      $"Description: {pathDescription}.";

            return RedirectToAction(nameof(Create));
        }
        catch (DbUpdateException exception)
        {
            await transaction.RollbackAsync();

            _logger.LogError(
                exception,
                "Database error while creating cross connection " +
                "from E1 {IncomingE1Id} to E1 {OutgoingE1Id}.",
                model.IncomingE1Id,
                model.OutgoingE1Id);

            ModelState.AddModelError(
                string.Empty,
                "The cross connection could not be saved. " +
                "One of the selected E1 channels may already be in use.");
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync();

            _logger.LogError(
                exception,
                "Unexpected error while creating cross connection.");

            ModelState.AddModelError(
                string.Empty,
                "An unexpected error occurred. " +
                "The cross connection was not created.");
        }

        await LoadSiteOptionsAsync(model);

        return View(model);
    }
    private static void NormalizeModel(
        CreateCrossConnectionViewModel model)
    {
        model.SiteId =
            model.SiteId?.Trim() ?? string.Empty;

        model.PreviousSiteId =
            model.PreviousSiteId?.Trim() ?? string.Empty;

        model.NextSiteId =
            model.NextSiteId?.Trim() ?? string.Empty;
    }

    private void ValidateBasicSelections(
        CreateCrossConnectionViewModel model)
    {
        if (string.Equals(
                model.SiteId,
                model.PreviousSiteId,
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(model.PreviousSiteId),
                "The previous site cannot be the cross-connection site.");
        }

        if (string.Equals(
                model.SiteId,
                model.NextSiteId,
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(model.NextSiteId),
                "The next site cannot be the cross-connection site.");
        }

        if (!string.IsNullOrWhiteSpace(model.PreviousSiteId) &&
            string.Equals(
                model.PreviousSiteId,
                model.NextSiteId,
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(model.NextSiteId),
                "The previous site and next site must be different.");
        }

        if (model.IncomingStmId.HasValue &&
            model.OutgoingStmId.HasValue &&
            model.IncomingStmId.Value ==
            model.OutgoingStmId.Value)
        {
            ModelState.AddModelError(
                nameof(model.OutgoingStmId),
                "Incoming STM and outgoing STM must be different.");
        }

        if (model.IncomingE1Id.HasValue &&
            model.OutgoingE1Id.HasValue &&
            model.IncomingE1Id.Value ==
            model.OutgoingE1Id.Value)
        {
            ModelState.AddModelError(
                nameof(model.OutgoingE1Id),
                "Incoming E1 and outgoing E1 must be different.");
        }
    }

    private void ValidateIncomingE1(
        CreateCrossConnectionViewModel model,
        E1? incomingE1)
    {
        if (incomingE1 == null)
        {
            ModelState.AddModelError(
                nameof(model.IncomingE1Id),
                "The selected incoming E1 does not exist.");

            return;
        }

        if (incomingE1.StmId != model.IncomingStmId)
        {
            ModelState.AddModelError(
                nameof(model.IncomingE1Id),
                "The selected incoming E1 does not belong " +
                "to the selected incoming STM.");
        }

        /*
         * Incoming E1 must be located at the central site
         * on the link toward the previous site.
         *
         * B -> A
         */
        if (!string.Equals(
                incomingE1.Stm.Link.SiteFromId,
                model.SiteId,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                incomingE1.Stm.Link.SiteToId,
                model.PreviousSiteId,
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(model.IncomingE1Id),
                "The incoming E1 does not belong to the connection " +
                "between the selected site and previous site.");
        }
    }

    private void ValidateOutgoingE1(
        CreateCrossConnectionViewModel model,
        E1? outgoingE1)
    {
        if (outgoingE1 == null)
        {
            ModelState.AddModelError(
                nameof(model.OutgoingE1Id),
                "The selected outgoing E1 does not exist.");

            return;
        }

        if (outgoingE1.StmId != model.OutgoingStmId)
        {
            ModelState.AddModelError(
                nameof(model.OutgoingE1Id),
                "The selected outgoing E1 does not belong " +
                "to the selected outgoing STM.");
        }

        /*
         * Outgoing E1 must be located at the central site
         * on the link toward the next site.
         *
         * B -> C
         */
        if (!string.Equals(
                outgoingE1.Stm.Link.SiteFromId,
                model.SiteId,
                StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(
                outgoingE1.Stm.Link.SiteToId,
                model.NextSiteId,
                StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(
                nameof(model.OutgoingE1Id),
                "The outgoing E1 does not belong to the connection " +
                "between the selected site and next site.");
        }
    }

    private async Task ValidateCrossConnectionAvailabilityAsync(
        E1 incomingE1,
        E1 outgoingE1)
    {
        if (incomingE1.Id == outgoingE1.Id)
        {
            ModelState.AddModelError(
                string.Empty,
                "An E1 channel cannot be joined to itself.");

            return;
        }

        if (incomingE1.JoinE1Id.HasValue)
        {
            ModelState.AddModelError(
                nameof(CreateCrossConnectionViewModel.IncomingE1Id),
                $"Incoming E1 '{incomingE1.E1Number}' " +
                "already has a cross connection.");
        }

        if (outgoingE1.JoinE1Id.HasValue)
        {
            ModelState.AddModelError(
                nameof(CreateCrossConnectionViewModel.OutgoingE1Id),
                $"Outgoing E1 '{outgoingE1.E1Number}' " +
                "already has a cross connection.");
        }

        bool incomingIsAlreadyTarget =
            await _context.E1s.AnyAsync(e1 =>
                e1.JoinE1Id == incomingE1.Id);

        if (incomingIsAlreadyTarget)
        {
            ModelState.AddModelError(
                nameof(CreateCrossConnectionViewModel.IncomingE1Id),
                $"Incoming E1 '{incomingE1.E1Number}' " +
                "is already used by another cross connection.");
        }

        bool outgoingIsAlreadyTarget =
            await _context.E1s.AnyAsync(e1 =>
                e1.JoinE1Id == outgoingE1.Id);

        if (outgoingIsAlreadyTarget)
        {
            ModelState.AddModelError(
                nameof(CreateCrossConnectionViewModel.OutgoingE1Id),
                $"Outgoing E1 '{outgoingE1.E1Number}' " +
                "is already used by another cross connection.");
        }
    }

    private async Task LoadSiteOptionsAsync(
        CreateCrossConnectionViewModel model)
    {
        var sites = await _context.Sites
            .AsNoTracking()
            .OrderBy(site => site.Name)
            .Select(site => new SelectListItem
            {
                Value = site.Id,
                Text = site.Id + " - " + site.Name
            })
            .ToListAsync();

        model.SiteOptions = sites;
    }
    private async Task ValidatePathAvailabilityAsync(
     E1 incomingE1,
     E1 outgoingE1)
    {
        var incomingConnectedE1 =
            incomingE1.ConnectedE1;

        var outgoingConnectedE1 =
            outgoingE1.ConnectedE1;

        /*
         * Every selected E1 must already have its physical
         * connection across the communication link.
         */
        if (incomingConnectedE1 == null)
        {
            ModelState.AddModelError(
                nameof(CreateCrossConnectionViewModel.IncomingE1Id),
                "The incoming E1 does not have a connected E1.");

            return;
        }

        if (outgoingConnectedE1 == null)
        {
            ModelState.AddModelError(
                nameof(CreateCrossConnectionViewModel.OutgoingE1Id),
                "The outgoing E1 does not have a connected E1.");

            return;
        }

        /*
         * ConnectedE1 must be reciprocal.
         */
        if (incomingConnectedE1.ConnectedE1Id != incomingE1.Id)
        {
            ModelState.AddModelError(
                nameof(CreateCrossConnectionViewModel.IncomingE1Id),
                "The incoming E1 physical connection is not reciprocal.");

            return;
        }

        if (outgoingConnectedE1.ConnectedE1Id != outgoingE1.Id)
        {
            ModelState.AddModelError(
                nameof(CreateCrossConnectionViewModel.OutgoingE1Id),
                "The outgoing E1 physical connection is not reciprocal.");

            return;
        }

        /*
         * The operation must involve four different E1 records:
         *
         * 1. Previous site's remote E1.
         * 2. Incoming E1 at the central site.
         * 3. Outgoing E1 at the central site.
         * 4. Next site's remote E1.
         */
        var allIds = new[]
        {
        incomingE1.Id,
        incomingConnectedE1.Id,
        outgoingE1.Id,
        outgoingConnectedE1.Id
    };

        if (allIds.Distinct().Count() != 4)
        {
            ModelState.AddModelError(
                string.Empty,
                "The selected E1 records do not form a valid connection.");

            return;
        }

        /*
         * The outgoing physical E1 pair must always be available.
         *
         * This applies when creating a new path and when
         * extending an existing path.
         */
        bool outgoingPairHasStoredUsage =
            outgoingE1.CrossConnectionState !=
                E1CrossConnectionState.Available ||

            outgoingConnectedE1.CrossConnectionState !=
                E1CrossConnectionState.Available ||

            outgoingE1.PathId.HasValue ||
            outgoingE1.PathOrder.HasValue ||
            outgoingE1.JoinE1Id.HasValue ||

            outgoingConnectedE1.PathId.HasValue ||
            outgoingConnectedE1.PathOrder.HasValue ||
            outgoingConnectedE1.JoinE1Id.HasValue;

        bool outgoingPairIsJoinTarget =
            await _context.E1s.AnyAsync(e1 =>
                e1.JoinE1Id == outgoingE1.Id ||
                e1.JoinE1Id == outgoingConnectedE1.Id);

        if (outgoingPairHasStoredUsage ||
            outgoingPairIsJoinTarget)
        {
            ModelState.AddModelError(
                nameof(CreateCrossConnectionViewModel.OutgoingE1Id),
                "The outgoing E1 or its connected E1 is already in use.");
        }

        /*
         * Case 1:
         * Incoming E1 is Available, so create a new path.
         */
        if (incomingE1.CrossConnectionState ==
            E1CrossConnectionState.Available)
        {
            bool incomingPairHasStoredUsage =
                incomingConnectedE1.CrossConnectionState !=
                    E1CrossConnectionState.Available ||

                incomingE1.PathId.HasValue ||
                incomingE1.PathOrder.HasValue ||
                incomingE1.JoinE1Id.HasValue ||

                incomingConnectedE1.PathId.HasValue ||
                incomingConnectedE1.PathOrder.HasValue ||
                incomingConnectedE1.JoinE1Id.HasValue;

            bool incomingPairIsJoinTarget =
                await _context.E1s.AnyAsync(e1 =>
                    e1.JoinE1Id == incomingE1.Id ||
                    e1.JoinE1Id == incomingConnectedE1.Id);

            if (incomingPairHasStoredUsage ||
                incomingPairIsJoinTarget)
            {
                ModelState.AddModelError(
                    nameof(CreateCrossConnectionViewModel.IncomingE1Id),
                    "The incoming E1 or its connected E1 is already in use.");
            }

            /*
             * No more existing-path validation is needed.
             */
            return;
        }

        /*
         * Case 2:
         * Incoming E1 is an endpoint of an existing path.
         */
        if (incomingE1.CrossConnectionState ==
            E1CrossConnectionState.ExtendExistingPath)
        {
            if (!incomingE1.PathId.HasValue)
            {
                ModelState.AddModelError(
                    nameof(CreateCrossConnectionViewModel.IncomingE1Id),
                    "The selected path endpoint does not have a Path ID.");

                return;
            }

            if (!incomingE1.PathOrder.HasValue)
            {
                ModelState.AddModelError(
                    nameof(CreateCrossConnectionViewModel.IncomingE1Id),
                    "The selected path endpoint does not have a path order.");

                return;
            }

            /*
             * An endpoint must not already have a cross connection.
             */
            if (incomingE1.JoinE1Id.HasValue)
            {
                ModelState.AddModelError(
                    nameof(CreateCrossConnectionViewModel.IncomingE1Id),
                    "The selected path endpoint already has a cross connection.");
            }

            bool incomingIsJoinTarget =
                await _context.E1s.AnyAsync(e1 =>
                    e1.JoinE1Id == incomingE1.Id);

            if (incomingIsJoinTarget)
            {
                ModelState.AddModelError(
                    nameof(CreateCrossConnectionViewModel.IncomingE1Id),
                    "The selected path endpoint is already used by another cross connection.");
            }

            /*
             * Only the E1 with the highest PathOrder can extend
             * the existing path.
             */
            int maximumOrder = await _context.E1s
                .Where(e1 =>
                    e1.PathId == incomingE1.PathId)
                .MaxAsync(e1 =>
                    e1.PathOrder ?? 0);

            if (incomingE1.PathOrder.Value != maximumOrder)
            {
                ModelState.AddModelError(
                    nameof(CreateCrossConnectionViewModel.IncomingE1Id),
                    "The selected incoming E1 is not the final endpoint of the path.");
            }

            /*
             * Its physically connected E1 must be the preceding
             * record in the same path.
             *
             * Example:
             * order 3 ⇄ ConnectedE1 ⇄ order 4 endpoint
             */
            if (incomingConnectedE1.PathId != incomingE1.PathId ||
                !incomingConnectedE1.PathOrder.HasValue ||
                incomingConnectedE1.PathOrder.Value !=
                    incomingE1.PathOrder.Value - 1)
            {
                ModelState.AddModelError(
                    nameof(CreateCrossConnectionViewModel.IncomingE1Id),
                    "The selected E1 is not a valid endpoint of this path.");
            }

            return;
        }

        /*
         * CrossConnected and any unexpected state cannot
         * be selected as Incoming.
         */
        ModelState.AddModelError(
            nameof(CreateCrossConnectionViewModel.IncomingE1Id),
            "The incoming E1 must be available or an endpoint of an existing path.");
    }
}