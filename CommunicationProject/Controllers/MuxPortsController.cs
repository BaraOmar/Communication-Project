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
                    .ThenInclude(mux => mux.CommunicationLink)

            .FirstOrDefaultAsync(item =>
                item.Id == id.Value);

        if (port == null)
        {
            return NotFound();
        }

        /*
         * Only E1-category cards can contain E1 channels.
         */
        if (port.MuxCard.CardType.Category !=
            CardCategory.E1)
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
        var link = mux.CommunicationLink;

        /*
         * Determine the directional CommunicationLink
         * located at the MUX site.
         *
         * Example:
         *
         * Primary:  A -> B
         * MUX at A: use A -> B
         * MUX at B: use B -> A (ConnectedLink)
         */
        Guid localLinkId;

        if (link.SiteFromId == mux.SiteId)
        {
            localLinkId = link.Id;
        }
        else if (link.SiteToId == mux.SiteId &&
                 link.ConnectedLinkId.HasValue)
        {
            localLinkId =
                link.ConnectedLinkId.Value;
        }
        else
        {
            TempData["ErrorMessage"] =
                "The MUX link configuration is invalid.";

            return RedirectToAction(
                "Details",
                "MuxCards",
                new { id = port.MuxCardId });
        }


        var model =
            new AssignE1ToMuxPortViewModel
            {
                MuxPortId = port.Id,

                MuxCardId =
    port.MuxCardId,

                PortNumber =
                    port.PortNumber,

                MuxName =
                    mux.Name,

                CardTypeName =
                    port.MuxCard.CardType.Name,

                E1Options =
                    await _context.E1s
                        .AsNoTracking()

                        .Where(e1 =>
                            e1.ConnectionType ==
                                E1ConnectionType.Physical &&

                            e1.Stm.LinkId ==
                                localLinkId &&

                            !_context.MuxPorts.Any(
                                muxPort =>
                                    muxPort.E1Id ==
                                    e1.Id))

                        .OrderBy(e1 =>
                            e1.Stm.Number)

                        .ThenBy(e1 =>
                            e1.E1Number)

                        .Select(e1 =>
                            new SelectListItem
                            {
                                Value =
                                    e1.Id.ToString(),

                                Text =
                                    "STM " +
                                    e1.Stm.Number +
                                    " — E1 " +
                                    e1.E1Number
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
                    .ThenInclude(mux => mux.CommunicationLink)

            .FirstOrDefaultAsync(item =>
                item.Id == model.MuxPortId);

        if (port == null)
        {
            return NotFound();
        }


        /*
         * Only E1 cards can contain E1 channels.
         */
        if (port.MuxCard.CardType.Category !=
            CardCategory.E1)
        {
            ModelState.AddModelError(
                string.Empty,
                "E1 channels can only be assigned to E1 cards.");
        }


        if (port.E1Id.HasValue)
        {
            ModelState.AddModelError(
                string.Empty,
                "This port already has an E1 assigned.");
        }


        var e1 = model.E1Id.HasValue
            ? await _context.E1s
                .Include(item => item.Stm)
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
             * Logical E1s must never occupy
             * a physical MUX port.
             */
            if (e1.ConnectionType !=
                E1ConnectionType.Physical)
            {
                ModelState.AddModelError(
                    nameof(model.E1Id),
                    "Only Physical E1 channels can be assigned to a MUX port.");
            }


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


            var mux =
                port.MuxCard.Mux;

            var link =
                mux.CommunicationLink;

            Guid? localLinkId = null;

            if (link.SiteFromId == mux.SiteId)
            {
                localLinkId = link.Id;
            }
            else if (link.SiteToId == mux.SiteId &&
                     link.ConnectedLinkId.HasValue)
            {
                localLinkId =
                    link.ConnectedLinkId.Value;
            }


            if (!localLinkId.HasValue ||
                e1.Stm.LinkId != localLinkId.Value)
            {
                ModelState.AddModelError(
                    nameof(model.E1Id),
                    "The selected E1 does not belong to this MUX site's side of the communication link.");
            }
        }


        if (!ModelState.IsValid)
        {
            /*
             * We will improve form reloading
             * in the next step if validation fails.
             */
            TempData["ErrorMessage"] =
                "The E1 could not be assigned to this port.";

            return RedirectToAction(
                "Details",
                "MuxCards",
                new { id = port.MuxCardId });
        }


        /*
         * Assign the Physical E1.
         *
         * This does NOT change E1 operational status.
         * It only represents the physical MUX-port mapping.
         */
        port.E1Id =
            e1!.Id;

        port.Status =
            MuxPortStatus.Connected;


        await _context.SaveChangesAsync();


        TempData["SuccessMessage"] =
            $"STM {e1.Stm.Number} — E1 {e1.E1Number} " +
            $"was assigned to Port {port.PortNumber}.";


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