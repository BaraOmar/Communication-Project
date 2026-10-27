using CommunicationProject.Data;
using CommunicationProject.Models;
using CommunicationProject.Security;
using CommunicationProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using static CommunicationProject.Models.E1;
using System.Data;
using CommunicationProject.Services.Muxes;

namespace CommunicationProject.Controllers;

[Authorize(
    Roles =
        AppRoles.Admin + "," +
        AppRoles.Operator + "," +
        AppRoles.Viewer)]
public class MuxPortsController : Controller
{
    private readonly CommunicationDbContext _context;

    public MuxPortsController(
        CommunicationDbContext context)
    {
        _context = context;
    }

    // GET: MuxPorts/AssignStm/{id}
    [HttpGet]
    [Authorize(
        Roles =
            AppRoles.Admin + "," +
            AppRoles.Operator)]
    public async Task<IActionResult> AssignStm(Guid? id)
    {
        if (!id.HasValue ||
            id.Value == Guid.Empty)
        {
            return NotFound();
        }

        var port = await _context.MuxPorts
            .AsNoTracking()


            .Include(item => item.MuxCard)
                .ThenInclude(card => card.Mux)
                    .ThenInclude(mux => mux.Site)

            .Include(item => item.MuxCard)
                .ThenInclude(card => card.Mux)
                    .ThenInclude(mux => mux.MuxType)

            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (port == null)
        {
            return NotFound();
        }

        /*
         * Only STM-category cards can contain STMs.
         */
        if (port.MuxCard.Category != CardCategory.STM)
        {
            TempData["ErrorMessage"] =
                "STMs can only be assigned to STM cards.";

            return RedirectToAction(
                "Details",
                "MuxCards",
                new { id = port.MuxCardId });
        }

        /*
         * Do not replace an already assigned STM
         * through this action.
         */
        if (port.StmId.HasValue)
        {
            TempData["ErrorMessage"] =
                "This port already has an STM assigned.";

            return RedirectToAction(
                "Details",
                "MuxCards",
                new { id = port.MuxCardId });
        }

        var mux = port.MuxCard.Mux;

        var model = new AssignStmToMuxPortViewModel
        {
            MuxPortId = port.Id,

            MuxCardId = port.MuxCardId,

            MuxName = mux.Name,

            SiteName = mux.Site.Name,

            PortNumber = port.PortNumber,

            SlotNumber = port.MuxCard.SlotNumber,

            StmOptions = await _context.Stms
                .AsNoTracking()

                /*
                 * An STM physically belongs to Link.SiteFrom.
                 *
                 * Therefore only STMs physically located
                 * at this MUX's site may be selected.
                 */
                .Where(stm =>
                    stm.Link.SiteFromId == mux.SiteId &&

                    /*
                     * Do not show STMs that are already
                     * connected to another MUX port.
                     */
                    !_context.MuxPorts.Any(
                        otherPort =>
                            otherPort.StmId == stm.Id))

                .OrderBy(stm => stm.Link.Name)
                .ThenBy(stm => stm.Number)

                .Select(stm =>
                    new SelectListItem
                    {
                        Value = stm.Id.ToString(),

                        Text =
                            "STM " +
                            stm.Number +
                            " — " +
                            stm.Link.Name
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
    public async Task<IActionResult> AssignStm(
    AssignStmToMuxPortViewModel model)
    {
        var port = await _context.MuxPorts


            .Include(item => item.MuxCard)
                .ThenInclude(card => card.Mux)
                    .ThenInclude(mux => mux.Site)

            .FirstOrDefaultAsync(item =>
                item.Id == model.MuxPortId);

        if (port == null)
        {
            return NotFound();
        }

        /*
         * Only STM cards may contain STMs.
         */
        if (port.MuxCard.Category !=
            CardCategory.STM)
        {
            ModelState.AddModelError(
                string.Empty,
                "STMs can only be assigned to STM cards.");
        }

        /*
         * Do not overwrite an existing STM.
         */
        if (port.StmId.HasValue)
        {
            ModelState.AddModelError(
                string.Empty,
                "This port already has an STM assigned.");
        }

        var stm = model.StmId.HasValue
            ? await _context.Stms
                .Include(item => item.Link)
                .FirstOrDefaultAsync(item =>
                    item.Id == model.StmId.Value)
            : null;

        if (stm == null)
        {
            ModelState.AddModelError(
                nameof(model.StmId),
                "The selected STM is invalid.");
        }
        else
        {
            /*
             * The STM must physically belong to
             * the same site as the MUX.
             *
             * STM location = Link.SiteFromId.
             */
            if (stm.Link.SiteFromId !=
                port.MuxCard.Mux.SiteId)
            {
                ModelState.AddModelError(
                    nameof(model.StmId),
                    "The selected STM does not belong to this MUX site.");
            }

            /*
             * One STM cannot occupy more than
             * one MUX port.
             */
            bool alreadyAssigned =
                await _context.MuxPorts
                    .AnyAsync(otherPort =>
                        otherPort.StmId == stm.Id &&
                        otherPort.Id != port.Id);

            if (alreadyAssigned)
            {
                ModelState.AddModelError(
                    nameof(model.StmId),
                    "This STM is already assigned to another MUX port.");
            }
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] =
                "The STM could not be assigned to this port.";

            return RedirectToAction(
                "Details",
                "MuxCards",
                new { id = port.MuxCardId });
        }

        port.StmId = stm!.Id;

        port.Status =
            MuxPortStatus.Connected;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"STM {stm.Number} was assigned to Port {port.PortNumber}.";

        return RedirectToAction(
            "Details",
            "MuxCards",
            new { id = port.MuxCardId });
    }



    private IQueryable<E1> EligibleLtuE1s(MuxPort port)
    {
        Guid muxId = port.MuxCard.MuxId;
        string siteId = port.MuxCard.Mux.SiteId;

        return _context.E1s
            .AsNoTracking()
            .Where(e1 =>
                e1.StmId.HasValue &&
                e1.Link.SiteFromId == siteId &&
                e1.LinkId == e1.Stm!.LinkId &&
                e1.ConnectionType == E1ConnectionType.Physical &&
                e1.JoinE1Id == null &&
                e1.CrossConnectionState != E1CrossConnectionState.CrossConnected &&
                (
                    e1.Status == E1OperationalStatus.Available ||
                    e1.Status == E1OperationalStatus.Connected
                ) &&
                _context.MuxPorts.Any(stmPort =>
                    stmPort.StmId == e1.StmId &&
                    stmPort.MuxCard.Category == CardCategory.STM &&
                    stmPort.MuxCard.MuxId == muxId) &&
                !_context.MuxPorts.Any(other => other.E1Id == e1.Id));
    }

    private static int[] ConfiguredPhysicalStms(Mux mux)
    {
        return new[]
            {
            mux.FirstPhysicalStmNumber,
            mux.SecondPhysicalStmNumber
        }
            .Where(number =>
                number.HasValue &&
                number.Value >= 29 &&
                number.Value <= 31)
            .Select(number => number!.Value)
            .Distinct()
            .OrderBy(number => number)
            .ToArray();
    }

    private async Task<AssignE1ToMuxPortViewModel> BuildE1AssignmentAsync(
        MuxPort port,
        Guid? selectedE1Id = null,
        int? physicalStmNumber = null)
    {
        var mux = port.MuxCard.Mux;

        var rows = await (
            from e1 in EligibleLtuE1s(port)
            join stmPort in _context.MuxPorts.AsNoTracking()
                on e1.StmId equals stmPort.StmId
            where stmPort.MuxCard.Category == CardCategory.STM &&
                  stmPort.MuxCard.MuxId == mux.Id
            orderby e1.Link.Name, stmPort.MuxCard.ShelfNumber,
                    stmPort.MuxCard.SlotNumber, stmPort.PortNumber,
                    e1.E1Number
            select new
            {
                e1.Id,
                e1.E1Number,
                StmNumber = e1.Stm!.Number,
                LinkName = e1.Link.Name,
                ShelfNumber = stmPort.MuxCard.ShelfNumber,
                CardNumber = stmPort.MuxCard.SlotNumber,
                stmPort.PortNumber
            })
            .ToListAsync();

        return new AssignE1ToMuxPortViewModel
        {
            MuxPortId = port.Id,
            MuxCardId = port.MuxCardId,
            PortNumber = port.PortNumber,
            MuxName = mux.Name,
            CardCategoryName = port.MuxCard.Category.ToString(),
            Position = MuxDisplay.LtuLabel(
                mux.Model,
                port.MuxCard.ShelfNumber,
                port.MuxCard.SlotNumber,
                port.PortNumber),
            IsTellab = mux.Model == MuxModel.Tellab,
            E1Id = selectedE1Id,
            PhysicalStmNumber = physicalStmNumber,

            E1Options = rows.Select(row => new SelectListItem
            {
                Value = row.Id.ToString(),
                Text = MuxDisplay.StmLabel(
                    mux.Model,
                    row.ShelfNumber,
                    row.CardNumber,
                    row.PortNumber,
                    row.StmNumber)
                    + " " + MuxDisplay.E1Label(mux.Model, row.E1Number)
                    + " — " + row.LinkName
            }).ToList(),

            PhysicalStmOptions = mux.Model == MuxModel.Tellab
                ? ConfiguredPhysicalStms(mux)
                    .Select(number => new SelectListItem
                    {
                        Value = number.ToString(),
                        Text = $"VC4 {number - 1}"
                    })
                    .ToList()
                : []
        };
    }

    [HttpGet]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Operator)]
    public async Task<IActionResult> AssignE1(Guid? id)
    {
        var port = await _context.MuxPorts
            .AsNoTracking()
            .Include(item => item.MuxCard)
                .ThenInclude(card => card.Mux)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (port == null)
            return NotFound();

        if (port.MuxCard.Category != CardCategory.LTU ||
            port.E1Id.HasValue ||
            port.StmId.HasValue ||
            port.Status != MuxPortStatus.Available)
        {
            TempData["ErrorMessage"] = "Select an available LTU port.";

            return RedirectToAction(
                "Details", "MuxCards", new { id = port.MuxCardId });
        }

        return View(await BuildE1AssignmentAsync(port));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Operator)]
    public async Task<IActionResult> AssignE1(
        AssignE1ToMuxPortViewModel model)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var port = await _context.MuxPorts
            .Include(item => item.MuxCard)
                .ThenInclude(card => card.Mux)
            .FirstOrDefaultAsync(item => item.Id == model.MuxPortId);

