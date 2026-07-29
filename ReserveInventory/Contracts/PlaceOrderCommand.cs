namespace Contoso.InventoryFunctions.Contracts
{
    public sealed record PlaceOrderCommand(
    string OrderId,
    string OperationId,
    string CustomerId,
    string ProductId,
    int Quantity,
    string PaymentMethodId,
    decimal Amount);
}
