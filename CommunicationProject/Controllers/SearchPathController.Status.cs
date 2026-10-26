using CommunicationProject.Models;
using CommunicationProject.Security;
using CommunicationProject.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;
using static CommunicationProject.Models.E1;

namespace CommunicationProject.Controllers;

public partial class SearchPathController
{
    [HttpGet]
    [Authorize(
        Roles = AppRoles.Admin + "," + AppRoles.Operator)]
    public async Task<IActionResult> FocusE1Status(Guid id)
    {
        var e1 = await _context.E1s
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (e1?.ConnectionGroupId == null)
        {
            TempData["ErrorMessage"] =
                "This E1 is not assigned to a documented customer path.";

            return RedirectToAction(nameof(Index));
        }

        var pathId = await _context.CustomerConnections
            .AsNoTracking()
            .Where(connection =>
                connection.IsActive &&
                connection.ConnectionGroupId ==
                    e1.ConnectionGroupId.Value &&
                connection.CommunicationPath.IsActive &&
                connection.Segments.Any(segment =>
                    segment.E1Id == id ||
                    segment.E1.ConnectedE1Id == id))
            .Select(connection =>
                (Guid?)connection.CommunicationPathId)
            .FirstOrDefaultAsync();

        if (!pathId.HasValue)
        {
            TempData["ErrorMessage"] =
                "The documented path for this E1 could not be found.";

            return RedirectToAction(nameof(Index));
        }

        var pathIds = await _context.CommunicationPaths
            .AsNoTracking()
            .Where(path =>
                path.IsActive &&
                path.Segments.Any())
            .OrderBy(path => path.CreatedAt)
            .ThenBy(path => path.Id)
            .Select(path => path.Id)
            .ToListAsync();

        int pathIndex = pathIds.IndexOf(pathId.Value);

        if (pathIndex < 0)
        {
            return RedirectToAction(nameof(Index));
        }

        string indexUrl = Url.Action(
            nameof(Index),
            new
            {
                PageNumber = pathIndex / 10 + 1,
                focusE1Id = id
            })!;

        return Redirect(
            indexUrl + $"#path-body-{pathId.Value}");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(
        Roles = AppRoles.Admin + "," + AppRoles.Operator)]
    public async Task<IActionResult> ChangeE1Status(
        ChangePathE1StatusViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = string.Join(
                " ",
                ModelState.Values
                    .SelectMany(value => value.Errors)
                    .Select(error =>
                        string.IsNullOrWhiteSpace(error.ErrorMessage)
                            ? "Invalid status update."
                            : error.ErrorMessage));

            return ReturnToPath(model.ReturnUrl);
        }

        model.VisitorName = model.VisitorName.Trim();
        model.Description = model.Description?.Trim();

        if (string.IsNullOrWhiteSpace(model.VisitorName))
        {
            TempData["ErrorMessage"] =
                "Visitor name is required.";

            return ReturnToPath(model.ReturnUrl);
        }