        if (port == null)
            return NotFound();

        if (port.MuxCard.Category != CardCategory.LTU ||
            port.E1Id.HasValue ||
            port.StmId.HasValue ||
            port.Status != MuxPortStatus.Available)
        {
            TempData["ErrorMessage"] = "This LTU port is unavailable.";

            return RedirectToAction(
                "Details", "MuxCards", new { id = port.MuxCardId });
        }

        var e1 = model.E1Id.HasValue
            ? await EligibleLtuE1s(port)
                .FirstOrDefaultAsync(item => item.Id == model.E1Id.Value)
            : null;

        if (e1 == null)
        {
            ModelState.AddModelError(
                nameof(model.E1Id),
                "Select an unassigned physical E1 on this MUX. " +
                "Joined E1s must be managed through Cross Connection.");
        }

        var mux = port.MuxCard.Mux;

        if (mux.Model == MuxModel.Tellab)
        {
            int[] configured = ConfiguredPhysicalStms(mux);

            if (configured.Length != 2 ||
                !model.PhysicalStmNumber.HasValue ||
                !configured.Contains(model.PhysicalStmNumber.Value))
            {
                ModelState.AddModelError(
                    nameof(model.PhysicalStmNumber),
                    "Select one of this Tellab's configured physical VC4s.");
            }
        }
        else
        {
            model.PhysicalStmNumber = null;
        }

