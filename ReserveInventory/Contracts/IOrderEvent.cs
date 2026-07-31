namespace Contoso.InventoryFunctions.Contracts;

public interface IOrderEvent
{
    string OrderId { get; }
    string OperationId { get; }
}
