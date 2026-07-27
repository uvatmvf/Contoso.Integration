using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;
using Contoso.InventoryFunctions;

namespace ReserveInventory.NUnitTest;

[TestFixture]
public class ReserveInventoryTests
{
    private static HttpRequest CreateHttpRequest(string json)
    {
        var context = new DefaultHttpContext();
        var request = context.Request;
        var bytes = Encoding.UTF8.GetBytes(json ?? string.Empty);
        request.Body = new MemoryStream(bytes);
        request.ContentLength = bytes.Length;
        request.ContentType = "application/json";
        return request;
    }

    [Test]
    public async Task Run_Returns_Ok_For_Valid_Request()
    {
        var payload = "{\"OrderId\":\"ORD-1\",\"ProductId\":\"PROD-1\",\"Quantity\":5}";
        var request = CreateHttpRequest(payload);
        var function = new Contoso.InventoryFunctions.ReserveInventory(new NullLogger<Contoso.InventoryFunctions.ReserveInventory>());

        var result = await function.Run(request, CancellationToken.None);

        Assert.That(result, Is.TypeOf<OkObjectResult>());
        var ok = (OkObjectResult)result;
        var response = (ReserveInventoryResponse)ok.Value!;
        Assert.That(response.Success, Is.True);
        Assert.That(response.OrderId, Is.EqualTo("ORD-1"));
        Assert.That(response.ProductId, Is.EqualTo("PROD-1"));
        Assert.That(response.QuantityReserved, Is.EqualTo(5));
        Assert.That(response.RemainingInventory, Is.EqualTo(15));
    }

    [Test]
    public async Task Run_Returns_BadRequest_For_Invalid_Json()
    {
        var payload = "{ bad json }";
        var request = CreateHttpRequest(payload);
        var function = new Contoso.InventoryFunctions.ReserveInventory(new NullLogger<Contoso.InventoryFunctions.ReserveInventory>());

        var result = await function.Run(request, CancellationToken.None);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var bad = (BadRequestObjectResult)result;
        var error = (ErrorResponse)bad.Value!;
        Assert.That(error.ErrorCode, Is.EqualTo("INVALID_REQUEST"));
    }

    [Test]
    public async Task Run_Returns_BadRequest_For_Missing_Fields()
    {
        var payload = "{\"OrderId\":\"\",\"ProductId\":\"\",\"Quantity\":0}";
        var request = CreateHttpRequest(payload);
        var function = new Contoso.InventoryFunctions.ReserveInventory(new NullLogger<Contoso.InventoryFunctions.ReserveInventory>());

        var result = await function.Run(request, CancellationToken.None);

        Assert.That(result, Is.TypeOf<BadRequestObjectResult>());
        var bad = (BadRequestObjectResult)result;
        var error = (ErrorResponse)bad.Value!;
        Assert.That(error.ErrorCode, Is.EqualTo("INVALID_REQUEST"));
    }

    [Test]
    public async Task Run_Returns_Conflict_When_Not_Enough_Inventory()
    {
        var payload = "{\"OrderId\":\"ORD-2\",\"ProductId\":\"PROD-2\",\"Quantity\":999}";
        var request = CreateHttpRequest(payload);
        var function = new Contoso.InventoryFunctions.ReserveInventory(new NullLogger<Contoso.InventoryFunctions.ReserveInventory>());

        var result = await function.Run(request, CancellationToken.None);

        Assert.That(result, Is.TypeOf<ConflictObjectResult>());
        var conflict = (ConflictObjectResult)result;
        var error = (ErrorResponse)conflict.Value!;
        Assert.That(error.ErrorCode, Is.EqualTo("OUT_OF_STOCK"));
    }
}
