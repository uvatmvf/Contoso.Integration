using Azure.Data.Tables;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Monitor.OpenTelemetry.Exporter;
using Contoso.InventoryFunctions.Services;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.OpenTelemetry;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services.AddSingleton<IInventoryService, InMemoryInventoryService>();
builder.Services.AddSingleton<IPaymentService, SimulatedPaymentService>();
builder.Services.AddSingleton(sp =>
{
    var configuration =
        sp.GetRequiredService<IConfiguration>();

    var fullyQualifiedNamespace =
        configuration["ServiceBusConnection:fullyQualifiedNamespace"];

    return new ServiceBusClient(
        fullyQualifiedNamespace,
        new DefaultAzureCredential());
});

builder.Services.AddSingleton<
    IInventoryCommandPublisher,
    ServiceBusInventoryCommandPublisher>();

builder.Services.AddSingleton(sp =>
{
    var configuration =
        sp.GetRequiredService<IConfiguration>();

    var tableEndpoint =
        configuration["OrderStateStorage:tableEndpoint"];

    if (string.IsNullOrWhiteSpace(tableEndpoint))
    {
        throw new InvalidOperationException(
            "OrderStateStorage:tableEndpoint is not configured.");
    }

    var tableClient = new TableClient(
        new Uri(tableEndpoint),
        "OrderProcessing",
        new DefaultAzureCredential());

    tableClient.CreateIfNotExists();

    return tableClient;
});

builder.Services.AddSingleton<
    IOrderProcessingStore,
    TableOrderProcessingStore>();

builder.Services.AddSingleton<IOrderEventPublisher, ServiceBusOrderEventPublisher>();

builder.ConfigureFunctionsWebApplication();

if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING")))
{
    builder.Services.AddOpenTelemetry()
        .UseFunctionsWorkerDefaults()
        .UseAzureMonitorExporter();
}

builder.Build().Run();
