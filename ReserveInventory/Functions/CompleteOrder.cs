using Azure.Messaging.ServiceBus;
using Contoso.InventoryFunctions.Contracts;
using Contoso.InventoryFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Contoso.InventoryFunctions.Functions;

public sealed class CompleteOrder
{
    private readonly IOrderProcessingStore _orderStore;
    private readonly ILogger<CompleteOrder> _logger;

    public CompleteOrder(
        IOrderProcessingStore orderStore,
        ILogger<CompleteOrder> logger)
    {
        _orderStore = orderStore;
        _logger = logger;
    }

    [Function(nameof(CompleteOrder))]
    public async Task RunAsync(
        [ServiceBusTrigger(
            topicName: "order-events",
            subscriptionName: "complete-order",
            Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        CancellationToken cancellationToken)
    {
        var paymentAuthorized =
            message.Body.ToObjectFromJson<PaymentAuthorizedEvent>()
            ?? throw new InvalidOperationException(
                "PaymentAuthorized event body was empty.");

        var operationId =
            $"{paymentAuthorized.OrderId}:complete-order";

        using var logScope = _logger.BeginScope(
            new Dictionary<string, object>
            {
                ["OrderId"] = paymentAuthorized.OrderId,
                ["CorrelationId"] =
                    message.CorrelationId ?? paymentAuthorized.OrderId,
                ["MessageId"] = message.MessageId,
                ["OperationId"] = operationId,
                ["PaymentOperationId"] =
                    paymentAuthorized.OperationId,
                ["DeliveryCount"] = message.DeliveryCount
            });

        _logger.LogInformation(
            "Received {Subject} event.",
            message.Subject);

        var order = await _orderStore.GetAsync(
            paymentAuthorized.OrderId,
            cancellationToken)
            ?? throw new InvalidOperationException(
                $"Order {paymentAuthorized.OrderId} was not found.");

        _logger.LogInformation(
            "Loaded order state before completion: InventoryStatus={InventoryStatus}, PaymentStatus={PaymentStatus}, ETag={ETag}.",
            order.InventoryStatus,
            order.PaymentStatus,
            order.ETag);

        if (order.OrderStatus == "Completed")
        {
            _logger.LogInformation(
                "Order was already completed. Skipping.");

            return;
        }

        if (order.InventoryStatus != "Completed")
        {
            throw new InvalidOperationException(
                $"Order {paymentAuthorized.OrderId} cannot be completed because inventory status is {order.InventoryStatus}.");
        }

        if (order.PaymentStatus != "Completed")
        {
            throw new InvalidOperationException(
                $"Order {paymentAuthorized.OrderId} cannot be completed because payment status is {order.PaymentStatus}.");
        }

        await _orderStore.MarkOrderCompletedAsync(
            order,
            cancellationToken);

        _logger.LogInformation(
            "Order completed successfully.");
    }
}