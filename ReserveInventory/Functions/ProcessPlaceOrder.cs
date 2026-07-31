using Azure.Messaging.ServiceBus;
using Contoso.InventoryFunctions.Contracts;
using Contoso.InventoryFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Contoso.InventoryFunctions.Functions;

public sealed class ProcessPlaceOrder
{
    private readonly IInventoryCommandPublisher _inventoryCommandPublisher;    
    private readonly ILogger<ProcessPlaceOrder> _logger;
    private readonly IOrderProcessingStore _orderStore;

    public ProcessPlaceOrder(
        IInventoryCommandPublisher inventoryService,
        ILogger<ProcessPlaceOrder> logger,
        IOrderProcessingStore orderStore)
    {
        _inventoryCommandPublisher = inventoryService;        
        _logger = logger;
        _orderStore = orderStore;
    }


    [Function(nameof(ProcessPlaceOrder))]
    public async Task RunAsync(
        [ServiceBusTrigger(
            "place-order",
            Connection = "ServiceBusConnection")]
        ServiceBusReceivedMessage message,
        CancellationToken cancellationToken)
    {
        var command = message.Body.ToObjectFromJson<PlaceOrderCommand>(
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            })
            ?? throw new InvalidOperationException(
                "The place-order message could not be deserialized.");

        Validate(command);

        var correlationId =
            string.IsNullOrWhiteSpace(message.CorrelationId)
                ? command.OrderId
                : message.CorrelationId;

        using var logScope = _logger.BeginScope(
            new Dictionary<string, object>
            {
                ["OrderId"] = command.OrderId,
                ["CustomerId"] = command.CustomerId,
                ["ProductId"] = command.ProductId,
                ["CorrelationId"] = correlationId,
                ["MessageId"] = message.MessageId,
                ["Subject"] = message.Subject ?? "PlaceOrder",
                ["DeliveryCount"] = message.DeliveryCount
            });

        _logger.LogInformation(
            "Received place-order command.");

        var state = await _orderStore.GetOrCreateAsync(
            command,
            cancellationToken);

        if (state.Entity.OrderStatus == "Completed")
        {
            _logger.LogInformation(
                "Order was already completed. Skipping.");

            return;
        }

        if (state.Entity.InventoryStatus == "Completed")
        {
            _logger.LogInformation(
                "Inventory was already reserved. Skipping command publication.");

            return;
        }

        if (!state.Created)
        {
            _logger.LogInformation(
                "Order already exists. Skipping duplicate PlaceOrder publication.");

            return;
        }

        var operationId =
            $"{command.OrderId}:reserve-inventory";

        await _inventoryCommandPublisher.PublishReserveInventoryAsync(
            new ReserveInventoryRequest(
                command.OrderId,
                operationId,
                command.ProductId,
                command.Quantity),
            cancellationToken);

        _logger.LogInformation(
            "Published reserve-inventory command with operation {OperationId}.",
            operationId);
    }

    private static void Validate(PlaceOrderCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.OrderId))
        {
            throw new InvalidOperationException(
                "OrderId is required.");
        }

        if (string.IsNullOrWhiteSpace(command.CustomerId))
        {
            throw new InvalidOperationException(
                "CustomerId is required.");
        }

        if (string.IsNullOrWhiteSpace(command.ProductId))
        {
            throw new InvalidOperationException(
                "ProductId is required.");
        }

        if (command.Quantity <= 0)
        {
            throw new InvalidOperationException(
                "Quantity must be greater than zero.");
        }

        if (command.Amount < 0)
        {
            throw new InvalidOperationException(
                "Amount cannot be negative.");
        }
    }
}