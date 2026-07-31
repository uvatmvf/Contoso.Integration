namespace Contoso.InventoryFunctions.Contracts;

public sealed record PaymentAuthorizedEvent(
    string OrderId,
    string OperationId,
    decimal Amount,
    string Currency,
    DateTimeOffset AuthorizedAt) : IOrderEvent;
