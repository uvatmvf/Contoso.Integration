namespace Contoso.InventoryFunctions.Contracts;

public sealed record ReserveInventoryRequest(
    string OrderId,
    string OperationId,
    string ProductId,
    int Quantity);