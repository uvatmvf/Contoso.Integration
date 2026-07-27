using Contoso.InventoryFunctions.Contracts;

namespace Contoso.InventoryFunctions.Services;

public interface IPaymentService
{
    Task<AuthorizePaymentResponse> AuthorizeAsync(
        AuthorizePaymentRequest request,
        CancellationToken cancellationToken = default);
}