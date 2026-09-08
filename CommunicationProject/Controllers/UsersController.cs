using CommunicationProject.Models;
using CommunicationProject.Security;
using CommunicationProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunicationProject.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
    public class UsersController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UsersController(
            UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }
        [HttpGet]
        public async Task<IActionResult> Index(
    string? search,
    string? role,
    int pageNumber = 1)
        {
            const int pageSize = 10;

            if (pageNumber < 1)
                pageNumber = 1;

            var query = _userManager.Users.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(u =>
                    u.FullName.Contains(search) ||
                    (u.Email != null && u.Email.Contains(search)));
            }

            if (!string.IsNullOrWhiteSpace(role))
            {
                var usersInRole =
                    await _userManager.GetUsersInRoleAsync(role);

                var userIds = usersInRole
                    .Select(u => u.Id)
                    .ToList();

                query = query.Where(u =>
                    userIds.Contains(u.Id));
            }

            var totalUsers = await query.CountAsync();

            var totalPages =
                (int)Math.Ceiling(
                    totalUsers / (double)pageSize);

            var users = await query
                .OrderBy(u => u.FullName)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var userItems = new List<UserItemViewModel>();

            foreach (var user in users)
            {
                var roles =
                    await _userManager.GetRolesAsync(user);

                userItems.Add(new UserItemViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? "",
                    Role = roles.FirstOrDefault() ?? "No Role"
                });
            }

            var model = new UserListViewModel
            {
                Users = userItems,

                Search = search,
                Role = role,

                PageNumber = pageNumber,
                TotalPages = totalPages
            };

            ViewBag.Roles = AppRoles.All;

            return View(model);
        }
        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.Roles = AppRoles.All;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            CreateUserViewModel model)
        {
            if (!AppRoles.All.Contains(model.Role))
            {
                ModelState.AddModelError(
                    nameof(model.Role),
                    "Invalid role.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = AppRoles.All;
                return View(model);
            }

            var existingUser =
                await _userManager.FindByEmailAsync(
                    model.Email);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "A user with this email already exists.");

                ViewBag.Roles = AppRoles.All;

                return View(model);
            }

            var user = new ApplicationUser
            {
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                UserName = model.Email.Trim(),
                EmailConfirmed = true
            };

            IdentityResult createResult =
                await _userManager.CreateAsync(
                    user,
                    model.Password);

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                ViewBag.Roles = AppRoles.All;

                return View(model);
            }

            IdentityResult roleResult =
                await _userManager.AddToRoleAsync(
                    user,
                    model.Role);

            if (!roleResult.Succeeded)
            {
                // Don't leave a user without a role.
                await _userManager.DeleteAsync(user);

                foreach (var error in roleResult.Errors)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        error.Description);
                }

                ViewBag.Roles = AppRoles.All;

                return View(model);
            }

            TempData["SuccessMessage"] =
                $"User {user.FullName} was created successfully.";

            return RedirectToAction(nameof(Create));
        }
    }
}