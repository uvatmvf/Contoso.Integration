namespace Contoso.InventoryFunctions.Contracts;

public sealed record AuthorizePaymentRequest(
    string OrderId,
    string OperationId,
    string PaymentMethodId,
    decimal Amount,
    string Currency);
