using Contoso.InventoryFunctions.Contracts;
using Contoso.InventoryFunctions.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Contoso.InventoryFunctions.Functions;

public sealed class ReserveInventory
{
    private readonly IInventoryService _inventoryService;
    private readonly IOrderEventPublisher _eventPublisher;
    private readonly ILogger<ReserveInventory> _logger;

    public ReserveInventory(
        IInventoryService inventoryService,
        IOrderEventPublisher eventPublisher,
        ILogger<ReserveInventory> logger)
    {
        _inventoryService = inventoryService;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    [Function(nameof(ReserveInventory))]
    public async Task RunAsync(
        [ServiceBusTrigger(
            "reserve-inventory",
            Connection = "ServiceBusConnection")]
        ReserveInventoryRequest request,
        CancellationToken cancellationToken)
    {
        await _inventoryService.ReserveAsync(
            request,
            cancellationToken);

        var inventoryReserved = new InventoryReservedEvent(
            request.OrderId,
            request.OperationId,
            request.ProductId,
            request.Quantity,
            DateTimeOffset.UtcNow);

        await _eventPublisher.PublishInventoryReservedAsync(
            inventoryReserved,
            cancellationToken);

        _logger.LogInformation(
            "Inventory reserved event published for order {OrderId}",
            request.OrderId);
    }
}