using Contoso.InventoryFunctions.Contracts;
using Contoso.InventoryFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Contoso.InventoryFunctions.Functions;

public sealed class AuthorizePayment
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<AuthorizePayment> _logger;

    public AuthorizePayment(
        IPaymentService paymentService,
        ILogger<AuthorizePayment> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [Function(nameof(AuthorizePayment))]
    public async Task RunAsync(
        [ServiceBusTrigger(
        topicName: "order-events",
        subscriptionName: "authorize-payment",
        Connection = "ServiceBusConnection")]
    InventoryReservedEvent inventoryReserved,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inventoryReserved);

        var paymentOperationId =
            $"{inventoryReserved.OrderId}:authorize-payment";

        _logger.LogInformation(
            "Authorizing payment for order {OrderId}. " +
            "Inventory operation {InventoryOperationId}; " +
            "payment operation {PaymentOperationId}.",
            inventoryReserved.OrderId,
            inventoryReserved.OperationId,
            paymentOperationId);

        try
        {
            var request = new AuthorizePaymentRequest(
                inventoryReserved.OrderId,
                paymentOperationId,
                inventoryReserved.Quantity);

            await _paymentService.AuthorizeAsync(
                request,
                cancellationToken);

            _logger.LogInformation(
                "Payment authorized for order {OrderId}",
                inventoryReserved.OrderId);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Payment authorization failed for order {OrderId}",
                inventoryReserved.OrderId);

            throw;
        }
    }
}