using CommunicationProject.Data;
using CommunicationProject.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    public async Task<IActionResult> Details(
        Guid? id)
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
}