        if (!ModelState.IsValid)
        {
            await transaction.RollbackAsync();

            return View(await BuildE1AssignmentAsync(
                port, model.E1Id, model.PhysicalStmNumber));
        }

        port.E1Id = e1!.Id;
        port.PhysicalStmNumber = model.PhysicalStmNumber;
        port.Status = MuxPortStatus.Connected;

        try
        {
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            _context.ChangeTracker.Clear();

            ModelState.AddModelError(
                string.Empty,
                "The assignment could not be saved. " +
                "The E1 or LTU port may have been assigned by another user.");

            return View(await BuildE1AssignmentAsync(
                port, model.E1Id, model.PhysicalStmNumber));
        }

        TempData["SuccessMessage"] = "Physical E1 assigned.";

        return RedirectToAction(
            "Details", "MuxCards", new { id = port.MuxCardId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Operator)]
    public async Task<IActionResult> ReleaseStm(Guid id)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var port = await _context.MuxPorts
            .FirstOrDefaultAsync(item => item.Id == id);

        if (port == null)
            return NotFound();

        if (!port.StmId.HasValue)
        {
            TempData["ErrorMessage"] = "This port has no STM assigned.";

            return RedirectToAction(
                "Details", "MuxCards", new { id = port.MuxCardId });
        }

