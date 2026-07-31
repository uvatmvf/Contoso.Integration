using Azure.Messaging.ServiceBus;
using Contoso.InventoryFunctions.Contracts;
using Microsoft.Extensions.Logging;

namespace Contoso.InventoryFunctions.Services;
public sealed class ServiceBusOrderEventPublisher : IOrderEventPublisher
{
    private readonly ServiceBusSender _sender;
    private readonly ILogger _logger;

    public ServiceBusOrderEventPublisher(ServiceBusClient client
        , ILogger logger)
    {
        _sender = client.CreateSender("order-events");
        _logger = logger;
    }

    public async Task PublishInventoryReservedAsync(
        InventoryReservedEvent inventoryReserved,
        CancellationToken cancellationToken) => 
        await PublishEvent(
            inventoryReserved,
            "InventoryReserved",
            cancellationToken);

    public async Task PublishPaymentAuthorizedAsync(
    PaymentAuthorizedEvent paymentAuthorized,
    CancellationToken cancellationToken) =>
        await PublishEvent(
            paymentAuthorized,
            "PaymentAuthorized",
            cancellationToken);

    private async Task PublishEvent<T>(
        T eventToPublish,
        string subject,
        CancellationToken cancellationToken) where T : IOrderEvent
    {
        ArgumentNullException.ThrowIfNull(eventToPublish);

        if (string.IsNullOrWhiteSpace(eventToPublish.OperationId))
        {
            throw new ArgumentException(
                "OperationId is required.",
                nameof(eventToPublish));
        }
        var message = new ServiceBusMessage(
            BinaryData.FromObjectAsJson(eventToPublish))
        {
            MessageId = $"{eventToPublish.OperationId}:completed",
            CorrelationId = eventToPublish.OrderId,
            Subject =subject,
            ContentType = "application/json"
        };
        
        _logger.LogInformation(
            "Published event {Subject}. MessageId: {MessageId}, CorrelationId: {CorrelationId}",
            subject,
            message.MessageId,
            message.CorrelationId);

        message.ApplicationProperties["EventType"] = subject;
        message.ApplicationProperties["EventVersion"] = 1;

        await _sender.SendMessageAsync(message, cancellationToken);
    }
}