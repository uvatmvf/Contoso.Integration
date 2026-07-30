using Contoso.InventoryFunctions.Contracts;

namespace Contoso.InventoryFunctions.Services;

internal interface IPaymentEventPublisher
{
    Task PublishPaymentAsync(
        PaymentAuthorizedEvent inventoryReserved,
        CancellationToken cancellationToken);
}