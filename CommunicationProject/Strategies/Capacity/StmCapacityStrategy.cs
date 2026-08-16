using CommunicationProject.Data;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace CommunicationProject.Strategies.Capacity;

public sealed class StmCapacityStrategy
    : ICapacityStrategy
{
    private const int MaximumStmCount = 100;

    private readonly CommunicationDbContext _context;

    private readonly IStmInventoryFactory
        _inventoryFactory;

    private readonly ILogger<StmCapacityStrategy>
        _logger;

    public StmCapacityStrategy(
        CommunicationDbContext context,
        IStmInventoryFactory inventoryFactory,
        ILogger<StmCapacityStrategy> logger)
    {
        _context = context;
        _inventoryFactory = inventoryFactory;
        _logger = logger;
    }

    public bool CanHandle(string? capacity)
    {
        return StmCapacityParser.CanHandle(
            capacity);
    }

    public async Task<CapacityProcessingResult> ProcessAsync(
        CapacityProcessingRequest request,
        CancellationToken cancellationToken = default)
    {
        int? extractedCount =
            StmCapacityParser.ExtractCount(
                request.Capacity);

        if (!extractedCount.HasValue ||
            extractedCount.Value is < 1 or > MaximumStmCount)
        {
            return CapacityProcessingResult.Skip(
                $"Capacity '{request.Capacity}' does not contain " +
                $"a valid STM count between 1 and {MaximumStmCount}.");
        }

        await using var transaction =
            await _context.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        try
        {
            bool alreadyConfigured =
                await InventoryAlreadyExistsAsync(
                    request,
                    cancellationToken);

            if (alreadyConfigured)
            {
                return CapacityProcessingResult.Skip(
                    $"Link '{request.LinkName}' already contains STM records.");
            }

            StmInventoryBatch inventory =
                _inventoryFactory.Create(
                    request.PrimaryLinkId,
                    request.ReverseLinkId,
                    extractedCount.Value);

            /*
             * First save:
             * reciprocal IDs remain null to avoid
             * circular foreign-key insertion errors.
             */
            _context.Stms.AddRange(
                inventory.Stms);

            _context.E1s.AddRange(
                inventory.E1s);

            await _context.SaveChangesAsync(
                cancellationToken);

            /*
             * Second save:
             * connect the reciprocal STM and E1 records
             * after every record exists.
             */
            inventory.ConnectReciprocalRecords();

            await _context.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return CapacityProcessingResult.Success(
                inventory.StmCount,
                inventory.E1Count,
                $"Created {inventory.StmCount} STM records " +
                $"and {inventory.E1Count} E1 records " +
                $"for '{request.LinkName}'.");
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            throw;
        }
        catch (Exception exception)
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

            _logger.LogError(
                exception,
                "STM capacity strategy failed for link {LinkId} " +
                "with capacity {Capacity}.",
                request.PrimaryLinkId,
                request.Capacity);

            return CapacityProcessingResult.Failure(
                $"STM/E1 generation failed for " +
                $"'{request.LinkName}'.");
        }
        finally
        {
            /*
             * Thousands of generated objects may otherwise
             * remain tracked while processing the next link.
             */
            _context.ChangeTracker.Clear();
        }
    }

    private Task<bool> InventoryAlreadyExistsAsync(
        CapacityProcessingRequest request,
        CancellationToken cancellationToken)
    {
        return _context.Stms
            .AsNoTracking()
            .AnyAsync(
                stm =>
                    stm.LinkId ==
                        request.PrimaryLinkId ||
                    stm.LinkId ==
                        request.ReverseLinkId,
                cancellationToken);
    }
}