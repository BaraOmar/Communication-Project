using CommunicationProject.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunicationProject.Controllers;

[Authorize(Roles = "Admin,Operator,Viewer")]
public class CustomersController : Controller
{
    private readonly CommunicationDbContext _context;

    public CustomersController(
        CommunicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var customers = await _context.Customers
            .AsNoTracking()
            .OrderBy(customer => customer.Name)
            .ToListAsync();

        return View(customers);
    }
}