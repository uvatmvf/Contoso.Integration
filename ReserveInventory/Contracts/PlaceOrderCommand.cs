namespace Contoso.InventoryFunctions.Contracts
{
    public sealed record PlaceOrderCommand(
    string OrderId,
    string CustomerId,
    string ProductId,
    int Quantity,
    string PaymentMethodId,
    decimal Amount);
}
