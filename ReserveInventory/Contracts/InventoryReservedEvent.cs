namespace Contoso.InventoryFunctions.Contracts;

public sealed record InventoryReservedEvent(
    string OrderId,
    string OperationId,
    string ProductId,
    int Quantity,
    DateTimeOffset OccurredAtUtc) : IOrderEvent;
