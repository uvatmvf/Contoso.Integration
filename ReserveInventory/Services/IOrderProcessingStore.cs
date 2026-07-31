using Contoso.InventoryFunctions.Contracts;
namespace Contoso.InventoryFunctions.Services;

public interface IOrderProcessingStore
{
    Task<GetOrCreateOrderResult> GetOrCreateAsync(
        PlaceOrderCommand command,
        CancellationToken cancellationToken);

    Task<OrderProcessingEntity?> GetAsync(string orderId
        , CancellationToken cancellationToken);

    Task MarkInventoryReservedAsync(
        OrderProcessingEntity entity,
        CancellationToken cancellationToken);

    Task MarkOrderCompletedAsync(
        OrderProcessingEntity entity,
        CancellationToken cancellationToken);

    Task MarkPaymentAuthorizedAsync(
        OrderProcessingEntity entity,
        CancellationToken cancellationToken);

    Task MarkPaymentFailedAsync(
        OrderProcessingEntity entity,
        string error,
        CancellationToken cancellationToken);
}