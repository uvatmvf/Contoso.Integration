namespace Contoso.InventoryFunctions.Contracts;

public sealed record ReserveInventoryResponse(
    bool Success,
    string? ReservationId,
    string OrderId,
    string ProductId,
    int QuantityReserved,
    int RemainingInventory,
    string? ErrorCode = null,
    string? Message = null);