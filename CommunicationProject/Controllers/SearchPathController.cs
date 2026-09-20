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

namespace CommunicationProject.Controllers
{
    [Authorize(
    Roles =
        AppRoles.Admin + "," +
        AppRoles.Operator + "," +
        AppRoles.Viewer)]
    public class SearchPathController : Controller
    {
        private readonly CommunicationDbContext _context;

        public SearchPathController(
            CommunicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
    SearchPathViewModel model)
        {
            NormalizeFilters(model);

            await LoadFilterOptionsAsync(model);

            if (model.PageNumber < 1)
            {
                model.PageNumber = 1;
            }

            const int pageSize = 10;

            model.PageSize = pageSize;


            /*
             * Load only E1 records that belong to saved paths.
             *
             * We already created an index for PathId IS NOT NULL,
             * so this is efficient.
             */

            var communicationPaths =
    await _context.CommunicationPaths
        .AsNoTracking()
        .Where(path =>
            path.IsActive)

        .Include(path =>
            path.Segments)
            .ThenInclude(segment =>
                segment.CommunicationLink)

        .OrderBy(path =>
            path.CreatedAt)

        .ToListAsync();




            /*
             * Build connected routes.
             */
            var allResults =
                BuildCommunicationPathResults(
                    communicationPaths);


            /*
             * Site filtering over reusable paths.
             */
            if (!string.IsNullOrWhiteSpace(model.SiteId))
            {
                string siteId =
                    model.SiteId.Trim();

                if (model.SitePosition == "start")
                {
                    allResults = allResults
                        .Where(result =>
                            result.StartSiteId == siteId)
                        .ToList();
                }
                else if (model.SitePosition == "destination")
                {
                    allResults = allResults
                        .Where(result =>
                            result.EndSiteId == siteId)
                        .ToList();
                }
                else
                {
                    allResults = allResults
                        .Where(result =>
                            result.SitePath
                                .Split(
                                    new[] { " → " },
                                    StringSplitOptions.RemoveEmptyEntries)
                                .Any(site =>
                                    site.Equals(
                                        siteId,
                                        StringComparison.OrdinalIgnoreCase)))
                        .ToList();
                }
            }


            /*
             * Description filtering is now done over the
             * already-built paths instead of querying the huge
             * E1 table again.
             */
            if (!string.IsNullOrWhiteSpace(
                    model.Description))
            {
                string description =
                    model.Description.Trim();

                allResults = allResults
                    .Where(result =>
                        result.Description.Contains(
                            description,
                            StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }


            /*
             * Link filter.
             */
            if (!string.IsNullOrWhiteSpace(
                    model.LinkName))
            {
                string linkName =
                    model.LinkName.Trim();

                allResults = allResults
                    .Where(result =>
                        result.LinkPath
                            .Split(
                                new[] { " → " },
                                StringSplitOptions
                                    .RemoveEmptyEntries)
                            .Any(link =>
                                link.Equals(
                                    linkName,
                                    StringComparison
                                        .OrdinalIgnoreCase)))
                    .ToList();
            }


            model.TotalItems =
                allResults.Count;


            model.TotalPages =
                (int)Math.Ceiling(
                    model.TotalItems /
                    (double)model.PageSize);


            if (model.TotalPages > 0 &&
                model.PageNumber > model.TotalPages)
            {
                model.PageNumber =
                    model.TotalPages;
            }


            model.Results = allResults
                .Skip(
                    (model.PageNumber - 1) *
                    model.PageSize)
                .Take(model.PageSize)
                .ToList();
            var visiblePathIds =
    model.Results
        .Select(result => result.PathId)
        .ToList();

            if (visiblePathIds.Count > 0)
            {
                var customerConnections =
                    await _context.CustomerConnections
                        .AsNoTracking()
                        .Where(connection =>
                            visiblePathIds.Contains(
                                connection.CommunicationPathId))

                        .Include(connection =>
                            connection.Customer)

.Include(connection =>
    connection.Segments)
    .ThenInclude(segment =>
        segment.E1)
        .ThenInclude(e1 =>
            e1.Stm)

                        .Include(connection =>
                            connection.Segments)
                            .ThenInclude(segment =>
                                segment.CommunicationPathSegment)
                                .ThenInclude(pathSegment =>
                                    pathSegment.CommunicationLink)

                        .ToListAsync();


                foreach (var result in model.Results)
                {
                    result.Customers =
                        customerConnections
                            .Where(connection =>
                                connection.CommunicationPathId ==
                                result.PathId)

                            .OrderBy(connection =>
                                connection.Customer.Name)

                            .Select(connection =>
                            {
                                var customerSegments =
                                    connection.Segments
                                        .OrderBy(segment =>
                                            segment.CommunicationPathSegment.Order)
                                        .ToList();

                                return new CustomerPathUsageViewModel
                                {
                                    CustomerConnectionId =
                                        connection.Id,

                                    ManageE1Id =
    customerSegments.Count > 0
        ? customerSegments[0].E1Id
        : null,

                                    CustomerName =
                                        connection.Customer.Name,

                                    Description =
                                        connection.Description,

                                    IsActive =
                                        connection.IsActive,

                                    OperationalStatus =
    customerSegments.Count > 0
        ? customerSegments[0].E1.Status
        : null,

                                    StartSiteId =
                                        customerSegments.Count > 0
                                            ? customerSegments[0]
                                                .CommunicationPathSegment
                                                .CommunicationLink
                                                .SiteFromId
                                            : string.Empty,

                                    DestinationSiteId =
                                        customerSegments.Count > 0
                                            ? customerSegments[^1]
                                                .CommunicationPathSegment
                                                .CommunicationLink
                                                .SiteToId
                                            : string.Empty,

                                    E1s =
                                        customerSegments
                                            .Select(segment =>
                                                new CustomerPathE1ViewModel
                                                {
                                                    SegmentOrder =
                                                        segment
                                                            .CommunicationPathSegment
                                                            .Order,

                                                    SiteFrom =
                                                        segment
                                                            .CommunicationPathSegment
                                                            .CommunicationLink
                                                            .SiteFromId,

                                                    SiteTo =
                                                        segment
                                                            .CommunicationPathSegment
                                                            .CommunicationLink
                                                            .SiteToId,

                                                    StmNumber = segment.E1.Stm?.Number ?? "Direct (PDH)",

                                                    E1Number =
                                                        segment.E1.E1Number
                                                            .ToString()
                                                })
                                            .ToList()
                                };
                            })
                            .ToList();
                }
            }

            return View(model);
        }

        private static void NormalizeFilters(
            SearchPathViewModel model)
        {
            model.Description =
                string.IsNullOrWhiteSpace(
                    model.Description)
                    ? null
                    : model.Description.Trim();

            model.LinkName =
                string.IsNullOrWhiteSpace(
                    model.LinkName)
                    ? null
                    : model.LinkName.Trim();

            model.SiteId =
                string.IsNullOrWhiteSpace(
                    model.SiteId)
                    ? null
                    : model.SiteId.Trim();


            model.SitePosition =
                model.SitePosition
                    ?.Trim()
                    .ToLowerInvariant()
                switch
                {
                    "start" =>
                        "start",

                    "destination" =>
                        "destination",

                    _ =>
                        "anywhere"
                };
        }

        private async Task LoadFilterOptionsAsync(
            SearchPathViewModel model)
        {
            /*
             * Distinct removes the duplicate directional
             * record for each logical link.
             */
            model.LinkOptions =
                await _context.CommunicationLinks
                    .AsNoTracking()
                    .Select(link => link.Name)
                    .Distinct()
                    .OrderBy(name => name)
                    .Select(name =>
                        new SelectListItem
                        {
                            Value = name,
                            Text = name
                        })
                    .ToListAsync();

            model.SiteOptions =
                await _context.Sites
                    .AsNoTracking()
                    .OrderBy(site => site.Id)
                    .Select(site =>
                        new SelectListItem
                        {
                            Value = site.Id,
                            Text = site.Id +
                                   " - " +
                                   site.Name
                        })
                    .ToListAsync();
        }


        [HttpGet]
        [Authorize(Roles = "Admin,Operator")]
        public async Task<IActionResult> AddCustomer(Guid pathId)
        {
            var path = await _context.CommunicationPaths
                .AsNoTracking()
                .Include(path => path.Segments)
                    .ThenInclude(segment =>
                        segment.CommunicationLink)
                .FirstOrDefaultAsync(path =>
                    path.Id == pathId);

            if (path == null)
            {
                return NotFound();
            }
            var orderedSegments =
    path.Segments
        .OrderBy(segment => segment.Order)
        .ToList();

            var pathSiteIds =
    orderedSegments
        .Select(segment =>
            segment.CommunicationLink.SiteFromId)
        .ToList();

            if (orderedSegments.Count > 0)
            {
                pathSiteIds.Add(
                    orderedSegments[^1]
                        .CommunicationLink.SiteToId);
            }

            var model = new AddCustomerToPathViewModel
            {
                PathId = path.Id,

                PathDisplay = string.Join(
                    " → ",
                    pathSiteIds),

                PathSites =
                    pathSiteIds
                        .Select(siteId =>
                            new SelectListItem
                            {
                                Value = siteId,
                                Text = siteId
                            })
                        .ToList()
            };

            var usedE1Ids =
    await _context.CustomerConnectionSegments
        .AsNoTracking()
        .Select(segment => segment.E1Id)
        .ToListAsync();

            foreach (var segment in path.Segments
                         .OrderBy(segment => segment.Order))
            {
                var availableE1s =
    await _context.E1s
        .AsNoTracking()
        .Where(e1 =>
            e1.Stm.LinkId ==
                segment.CommunicationLinkId &&

            e1.ConnectionType ==
                E1ConnectionType.Physical &&

            e1.ConnectedE1Id != null &&

            e1.Status ==
                E1OperationalStatus.Available &&

            e1.ConnectedE1!.Status ==
                E1OperationalStatus.Available &&

            e1.CrossConnectionState ==
                E1CrossConnectionState.Available &&

            e1.ConnectedE1.CrossConnectionState ==
                E1CrossConnectionState.Available &&

            e1.ConnectionGroupId == null &&

            e1.ConnectedE1.ConnectionGroupId == null &&

            !usedE1Ids.Contains(e1.Id) &&

            !usedE1Ids.Contains(
                e1.ConnectedE1Id.Value))
        .OrderBy(e1 => e1.E1Number)
.Select(e1 =>
    new SelectListItem
    {
        Value = e1.Id.ToString(),
        Text = $"STM {e1.Stm.Number} — E1 {e1.E1Number}"
    })
        .ToListAsync();

                model.Segments.Add(
                    new CustomerPathSegmentInputViewModel
                    {
                        CommunicationPathSegmentId =
                            segment.Id,

                        Order =
                            segment.Order,

                        SiteFrom =
                            segment.CommunicationLink.SiteFromId,

                        SiteTo =
                            segment.CommunicationLink.SiteToId,

                        AvailableE1s = availableE1s
                    });
            }

            return View(model);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Operator")]
        public async Task<IActionResult> AddCustomer(
    AddCustomerToPathViewModel model)
        {
            string customerName =
                model.CustomerName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(customerName))
            {
                return BadRequest("Customer name is required.");
            }

            var path = await _context.CommunicationPaths
                .Include(path => path.Segments)
                    .ThenInclude(segment =>
                        segment.CommunicationLink)
                .FirstOrDefaultAsync(path =>
                    path.Id == model.PathId);

            if (path == null)
            {
                return NotFound();
            }

            var orderedPathSegments =
                path.Segments
                    .OrderBy(segment => segment.Order)
                    .ToList();

            var pathSiteIds =
                orderedPathSegments
                    .Select(segment =>
                        segment.CommunicationLink.SiteFromId)
                    .ToList();

            if (orderedPathSegments.Count > 0)
            {
                pathSiteIds.Add(
                    orderedPathSegments[^1]
                        .CommunicationLink.SiteToId);
            }


            int startIndex =
                pathSiteIds.IndexOf(model.StartSiteId);

            int destinationIndex =
                pathSiteIds.IndexOf(model.DestinationSiteId);


            if (startIndex < 0 ||
                destinationIndex < 0)
            {
                return BadRequest(
                    "The selected sites do not belong to this path.");
            }

            if (startIndex >= destinationIndex)
            {
                return BadRequest(
                    "The destination site must come after the start site.");
            }


            var requiredPathSegments =
                orderedPathSegments
                    .Skip(startIndex)
                    .Take(destinationIndex - startIndex)
                    .ToList();


            var requiredSegmentIds =
                requiredPathSegments
                    .Select(segment => segment.Id)
                    .ToHashSet();


            var postedRequiredSegments =
                model.Segments
                    .Where(segment =>
                        requiredSegmentIds.Contains(
                            segment.CommunicationPathSegmentId))
                    .ToList();


            if (postedRequiredSegments.Count !=
                requiredPathSegments.Count)
            {
                return BadRequest(
                    "One or more required path segments are missing.");
            }


            if (postedRequiredSegments.Any(segment =>
                    !segment.E1Id.HasValue))
            {
                return BadRequest(
                    "Select an E1 for every segment between the start and destination sites.");
            }


            var selectedE1Ids =
                postedRequiredSegments
                    .Select(segment =>
                        segment.E1Id!.Value)
                    .ToList();


            if (selectedE1Ids.Distinct().Count() !=
                selectedE1Ids.Count)
            {
                return BadRequest(
                    "The same E1 cannot be used twice.");
            }

            if (selectedE1Ids.Distinct().Count() !=
                selectedE1Ids.Count)
            {
                return BadRequest(
                    "The same E1 cannot be used twice.");
            }

            await using var transaction =
                await _context.Database
                    .BeginTransactionAsync(
                        IsolationLevel.Serializable);

            try
            {
                var selectedE1s =
                    await _context.E1s
                        .Include(e1 => e1.Stm)
                            .ThenInclude(stm => stm.Link)
                        .Include(e1 => e1.ConnectedE1)
                        .Where(e1 =>
                            selectedE1Ids.Contains(e1.Id))
                        .ToListAsync();

                if (selectedE1s.Count !=
                    selectedE1Ids.Count)
                {
                    return BadRequest(
                        "One or more selected E1s no longer exist.");
                }

                foreach (var postedSegment in postedRequiredSegments)
                {
                    var pathSegment =
                        orderedPathSegments
                            .FirstOrDefault(segment =>
                                segment.Id ==
                                postedSegment
                                    .CommunicationPathSegmentId);

                    var e1 =
                        selectedE1s
                            .FirstOrDefault(item =>
                                item.Id ==
                                postedSegment.E1Id);

                    if (pathSegment == null ||
                        e1 == null)
                    {
                        return BadRequest(
                            "Invalid path segment or E1.");
                    }

                    if (e1.Stm.LinkId !=
                        pathSegment.CommunicationLinkId)
                    {
                        return BadRequest(
                            $"The selected E1 does not belong to " +
                            $"segment {pathSegment.Order}.");
                    }

                    if (e1.ConnectionType !=
                        E1ConnectionType.Physical ||
                        e1.ConnectedE1 == null)
                    {
                        return BadRequest(
                            "Only physical connected E1s can be used.");
                    }

                    if (e1.Status !=
                            E1OperationalStatus.Available ||
                        e1.ConnectedE1.Status !=
                            E1OperationalStatus.Available ||

                        e1.CrossConnectionState !=
                            E1CrossConnectionState.Available ||
                        e1.ConnectedE1.CrossConnectionState !=
                            E1CrossConnectionState.Available ||

                        e1.ConnectionGroupId.HasValue ||
                        e1.ConnectedE1.ConnectionGroupId.HasValue)
                    {
                        return BadRequest(
                            $"E1 {e1.E1Number} is no longer available.");
                    }
                }

                var customer =
                    await _context.Customers
                        .FirstOrDefaultAsync(customer =>
                            customer.Name == customerName);

                if (customer == null)
                {
                    customer = new Customer
                    {
                        Id = Guid.NewGuid(),
                        Name = customerName,
                        IsActive = true
                    };

                    _context.Customers.Add(customer);
                }

                Guid connectionGroupId =
                    Guid.NewGuid();

                var customerConnection =
                    new CustomerConnection
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customer.Id,
                        CommunicationPathId = path.Id,
                        ConnectionGroupId = connectionGroupId,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                _context.CustomerConnections.Add(
                    customerConnection);

                // Existing cross-connections at intermediate sites.
                E1? previousDirectionalE1 = null;

                foreach (var pathSegment in
                         requiredPathSegments)
                {
                    var postedSegment =
                        model.Segments
                            .First(segment =>
                                segment
                                    .CommunicationPathSegmentId ==
                                pathSegment.Id);

                    var e1 =
                        selectedE1s
                            .First(item =>
                                item.Id ==
                                postedSegment.E1Id!.Value);

                    var connectedE1 =
                        e1.ConnectedE1!;

                    e1.ConnectionGroupId =
                        connectionGroupId;

                    connectedE1.ConnectionGroupId =
                        connectionGroupId;

                    e1.Status =
                        E1OperationalStatus.Connected;



                    connectedE1.Status =
                        E1OperationalStatus.Connected;

                    if (pathSegment.Order ==
    path.Segments.Max(segment => segment.Order))
                    {
                        connectedE1.CrossConnectionState =
                            E1CrossConnectionState.ExtendExistingPath;
                    }

                    if (previousDirectionalE1 != null)
                    {
                        var incomingAtSite =
                            previousDirectionalE1
                                .ConnectedE1!;

                        incomingAtSite.JoinE1Id =
                            e1.Id;

                        e1.JoinE1Id =
                            incomingAtSite.Id;

                        incomingAtSite.CrossConnectionState =
                            E1CrossConnectionState.CrossConnected;

                        e1.CrossConnectionState =
                            E1CrossConnectionState.CrossConnected;
                    }

                    _context.CustomerConnectionSegments.Add(
                        new CustomerConnectionSegment
                        {
                            Id = Guid.NewGuid(),

                            CustomerConnectionId =
                                customerConnection.Id,

                            CommunicationPathSegmentId =
                                pathSegment.Id,

                            E1Id =
                                e1.Id
                        });

                    previousDirectionalE1 = e1;
                }

                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    $"Customer {customerName} added to the path successfully.";

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        private static List<PathSearchResultViewModel>
    BuildCommunicationPathResults(
        List<CommunicationPath> paths)
        {
            var results =
                new List<PathSearchResultViewModel>();

            foreach (var path in paths)
            {
                var segments =
                    path.Segments
                        .OrderBy(segment =>
                            segment.Order)
                        .ToList();

                if (segments.Count == 0)
                {
                    continue;
                }

                var siteIds =
                    segments
                        .Select(segment =>
                            segment.CommunicationLink.SiteFromId)
                        .ToList();

                siteIds.Add(
                    segments[^1]
                        .CommunicationLink
                        .SiteToId);

                var linkNames =
                    segments
                        .Select(segment =>
                            segment.CommunicationLink.Name)
                        .ToList();

                results.Add(
                    new PathSearchResultViewModel
                    {
                        PathId =
                            path.Id,

                        Description =
                            path.Description ??
                            string.Empty,

                        StartSiteId =
                            siteIds[0],

                        EndSiteId =
                            siteIds[^1],

                        SitePath =
                            string.Join(
                                " → ",
                                siteIds),

                        LinkPath =
                            string.Join(
                                " → ",
                                linkNames),

                        LinkCount =
                            segments.Count
                    });
            }

            for (int index = 0;
                 index < results.Count;
                 index++)
            {
                results[index].PathNumber =
                    index + 1;
            }

            return results;
        }
    }
}