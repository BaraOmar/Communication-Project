using CommunicationProject.Data;
using CommunicationProject.Models;
using CommunicationProject.Services.CommunicationLinks.Import;
using CommunicationProject.Strategies.Capacity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


// ==========================================================
// MVC
// Require authentication for all MVC controllers by default.
// ==========================================================

builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.Filters.Add(
        new AuthorizeFilter(policy));
});


// ==========================================================
// Razor Pages / Identity
// Login must remain accessible anonymously.
// ==========================================================

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AllowAnonymousToAreaPage(
        "Identity",
        "/Account/Login");
});


// ==========================================================
// Database
// ==========================================================

builder.Services.AddDbContext<CommunicationDbContext>(
    options =>
        options.UseSqlServer(
            builder.Configuration
                .GetConnectionString("DefaultConnection")));


// ==========================================================
// Identity
// ==========================================================

builder.Services
    .AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;

        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;

        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<CommunicationDbContext>();


builder.Services.AddAuthorization();


// ==========================================================
// Application Services
// ==========================================================

builder.Services.AddScoped<
    ICapacityStrategy,
    StmCapacityStrategy>();

builder.Services.AddScoped<
    UnsupportedCapacityStrategy>();

builder.Services.AddScoped<
    ICapacityStrategyResolver,
    CapacityStrategyResolver>();

builder.Services.AddScoped<
    IStmInventoryFactory,
    StmInventoryFactory>();

builder.Services.AddScoped<
    CommunicationLinkImportPlanner>();

builder.Services.AddScoped<
    CommunicationLinkImportWriter>();

builder.Services.AddScoped<
    ICommunicationLinkImportService,
    CommunicationLinkImportService>();


var app = builder.Build();


// ==========================================================
// Seed Identity
// ==========================================================

await using (var scope =
    app.Services.CreateAsyncScope())
{
    await IdentitySeeder.SeedAsync(
        scope.ServiceProvider,
        app.Configuration);
}


// ==========================================================
// Error Handling
// ==========================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler(
        "/Home/Error");

    app.UseHsts();
}


// ==========================================================
// HTTP Pipeline
// ==========================================================

app.UseHttpsRedirection();


// Disable public registration.
// This blocks direct URL access as well.
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments(
            "/Identity/Account/Register") ||
        context.Request.Path.StartsWithSegments(
            "/Identity/Account/RegisterConfirmation"))
    {
        context.Response.Redirect(
            "/Identity/Account/Login");

        return;
    }

    await next();
});


app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();


// ==========================================================
// Routes
// ==========================================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


// Do NOT add RequireAuthorization() here.
// Login must remain anonymous.
app.MapRazorPages();


app.Run();