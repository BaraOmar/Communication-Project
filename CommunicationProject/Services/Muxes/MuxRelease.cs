using CommunicationProject.Data;
using CommunicationProject.Models;
using Microsoft.EntityFrameworkCore;

namespace CommunicationProject.Services.Muxes;

public static class MuxRelease
{
    public static async Task ReleaseLtuAssignmentsAsync(
        CommunicationDbContext context,
        IReadOnlyCollection<E1> releasedEndpoints,
        IReadOnlyCollection<E1> adjacentEndpoints)
    {
        var releaseIds = releasedEndpoints
            .Select(e1 => e1.Id)
            .ToHashSet();

        var releasedPdhIds = releasedEndpoints
            .Where(e1 => !e1.StmId.HasValue)
            .Select(e1 => e1.Id)
            .ToHashSet();

        var pdhJoinedIds = releasedEndpoints
            .Where(e1 =>
                !e1.StmId.HasValue &&
                e1.JoinE1Id.HasValue)
            .Select(e1 => e1.JoinE1Id!.Value)
            .ToHashSet();

        // The LTU is stored against the SDH E1.
        // Include that E1 when its joined PDH leg is released.
        foreach (var endpoint in adjacentEndpoints)
        {
            if (endpoint.StmId.HasValue &&
                (
                    pdhJoinedIds.Contains(endpoint.Id) ||
                    (
                        endpoint.JoinE1Id.HasValue &&
                        releasedPdhIds.Contains(endpoint.JoinE1Id.Value)
                    )
                ))
            {
                releaseIds.Add(endpoint.Id);
            }
        }

        var ids = releaseIds.ToList();

        var ports = await context.MuxPorts
            .Where(port =>
                port.MuxCard.Category == CardCategory.LTU &&
                port.E1Id.HasValue &&
                ids.Contains(port.E1Id.Value))
            .ToListAsync();

        foreach (var port in ports)
        {
            port.E1Id = null;
            port.PhysicalStmNumber = null;
            port.Status = MuxPortStatus.Available;
        }
    }
}