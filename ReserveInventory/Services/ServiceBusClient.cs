using Azure.Messaging.ServiceBus;
using Contoso.InventoryFunctions.Contracts;
using Contoso.InventoryFunctions.Services;
using System.Text.Json;

public sealed class ServiceBusInventoryCommandPublisher
    : IInventoryCommandPublisher
{
    private readonly ServiceBusSender _sender;

    public ServiceBusInventoryCommandPublisher(ServiceBusClient client)
    {
        _sender = client.CreateSender("reserve-inventory");
    }

    public async Task PublishReserveInventoryAsync(
        ReserveInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var message = new ServiceBusMessage(
            JsonSerializer.Serialize(request))
        {
            ContentType = "application/json",
            MessageId = request.OperationId,
            Subject = "ReserveInventory"
        };

        await _sender.SendMessageAsync(
            message,
            cancellationToken);
    }
}