        bool inUse = await _context.E1s.AnyAsync(e1 =>
            e1.StmId == port.StmId &&
            (
                e1.ConnectionGroupId != null ||
                e1.JoinE1Id != null ||
                e1.Status != E1OperationalStatus.Available ||
                e1.CrossConnectionState != E1CrossConnectionState.Available ||
                (
                    e1.ConnectedE1 != null &&
                    (
                        e1.ConnectedE1.ConnectionGroupId != null ||
                        e1.ConnectedE1.JoinE1Id != null
                    )
                )
            ));

        bool hasLtuAssignments = await _context.MuxPorts.AnyAsync(item =>
            item.E1 != null &&
            item.E1.StmId == port.StmId);

        if (inUse || hasLtuAssignments || port.E1Id.HasValue)
        {
            TempData["ErrorMessage"] =
                "Release this STM's customer connections and LTU assignments first.";

            return RedirectToAction(
                "Details", "MuxCards", new { id = port.MuxCardId });
        }

        port.StmId = null;
        port.PhysicalStmNumber = null;
        port.Status = MuxPortStatus.Available;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = "STM assignment released.";

        return RedirectToAction(
            "Details", "MuxCards", new { id = port.MuxCardId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = AppRoles.Admin + "," + AppRoles.Operator)]
    public async Task<IActionResult> ReleaseE1(Guid id)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        var port = await _context.MuxPorts
            .Include(item => item.E1)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (port == null)
            return NotFound();

        if (port.E1 == null)
        {
            TempData["ErrorMessage"] = "This port has no E1 assigned.";

            return RedirectToAction(
                "Details", "MuxCards", new { id = port.MuxCardId });
        }

        if (port.E1.ConnectionGroupId.HasValue ||
            port.E1.JoinE1Id.HasValue)
        {
            TempData["ErrorMessage"] =
                "Release this E1 through Search Path while it belongs to a customer connection.";

            return RedirectToAction(
                "Details", "MuxCards", new { id = port.MuxCardId });
        }

        port.E1Id = null;
        port.PhysicalStmNumber = null;
        port.Status = MuxPortStatus.Available;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        TempData["SuccessMessage"] = "E1 assignment released.";

        return RedirectToAction(
            "Details", "MuxCards", new { id = port.MuxCardId });
    }



}