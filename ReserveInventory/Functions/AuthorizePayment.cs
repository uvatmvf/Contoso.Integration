using Azure.Messaging.ServiceBus;
using Contoso.InventoryFunctions.Contracts;
using Contoso.InventoryFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Contoso.InventoryFunctions.Functions;

public sealed class AuthorizePayment
{
    private readonly IPaymentService _paymentService;
    private readonly IOrderProcessingStore _orderStore;
    private readonly IOrderEventPublisher _eventPublisher;
    private readonly ILogger<AuthorizePayment> _logger;

    public AuthorizePayment(
        IPaymentService paymentService,
        IOrderProcessingStore orderStore,
        IOrderEventPublisher eventPublisher,
        ILogger<AuthorizePayment> logger)
    {
        _paymentService = paymentService;
        _orderStore = orderStore;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    [Function(nameof(AuthorizePayment))]
    public async Task RunAsync(
    [ServiceBusTrigger(
        topicName: "order-events",
        subscriptionName: "authorize-payment",
        Connection = "ServiceBusConnection")]
    ServiceBusReceivedMessage message,
    CancellationToken cancellationToken)
    {
        var inventoryReserved =
            message.Body.ToObjectFromJson<InventoryReservedEvent>()
            ?? throw new InvalidOperationException(
                "InventoryReserved event body was empty.");

        var paymentOperationId =
            $"{inventoryReserved.OrderId}:authorize-payment";

        using var logScope = _logger.BeginScope(
            new Dictionary<string, object>
            {
                ["OrderId"] = inventoryReserved.OrderId,
                ["CorrelationId"] =
                    message.CorrelationId ?? inventoryReserved.OrderId,
                ["MessageId"] = message.MessageId,
                ["OperationId"] = paymentOperationId,
                ["InventoryOperationId"] =
                    inventoryReserved.OperationId,
                ["DeliveryCount"] = message.DeliveryCount
            });

        _logger.LogInformation(
            "Received {Subject} event.",
            message.Subject);

        var order = await _orderStore.GetAsync(
            inventoryReserved.OrderId,
            cancellationToken)
            ?? throw new InvalidOperationException(
                $"Order {inventoryReserved.OrderId} was not found.");

        if (order.PaymentStatus == "Completed")
        {
            _logger.LogInformation(
                "Payment was already authorized. Skipping.");

            return;
        }

        var paymentRequest = new AuthorizePaymentRequest(
            inventoryReserved.OrderId,
            paymentOperationId,
            order.PaymentMethodId,
            order.Amount,
            order.Currency);

        var result = await _paymentService.AuthorizeAsync(
            paymentRequest,
            cancellationToken);

        if (!result.Success)
        {
            await _orderStore.MarkPaymentFailedAsync(
                order,
                result.ErrorCode ?? "Payment authorization failed.",
                cancellationToken);

            throw new InvalidOperationException(
                $"Payment failed: {result.ErrorCode}");
        }

        await _orderStore.MarkPaymentAuthorizedAsync(
            order,
            cancellationToken);

        var paymentAuthorizedEvent = new PaymentAuthorizedEvent(
            inventoryReserved.OrderId,
            paymentOperationId,
            order.Amount,
            order.Currency,
            DateTimeOffset.UtcNow);

        await _eventPublisher.PublishPaymentAuthorizedAsync(
            paymentAuthorizedEvent,
            cancellationToken);

        _logger.LogInformation(
            "Payment authorization completed for {Amount} {Currency}.",
            order.Amount,
            order.Currency);


    }
}