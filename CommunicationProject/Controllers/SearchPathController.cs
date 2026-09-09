using CommunicationProject.Data;
using CommunicationProject.Models;
using CommunicationProject.Security;
using CommunicationProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

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
             * Start with every PathId stored in the E1 table.
             */
            /*
             * Start with every PathId stored in the E1 table.
             */
            IQueryable<Guid> pathIdsQuery =
                _context.E1s
                    .AsNoTracking()
                    .Where(e1 => e1.PathId.HasValue)
                    .Select(e1 => e1.PathId!.Value)
                    .Distinct();


            /*
             * Description search.
             *
             * Get matching PathIds directly instead of running
             * a correlated Any() for every path.
             */
            if (!string.IsNullOrWhiteSpace(model.Description))
            {
                string description =
                    model.Description.Trim();

                var descriptionPathIds =
                    _context.E1s
                        .AsNoTracking()
                        .Where(e1 =>
                            e1.PathId.HasValue &&
                            e1.Description != null &&
                            e1.Description.StartsWith(description))
                        .Select(e1 => e1.PathId!.Value)
                        .Distinct();

                pathIdsQuery =
                    pathIdsQuery.Intersect(
                        descriptionPathIds);
            }


            /*
             * Link filter.
             */
            if (!string.IsNullOrWhiteSpace(model.LinkName))
            {
                string linkName =
                    model.LinkName.Trim();

                var linkPathIds =
                    _context.E1s
                        .AsNoTracking()
                        .Where(e1 =>
                            e1.PathId.HasValue &&
                            e1.Stm.Link.Name == linkName)
                        .Select(e1 => e1.PathId!.Value)
                        .Distinct();

                pathIdsQuery =
                    pathIdsQuery.Intersect(
                        linkPathIds);
            }


            /*
             * Source site is the site of PathOrder 1.
             */
            if (!string.IsNullOrWhiteSpace(model.SourceSiteId))
            {
                string sourceSiteId =
                    model.SourceSiteId.Trim();

                var sourcePathIds =
                    _context.E1s
                        .AsNoTracking()
                        .Where(e1 =>
                            e1.PathId.HasValue &&
                            e1.PathOrder == 1 &&
                            e1.Stm.Link.SiteFromId ==
                                sourceSiteId)
                        .Select(e1 => e1.PathId!.Value)
                        .Distinct();

                pathIdsQuery =
                    pathIdsQuery.Intersect(
                        sourcePathIds);
            }

            var pathIds =
                await pathIdsQuery.ToListAsync();

            if (pathIds.Count == 0)
            {
                model.TotalItems = 0;
                model.TotalPages = 0;

                return View(model);
            }

            /*
             * Load the complete E1 records for the paths
             * that matched the filters.
             */
            var pathE1s = await _context.E1s
                .AsNoTracking()
                .Include(e1 => e1.Stm)
                    .ThenInclude(stm => stm.Link)
                .Where(e1 =>
                    e1.PathId.HasValue &&
                    pathIds.Contains(e1.PathId.Value) &&
                    e1.PathOrder.HasValue)
                .OrderBy(e1 => e1.PathId)
                .ThenBy(e1 => e1.PathOrder)
                .ToListAsync();

            var allResults =
                BuildPathResults(pathE1s);


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


            return View(model);
        }

        private static void NormalizeFilters(
            SearchPathViewModel model)
        {
            model.Description =
                model.Description?.Trim();

            model.LinkName =
                model.LinkName?.Trim();

            model.SourceSiteId =
                model.SourceSiteId?.Trim();
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

        private static List<PathSearchResultViewModel>
            BuildPathResults(List<E1> pathE1s)
        {
            var results =
                new List<PathSearchResultViewModel>();

            var pathGroups = pathE1s
                .Where(e1 => e1.PathId.HasValue)
                .GroupBy(e1 => e1.PathId!.Value);

            foreach (var group in pathGroups)
            {
                var orderedE1s = group
                    .Where(e1 => e1.PathOrder.HasValue)
                    .OrderBy(e1 => e1.PathOrder)
                    .ToList();

                /*
                 * Odd path orders represent each physical
                 * link in the direction of the path:
                 *
                 * 1, 3, 5, 7...
                 */
                var pathLegs = orderedE1s
                    .Where(e1 =>
                        e1.PathOrder!.Value % 2 == 1)
                    .OrderBy(e1 => e1.PathOrder)
                    .ToList();

                if (pathLegs.Count == 0)
                {
                    continue;
                }

                /*
                 * Create the detailed site path using every ordered E1.
                 *
                 * Odd orders represent the outgoing E1.
                 * Even orders represent the incoming E1.
                 *
                 * Intermediate sites contain two E1s:
                 * one incoming and one outgoing.
                 */
                var siteNodes =
                    new List<(string SiteId, List<string> Channels)>();

                foreach (var e1 in orderedE1s)
                {
                    string siteId =
                        e1.Stm.Link.SiteFromId;

                    string direction =
                        e1.PathOrder!.Value % 2 == 0
                            ? "IN"
                            : "OUT";

                    string channel =
                        $"{direction}: STM {e1.Stm.Number}, " +
                        $"E1 {e1.E1Number}";

                    /*
                     * Consecutive E1 records can belong to the same
                     * intermediate site.
                     */
                    if (siteNodes.Count > 0 &&
                        siteNodes[^1].SiteId == siteId)
                    {
                        siteNodes[^1].Channels.Add(channel);
                    }
                    else
                    {
                        siteNodes.Add(
                            (
                                siteId,
                                new List<string> { channel }
                            ));
                    }
                }

                string detailedSitePath =
                    string.Join(
                        " → ",
                        siteNodes.Select(node =>
                            $"{node.SiteId} " +
                            $"({string.Join(" | ", node.Channels)})"));

                string description = orderedE1s
                    .Select(e1 => e1.Description)
                    .FirstOrDefault(value =>
                        !string.IsNullOrWhiteSpace(value))
                    ?? string.Empty;

                results.Add(
                    new PathSearchResultViewModel
                    {

                        Description = description,

                        StartSiteId =
                            pathLegs[0]
                                .Stm
                                .Link
                                .SiteFromId,

                        EndSiteId =
                            pathLegs[^1]
                                .Stm
                                .Link
                                .SiteToId,

                        SitePath = detailedSitePath,

                        LinkPath =
                            string.Join(
                                " → ",
                                pathLegs.Select(e1 =>
                                    e1.Stm.Link.Name)),



                        LinkCount =
                            pathLegs.Count
                    });
            }

            var orderedResults = results
                .OrderBy(result =>
                    result.StartSiteId)
                .ThenBy(result =>
                    result.EndSiteId)
                .ThenBy(result =>
                    result.Description)
                .ToList();

            for (int index = 0;
                 index < orderedResults.Count;
                 index++)
            {
                orderedResults[index].PathNumber =
                    index + 1;
            }

            return orderedResults;
        }
    }
}