using Contoso.InventoryFunctions.Contracts;
using Microsoft.Extensions.Logging;

namespace Contoso.InventoryFunctions.Services;

public sealed class SimulatedPaymentService : IPaymentService
{
    private readonly ILogger<SimulatedPaymentService> _logger;

    public SimulatedPaymentService(
        ILogger<SimulatedPaymentService> logger)
    {
        _logger = logger;
    }

    public Task<AuthorizePaymentResponse> AuthorizeAsync(
        AuthorizePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            "Authorizing payment for order {OrderId}. PaymentMethodId: {PaymentMethodId}, Amount: {Amount}",
            request.OrderId,
            request.PaymentMethodId,
            request.Amount);

        return request.PaymentMethodId.ToUpperInvariant() switch
        {
            "PAY-APPROVED" => Task.FromResult(
                new AuthorizePaymentResponse(
                    Success: true,
                    AuthorizationId: $"AUTH-{Guid.NewGuid():N}",
                    OrderId: request.OrderId,
                    Amount: request.Amount,
                    ErrorCode: null,
                    Message: null)),

            "PAY-DECLINED" => Task.FromResult(
                new AuthorizePaymentResponse(
                    Success: false,
                    AuthorizationId: null,
                    OrderId: request.OrderId,
                    Amount: request.Amount,
                    ErrorCode: "PAYMENT_DECLINED",
                    Message: "The payment provider declined the transaction.")),

            "PAY-ERROR" => throw new InvalidOperationException(
                "Simulated payment provider failure."),

            _ => Task.FromResult(
                new AuthorizePaymentResponse(
                    Success: false,
                    AuthorizationId: null,
                    OrderId: request.OrderId,
                    Amount: request.Amount,
                    ErrorCode: "INVALID_PAYMENT_METHOD",
                    Message: "The supplied payment method is not recognized."))
        };
    }
}