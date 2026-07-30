using System.Threading.Tasks;
using System.Threading;
using NUnit.Framework;
using Contoso.InventoryFunctions.Services;
using Contoso.InventoryFunctions.Contracts;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace ReserveInventory.NUnitTest;

[TestFixture]
public class ServiceTests
{
    [Test]
    public async Task InMemoryInventory_Reserve_Succeeds()
    {
        var svc = new InMemoryInventoryService(new NullLogger<InMemoryInventoryService>());

        var req = new ReserveInventoryRequest("ORD-1", "OP-1", "PROD-1", 2);
        var resp = await svc.ReserveAsync(req, CancellationToken.None);

        Assert.That(resp.Success, Is.True);
        Assert.That(resp.QuantityReserved, Is.EqualTo(2));
        Assert.That(resp.RemainingInventory, Is.EqualTo(18));
        Assert.That(resp.ReservationId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task InMemoryInventory_Reserve_OutOfStock()
    {
        var svc = new InMemoryInventoryService(new NullLogger<InMemoryInventoryService>());

        var req = new ReserveInventoryRequest("ORD-2", "OP-2", "PROD-1", 100);
        var resp = await svc.ReserveAsync(req, CancellationToken.None);

        Assert.That(resp.Success, Is.False);
        Assert.That(resp.ErrorCode, Is.EqualTo("OUT_OF_STOCK"));
    }

    [Test]
    public async Task InMemoryInventory_Release_Succeeds()
    {
        var svc = new InMemoryInventoryService(new NullLogger<InMemoryInventoryService>());

        var reserve = await svc.ReserveAsync(new ReserveInventoryRequest("ORD-3", "OP-3", "PROD-1", 3), CancellationToken.None);
        Assert.That(reserve.Success, Is.True);

        var release = await svc.ReleaseAsync(new ReleaseInventoryRequest("ORD-3", reserve.ReservationId!), CancellationToken.None);
        Assert.That(release.Success, Is.True);
    }

    [Test]
    public async Task InMemoryInventory_Release_NotFound()
    {
        var svc = new InMemoryInventoryService(new NullLogger<InMemoryInventoryService>());

        var release = await svc.ReleaseAsync(new ReleaseInventoryRequest("ORD-4", "RES-UNKNOWN"), CancellationToken.None);
        Assert.That(release.Success, Is.False);
        Assert.That(release.ErrorCode, Is.EqualTo("RESERVATION_NOT_FOUND"));
    }

    [Test]
    public async Task SimulatedPayment_Approved()
    {
        var svc = new SimulatedPaymentService(new NullLogger<SimulatedPaymentService>());

        var resp = await svc.AuthorizeAsync(new AuthorizePaymentRequest("ORD-10", "OP-10", "PAY-APPROVED", 10m, "USD"), CancellationToken.None);
        Assert.That(resp.Success, Is.True);
        Assert.That(resp.AuthorizationId, Is.Not.Null.And.Not.Empty);
    }

    [Test]
    public async Task SimulatedPayment_Declined()
    {
        var svc = new SimulatedPaymentService(new NullLogger<SimulatedPaymentService>());

        var resp = await svc.AuthorizeAsync(new AuthorizePaymentRequest("ORD-11", "OP-11", "PAY-DECLINED", 5m, "USD"), CancellationToken.None);
        Assert.That(resp.Success, Is.False);
        Assert.That(resp.ErrorCode, Is.EqualTo("PAYMENT_DECLINED"));
    }

    [Test]
    public void SimulatedPayment_Error_Throws()
    {
        var svc = new SimulatedPaymentService(new NullLogger<SimulatedPaymentService>());

        Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await svc.AuthorizeAsync(new AuthorizePaymentRequest("ORD-12", "OP-12", "PAY-ERROR", 0m, "USD"), CancellationToken.None));
    }
}
