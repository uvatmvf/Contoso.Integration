using Azure.Messaging.ServiceBus;
using Contoso.InventoryFunctions.Contracts;
using Contoso.InventoryFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Contoso.InventoryFunctions.Functions;

public sealed class ReserveInventory
{
    private readonly IInventoryService _inventoryService;
    private readonly IOrderProcessingStore _orderProcessingStore;
    private readonly IOrderEventPublisher _eventPublisher;
    private readonly ILogger<ReserveInventory> _logger;

    public ReserveInventory(
        IInventoryService inventoryService,
        IOrderProcessingStore orderProcessingStore,
        IOrderEventPublisher eventPublisher,
        ILogger<ReserveInventory> logger)
    {
        _inventoryService = inventoryService;
        _orderProcessingStore = orderProcessingStore;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    [Function(nameof(ReserveInventory))]
    public async Task RunAsync(
        [ServiceBusTrigger(
        "reserve-inventory",
        Connection = "ServiceBusConnection")]
    ServiceBusReceivedMessage message,
        CancellationToken cancellationToken)
    {
        var request = message.Body.ToObjectFromJson<ReserveInventoryRequest>()
            ?? throw new InvalidOperationException(
                "ReserveInventoryRequest was null.");

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["OrderId"] = request.OrderId,
            ["CorrelationId"] = message.CorrelationId,
            ["MessageId"] = message.MessageId,
            ["OperationId"] = request.OperationId,
            ["DeliveryCount"] = message.DeliveryCount
        }))
        {
            _logger.LogInformation(
                "Received reserve inventory command.");

            await _inventoryService.ReserveAsync(
                request,
                cancellationToken);

            var inventoryReserved = new InventoryReservedEvent(
                request.OrderId,
                request.OperationId,
                request.ProductId,
                request.Quantity,
                DateTimeOffset.UtcNow);

            var order = await _orderProcessingStore.GetAsync(
                request.OrderId,
                cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Order {request.OrderId} was not found.");

            await _orderProcessingStore.MarkInventoryReservedAsync(
                order,
                cancellationToken);

            await _eventPublisher.PublishInventoryReservedAsync(
                inventoryReserved,
                cancellationToken);

            _logger.LogInformation(
                "Published InventoryReserved event.");
        }
    }
}