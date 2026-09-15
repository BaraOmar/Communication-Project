using CommunicationProject.Data;
using CommunicationProject.Models;
using CommunicationProject.Security;
using CommunicationProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using static CommunicationProject.Models.E1;

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
                .ThenInclude(card => card.CardType)

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
        if (port.MuxCard.CardType.Category != CardCategory.STM)
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

            ShelfNumber = port.MuxCard.ShelfNumber,

            HasShelves = mux.MuxType.HasShelves,

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
                .ThenInclude(card => card.CardType)

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
        if (port.MuxCard.CardType.Category !=
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



    // GET: MuxPorts/AssignE1/{id}
    // GET: MuxPorts/AssignE1/{id}
    [HttpGet]
    [Authorize(
        Roles =
            AppRoles.Admin + "," +
            AppRoles.Operator)]
    public async Task<IActionResult> AssignE1(Guid? id)
    {
        if (!id.HasValue ||
            id.Value == Guid.Empty)
        {
            return NotFound();
        }

        var port = await _context.MuxPorts
            .AsNoTracking()

            .Include(item => item.MuxCard)
                .ThenInclude(card => card.CardType)

            .Include(item => item.MuxCard)
                .ThenInclude(card => card.Mux)
                    .ThenInclude(mux => mux.Site)

            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (port == null)
        {
            return NotFound();
        }

        /*
         * Only E1-category cards can contain E1 channels.
         */
        if (port.MuxCard.CardType.Category != CardCategory.E1)
        {
            TempData["ErrorMessage"] =
                "E1 channels can only be assigned to E1 cards.";

            return RedirectToAction(
                "Details",
                "MuxCards",
                new { id = port.MuxCardId });
        }

        /*
         * Do not replace an already assigned E1
         * through this action.
         */
        if (port.E1Id.HasValue)
        {
            TempData["ErrorMessage"] =
                "This port already has an E1 assigned.";

            return RedirectToAction(
                "Details",
                "MuxCards",
                new { id = port.MuxCardId });
        }

        var mux = port.MuxCard.Mux;

        var model = new AssignE1ToMuxPortViewModel
        {
            MuxPortId = port.Id,

            MuxCardId = port.MuxCardId,

            PortNumber = port.PortNumber,

            MuxName = mux.Name,

            CardTypeName =
                port.MuxCard.CardType.Name,

            E1Options = await _context.E1s
                .AsNoTracking()

                .Where(e1 =>

                    /*
                     * Only Physical E1 channels can
                     * occupy physical MUX ports.
                     */
                    e1.ConnectionType ==
                        E1ConnectionType.Physical &&

                    /*
                     * The E1 belongs to an STM.
                     * The STM's physical location is
                     * Link.SiteFromId.
                     *
                     * Therefore the E1 must be located
                     * at the same site as the MUX.
                     */
                    e1.Stm.Link.SiteFromId ==
                        mux.SiteId &&

                    /*
                     * Do not show E1s already assigned
                     * to another MUX port.
                     */
                    !_context.MuxPorts.Any(
                        otherPort =>
                            otherPort.E1Id == e1.Id))

                .OrderBy(e1 =>
                    e1.Stm.Link.Name)

                .ThenBy(e1 =>
                    e1.Stm.Number)

                .ThenBy(e1 =>
                    e1.E1Number)

                .Select(e1 =>
                    new SelectListItem
                    {
                        Value = e1.Id.ToString(),

                        Text =
                            "STM " +
                            e1.Stm.Number +
                            " — E1 " +
                            e1.E1Number +
                            " — " +
                            e1.Stm.Link.Name
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
    public async Task<IActionResult> AssignE1(
     AssignE1ToMuxPortViewModel model)
    {
        var port = await _context.MuxPorts
            .Include(item => item.MuxCard)
                .ThenInclude(card => card.CardType)

            .Include(item => item.MuxCard)
                .ThenInclude(card => card.Mux)

            .FirstOrDefaultAsync(item =>
                item.Id == model.MuxPortId);

        if (port == null)
        {
            return NotFound();
        }

        /*
         * Only E1 cards may contain E1 channels.
         */
        if (port.MuxCard.CardType.Category !=
            CardCategory.E1)
        {
            ModelState.AddModelError(
                string.Empty,
                "E1 channels can only be assigned to E1 cards.");
        }

        /*
         * Do not overwrite an existing E1.
         */
        if (port.E1Id.HasValue)
        {
            ModelState.AddModelError(
                string.Empty,
                "This port already has an E1 assigned.");
        }

        var e1 = model.E1Id.HasValue
            ? await _context.E1s
                .Include(item => item.Stm)
                    .ThenInclude(stm => stm.Link)

                .FirstOrDefaultAsync(item =>
                    item.Id == model.E1Id.Value)
            : null;

        if (e1 == null)
        {
            ModelState.AddModelError(
                nameof(model.E1Id),
                "The selected E1 is invalid.");
        }
        else
        {
            /*
             * Only physical E1 channels may be
             * connected to physical MUX ports.
             */
            if (e1.ConnectionType !=
                E1ConnectionType.Physical)
            {
                ModelState.AddModelError(
                    nameof(model.E1Id),
                    "Only physical E1 channels can be assigned to a MUX port.");
            }

            /*
             * E1 location comes from its STM.
             * STM location = Link.SiteFromId.
             *
             * Therefore the E1 must physically
             * belong to the same site as the MUX.
             */
            if (e1.Stm.Link.SiteFromId !=
                port.MuxCard.Mux.SiteId)
            {
                ModelState.AddModelError(
                    nameof(model.E1Id),
                    "The selected E1 does not belong to this MUX site.");
            }

            /*
             * One E1 cannot occupy multiple
             * MUX ports.
             */
            bool alreadyAssigned =
                await _context.MuxPorts
                    .AnyAsync(otherPort =>
                        otherPort.E1Id == e1.Id &&
                        otherPort.Id != port.Id);

            if (alreadyAssigned)
            {
                ModelState.AddModelError(
                    nameof(model.E1Id),
                    "This E1 is already assigned to another MUX port.");
            }
        }

        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] =
                "The E1 could not be assigned to this port.";

            return RedirectToAction(
                "Details",
                "MuxCards",
                new { id = port.MuxCardId });
        }

        port.E1Id = e1!.Id;

        port.Status =
            MuxPortStatus.Connected;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"E1 {e1.E1Number} was assigned to Port {port.PortNumber}.";

        return RedirectToAction(
            "Details",
            "MuxCards",
            new { id = port.MuxCardId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(
    Roles =
        AppRoles.Admin + "," +
        AppRoles.Operator)]
    public async Task<IActionResult> ReleaseStm(Guid id)
    {
        var port = await _context.MuxPorts
            .FirstOrDefaultAsync(item =>
                item.Id == id);

        if (port == null)
        {
            return NotFound();
        }

        if (!port.StmId.HasValue)
        {
            TempData["ErrorMessage"] =
                "This port does not have an STM assigned.";

            return RedirectToAction(
                "Details",
                "MuxCards",
                new { id = port.MuxCardId });
        }

        port.StmId = null;
        port.Status = MuxPortStatus.Available;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            "The STM was released from the port successfully.";

        return RedirectToAction(
            "Details",
            "MuxCards",
            new { id = port.MuxCardId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(
    Roles =
        AppRoles.Admin + "," +
        AppRoles.Operator)]
    public async Task<IActionResult> ReleaseE1(Guid id)
    {
        if (id == Guid.Empty)
        {
            return NotFound();
        }

        var port = await _context.MuxPorts
            .Include(item => item.E1)
                .ThenInclude(e1 => e1!.Stm)
            .FirstOrDefaultAsync(item =>
                item.Id == id);

        if (port == null)
        {
            return NotFound();
        }

        if (!port.E1Id.HasValue)
        {
            TempData["ErrorMessage"] =
                "This port does not have an E1 assigned.";

            return RedirectToAction(
                "Details",
                "MuxCards",
                new { id = port.MuxCardId });
        }

        string e1Text =
            port.E1 == null
                ? "E1"
                : $"STM {port.E1.Stm.Number} — E1 {port.E1.E1Number}";

        /*
         * Release only the physical MUX mapping.
         *
         * Do NOT modify:
         * - E1 Status
         * - ConnectionGroupId
         * - PathId
         * - JoinE1Id
         *
         * The logical/end-to-end connection remains intact.
         */
        port.E1Id = null;

        port.Status =
            MuxPortStatus.Available;

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] =
            $"{e1Text} was released from Port {port.PortNumber}.";

        return RedirectToAction(
            "Details",
            "MuxCards",
            new { id = port.MuxCardId });
    }



}