        var selectedIds = model.E1Ids
            .Distinct()
            .ToList();

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable);

        try
        {
            var connection = await _context.CustomerConnections
                .Include(item => item.CommunicationPath)
                .Include(item => item.Segments)
                    .ThenInclude(segment => segment.E1)
                .FirstOrDefaultAsync(item =>
                    item.Id == model.CustomerConnectionId);

            if (connection == null ||
                !connection.IsActive ||
                !connection.CommunicationPath.IsActive)
            {
                TempData["ErrorMessage"] =
                    "This customer connection is no longer active.";

                return ReturnToPath(model.ReturnUrl);
            }

            // Both endpoints of each assigned physical E1 pair
            // are eligible, but only explicitly selected IDs change.
            var allowedIds = connection.Segments
                .SelectMany(segment =>
                    segment.E1.ConnectedE1Id.HasValue
                        ? new[]
                        {
                            segment.E1Id,
                            segment.E1.ConnectedE1Id.Value
                        }
                        : new[]
                        {
                            segment.E1Id
                        })
                .ToHashSet();

            if (selectedIds.Any(id => !allowedIds.Contains(id)))
            {
                TempData["ErrorMessage"] =
                    "One or more selected E1s do not belong " +
                    "to this customer's path.";

                return ReturnToPath(model.ReturnUrl);
            }

            var selectedE1s = await _context.E1s
                .Where(e1 =>
                    selectedIds.Contains(e1.Id) &&
                    e1.ConnectionGroupId ==
                        connection.ConnectionGroupId)
                .ToListAsync();

            if (selectedE1s.Count != selectedIds.Count ||
                selectedE1s.Any(e1 =>
                    e1.Status == E1OperationalStatus.Available))
            {
                TempData["ErrorMessage"] =
                    "One or more selected E1s have been released " +
                    "or reassigned. Refresh the path.";

                return ReturnToPath(model.ReturnUrl);
            }

            E1OperationalStatus status =
                model.Status!.Value;
            if (status == E1OperationalStatus.Available)
            {
                if (!await ReleaseSelectedPathE1sAsync(
                    connection, selectedIds, model))
                {
                    return ReturnToPath(model.ReturnUrl);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["SuccessMessage"] =
                    "Selected physical E1 pair(s) released. " +
                    "Other E1 assignments were kept.";

                return ReturnToPath(model.ReturnUrl);
            }
            foreach (var e1 in selectedE1s)
            {
                e1.Status = status;
                e1.VisitorName = model.VisitorName;
                e1.VisitDate = model.VisitDate;

                if (!string.IsNullOrWhiteSpace(model.Description))
                {
                    e1.Description = model.Description;
                }
            }

            // Update only E1/LTU ports assigned to selected endpoints.
            var ports = await _context.MuxPorts
                .Where(port =>
                    port.E1Id.HasValue &&
                    selectedIds.Contains(port.E1Id.Value) &&
                    port.MuxCard.Category == CardCategory.LTU)
                .ToListAsync();

            foreach (var port in ports)
            {
                port.Status = status switch
                {
                    E1OperationalStatus.Connected =>
                        MuxPortStatus.Connected,

                    E1OperationalStatus.Wrong =>
                        MuxPortStatus.Wrong,


                    _ => throw new InvalidOperationException(
                        "Invalid endpoint status.")
                };
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["SuccessMessage"] =
                $"Updated {selectedE1s.Count} E1 endpoint(s) " +
                $"to {status}.";
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        return ReturnToPath(model.ReturnUrl);
    }
    private async Task<bool> ReleaseSelectedPathE1sAsync(
    CustomerConnection connection,
    List<Guid> selectedIds,
    ChangePathE1StatusViewModel model)
    {
        var segments = connection.Segments
            .Where(segment =>
                selectedIds.Contains(segment.E1Id) ||
                (
                    segment.E1.ConnectedE1Id.HasValue &&
                    selectedIds.Contains(
                        segment.E1.ConnectedE1Id.Value)
                ))
            .ToList();

        if (segments.Count == 0 ||
            segments.Any(segment =>
                !segment.E1.ConnectedE1Id.HasValue))
        {
            TempData["ErrorMessage"] =
                "The connected physical E1 could not be found. " +
                "Refresh the path.";

            return false;
        }

        // Selecting either endpoint releases its pair.
        // Selecting both endpoints releases the pair once.
        var pairIds = segments
            .SelectMany(segment => new[]
            {
            segment.E1Id,
            segment.E1.ConnectedE1Id!.Value
            })
            .Distinct()
            .ToList();

        var releasedE1s = await _context.E1s
            .Where(e1 =>
                pairIds.Contains(e1.Id) &&
                e1.ConnectionGroupId == connection.ConnectionGroupId)
            .ToListAsync();

        var pairLookup = releasedE1s
            .ToDictionary(e1 => e1.Id);

        if (releasedE1s.Count != pairIds.Count ||
            releasedE1s.Any(e1 =>
                e1.Status == E1OperationalStatus.Available ||
                !e1.ConnectedE1Id.HasValue ||
                !pairLookup.TryGetValue(
                    e1.ConnectedE1Id.Value, out var opposite) ||
                opposite.ConnectedE1Id != e1.Id))
        {
            TempData["ErrorMessage"] =
                "One or more physical E1 pairs have changed. " +
                "Refresh the path.";

            return false;
        }

        var joinedIds = releasedE1s
            .Where(e1 => e1.JoinE1Id.HasValue)
            .Select(e1 => e1.JoinE1Id!.Value)
            .Distinct()
            .ToList();

        // Disconnect adjacent cross connections while
        // retaining the other E1 assignments.
        var adjacentEndpoints = await _context.E1s
            .Where(e1 =>
                !pairIds.Contains(e1.Id) &&
                (
                    joinedIds.Contains(e1.Id) ||
                    (
                        e1.JoinE1Id.HasValue &&
                        pairIds.Contains(e1.JoinE1Id.Value)
                    )
                ))
            .ToListAsync();

        foreach (var endpoint in adjacentEndpoints)
        {
            endpoint.JoinE1Id = null;

            endpoint.CrossConnectionState =
                E1CrossConnectionState.ExtendExistingPath;
        }

        foreach (var e1 in releasedE1s)
        {
            e1.Status = E1OperationalStatus.Available;
            e1.ConnectionGroupId = null;
            e1.JoinE1Id = null;

            e1.CrossConnectionState =
                E1CrossConnectionState.Available;

            e1.VisitorName = model.VisitorName;
            e1.VisitDate = model.VisitDate;

            if (!string.IsNullOrWhiteSpace(model.Description))
            {
                e1.Description = model.Description;
            }

            // Keep ConnectedE1Id: it describes the
            // permanent physical pairing.
        }

        var ports = await _context.MuxPorts
            .Where(port =>
                port.E1Id.HasValue &&
                pairIds.Contains(port.E1Id.Value))
            .ToListAsync();

        foreach (var port in ports)
        {
            port.E1Id = null;
            port.Status = MuxPortStatus.Available;
        }

        if (segments.Count == connection.Segments.Count)
        {
            // No E1 assignments remain for this customer connection.
            _context.CustomerConnections.Remove(connection);
        }
        else
        {
            _context.CustomerConnectionSegments.RemoveRange(segments);
        }

        return true;
    }
    private IActionResult ReturnToPath(string? returnUrl)
    {
        string indexPath =
            Url.Action(nameof(Index))!;
        
        string returnPath =
            (returnUrl?.Split('?', '#')[0] ?? string.Empty)
                .TrimEnd('/');

        var acceptedPaths = new[]
        {
            indexPath.TrimEnd('/'),
            Url.Content("~/SearchPath/Index").TrimEnd('/')
        };

        if (!string.IsNullOrWhiteSpace(returnUrl) &&
            Url.IsLocalUrl(returnUrl) &&
            acceptedPaths.Contains(
                returnPath,
                StringComparer.OrdinalIgnoreCase))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    private static List<PathE1EndpointViewModel>
        BuildStatusEndpoints(
            CustomerConnection connection,
            IReadOnlyDictionary<Guid, MuxPort> stmPortLookup)
    {
        var endpoints =
            new List<PathE1EndpointViewModel>();

        var seenIds = new HashSet<Guid>();

        foreach (var segment in connection.Segments
            .OrderBy(item =>
                item.CommunicationPathSegment.Order))
        {
            var link =
                segment.CommunicationPathSegment
                    .CommunicationLink;

            AddEndpoint(
                segment.E1,
                link.SiteFromId,
                link.Name);

            if (segment.E1.ConnectedE1 != null)
            {
                AddEndpoint(
                    segment.E1.ConnectedE1,
                    link.SiteToId,
                    link.Name);
            }
        }

        return endpoints;

        void AddEndpoint(
            E1 e1,
            string siteId,
            string linkName)
        {
            if (!seenIds.Add(e1.Id))
            {
                return;
            }

            endpoints.Add(
                new PathE1EndpointViewModel
                {
                    E1Id = e1.Id,
                    SiteId = siteId,
                    LinkName = linkName,

                    DisplayText =
                        BuildStmE1Display(
                            e1,
                            stmPortLookup),

                    Status = e1.Status
                });
        }
    }
}