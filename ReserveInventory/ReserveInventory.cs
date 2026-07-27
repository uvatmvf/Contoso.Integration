using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ReserveInventory;

public class ReserveInventory
{
    private readonly ILogger<ReserveInventory> _logger;

    public ReserveInventory(ILogger<ReserveInventory> logger)
    {
        _logger = logger;
    }

    [Function("ReserveInventory")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");
        return new OkObjectResult("Welcome to Azure Functions!");
    }
}