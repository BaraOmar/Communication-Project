using CommunicationProject.Data;
using CommunicationProject.Models;
using CommunicationProject.Security;
using CommunicationProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CommunicationProject.Controllers
{
[Authorize(Roles = AppRoles.Admin)]
    public class SitesController : Controller
    {
        private readonly CommunicationDbContext _context;
        private readonly ILogger<SitesController> _logger;

        public SitesController(
            CommunicationDbContext context,
            ILogger<SitesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Sites
        public async Task<IActionResult> Index(
            string? search,
            string? location,
            int pageNumber = 1)
        {
            const int pageSize = 10;

            if (pageNumber < 1)
            {
                pageNumber = 1;
            }

            var query = _context.Sites
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(s =>
                    s.Id.Contains(search) ||
                    s.Name.Contains(search) ||
                    s.Location.Contains(search));
            }

            if (!string.IsNullOrWhiteSpace(location))
            {
                query = query.Where(s =>
                    s.Location == location);
            }

            var totalItems = await query.CountAsync();

            var totalPages = (int)Math.Ceiling(
                totalItems / (double)pageSize);

            if (totalPages > 0 &&
                pageNumber > totalPages)
            {
                pageNumber = totalPages;
            }

            var sites = await query
                .OrderBy(s => s.Name)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var locations = await _context.Sites
                .AsNoTracking()
                .Select(s => s.Location)
                .Distinct()
                .OrderBy(locationName => locationName)
                .ToListAsync();

            var model = new SiteIndexViewModel
            {
                Sites = sites,
                Search = search,
                Location = location,
                Locations = locations,
                PageNumber = pageNumber,
                TotalPages = totalPages,
                TotalItems = totalItems
            };

            return View(model);
        }

        // GET: Sites/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var site = await _context.Sites
                .FirstOrDefaultAsync(m => m.Id == id);
            if (site == null)
            {
                return NotFound();
            }

            return View(site);
        }

        // GET: Sites/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Sites/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Id,Name,Location")] Site site)
        {
            // Data annotation errors are returned to the form.
            if (!ModelState.IsValid)
            {
                return View(site);
            }

            // Remove unnecessary spaces before saving.
            site.Id = site.Id.Trim();
            site.Name = site.Name.Trim();
            site.Location = site.Location.Trim();

            // Check before attempting the INSERT.
            bool idAlreadyExists = await _context.Sites
                .AsNoTracking()
                .AnyAsync(s => s.Id == site.Id);

            if (idAlreadyExists)
            {
                ModelState.AddModelError(
                    nameof(Site.Id),
                    $"Site ID '{site.Id}' already exists.");

                return View(site);
            }

            try
            {
                _context.Sites.Add(site);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    $"Site '{site.Name}' was created successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException exception)
                when (exception.InnerException is SqlException sqlException &&
                      (sqlException.Number == 2601 ||
                       sqlException.Number == 2627))
            {
                // Handles a duplicate submitted between the check and SaveChanges.
                _logger.LogWarning(
                    exception,
                    "Duplicate Site ID {SiteId} was submitted.",
                    site.Id);

                ModelState.AddModelError(
                    nameof(Site.Id),
                    $"Site ID '{site.Id}' already exists.");
            }
            catch (DbUpdateException exception)
            {
                _logger.LogError(
                    exception,
                    "A database error occurred while creating Site {SiteId}.",
                    site.Id);

                ModelState.AddModelError(
                    string.Empty,
                    "The site could not be saved because of a database error. Please try again.");
            }

            return View(site);
        }

        // GET: Sites/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var site = await _context.Sites.FindAsync(id);
            if (site == null)
            {
                return NotFound();
            }
            return View(site);
        }

        // POST: Sites/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("Id,Name,Location")] Site site)
        {
            if (id != site.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(site);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SiteExists(site.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(site);
        }

        // GET: Sites/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var site = await _context.Sites
                .FirstOrDefaultAsync(m => m.Id == id);
            if (site == null)
            {
                return NotFound();
            }

            return View(site);
        }

        // POST: Sites/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var site = await _context.Sites.FindAsync(id);

            if (site == null)
            {
                return NotFound();
            }

            bool siteIsUsed = await _context.CommunicationLinks
                .AsNoTracking()
                .AnyAsync(link =>
                    link.SiteFromId == id ||
                    link.SiteToId == id);

            if (siteIsUsed)
            {
                ModelState.AddModelError(
                    string.Empty,
                    $"Site '{site.Name}' cannot be deleted because it is used by one or more communication links.");

                return View("Delete", site);
            }

            try
            {
                _context.Sites.Remove(site);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    $"Site '{site.Name}' was deleted successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException exception)
                when (exception.InnerException is SqlException sqlException &&
                      sqlException.Number == 547)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "This site cannot be deleted because another record depends on it.");

                return View("Delete", site);
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "A database error occurred while deleting the site.");

                return View("Delete", site);
            }
        }
        private bool SiteExists(string id)
        {
            return _context.Sites.Any(site => site.Id == id);
        }
    }
}
