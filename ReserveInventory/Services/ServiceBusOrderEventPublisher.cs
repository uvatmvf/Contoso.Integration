using Azure.Messaging.ServiceBus;
using Contoso.InventoryFunctions.Contracts;

namespace Contoso.InventoryFunctions.Services;
public sealed class ServiceBusOrderEventPublisher : IOrderEventPublisher
{
    private readonly ServiceBusSender _sender;

    public ServiceBusOrderEventPublisher(ServiceBusClient client)
    {
        _sender = client.CreateSender("order-events");
    }

    public async Task PublishInventoryReservedAsync(
        InventoryReservedEvent inventoryReserved,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(inventoryReserved);

        if (string.IsNullOrWhiteSpace(inventoryReserved.OperationId))
        {
            throw new ArgumentException(
                "OperationId is required.",
                nameof(inventoryReserved));
        }

        var message = new ServiceBusMessage(
            BinaryData.FromObjectAsJson(inventoryReserved))
        {
            MessageId = $"{inventoryReserved.OperationId}:completed",
            CorrelationId = inventoryReserved.OrderId,
            Subject = "InventoryReserved",
            ContentType = "application/json"
        };

        message.ApplicationProperties["EventType"] = "InventoryReserved";
        message.ApplicationProperties["EventVersion"] = 1;

        await _sender.SendMessageAsync(message, cancellationToken);
    }
}