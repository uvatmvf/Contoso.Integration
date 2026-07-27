namespace Contoso.InventoryFunctions.Contracts;

public sealed record ReserveInventoryRequest(
    string OrderId,
    string ProductId,
    int Quantity);