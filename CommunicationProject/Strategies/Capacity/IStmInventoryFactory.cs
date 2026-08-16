namespace CommunicationProject.Strategies.Capacity;

public interface IStmInventoryFactory
{
    StmInventoryBatch Create(
        Guid primaryLinkId,
        Guid reverseLinkId,
        int stmCount);
}