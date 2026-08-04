using System;
using System.Threading;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Moq;
using NUnit.Framework;
using Contoso.InventoryFunctions.Functions;
using Contoso.InventoryFunctions.Services;
using Contoso.InventoryFunctions.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Azure;

namespace ReserveInventory.NUnitTest
{
    [TestFixture]
    public class CompleteOrderTests
    {
        private ServiceBusReceivedMessage CreateMessage(PaymentAuthorizedEvent ev)
        {
            var body = BinaryData.FromObjectAsJson(ev);
            return ServiceBusModelFactory.ServiceBusReceivedMessage(body);
        }

        [Test]
        [TestCase(InventoryStatuses.NotStarted, PaymentStatuses.Completed)]
        [TestCase(InventoryStatuses.NotStarted, PaymentStatuses.NotStarted)]
        public void Inventory_NotReserved_TableDriven_Throws(string inventoryStatus, string paymentStatus)
        {
            var order = new OrderProcessingEntity
            {
                RowKey = "ORD-T1",
                OrderStatus = OrderStatuses.Processing,
                InventoryStatus = inventoryStatus,
                PaymentStatus = paymentStatus
            };

            var storeMock = new Mock<IOrderProcessingStore>();
            storeMock.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var func = new CompleteOrder(storeMock.Object, new NullLogger<CompleteOrder>());

            var ev = new PaymentAuthorizedEvent(order.RowKey, "op-t1", 1m, "USD", DateTimeOffset.UtcNow);
            var msg = CreateMessage(ev);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await func.RunAsync(msg, CancellationToken.None));
        }

        [Test]
        public async Task OrderAlreadyCompleted_DoesNotCallMarkOrderCompleted()
        {
            var order = new OrderProcessingEntity
            {
                RowKey = "ORD-1",
                OrderStatus = OrderStatuses.Completed,
                InventoryStatus = InventoryStatuses.Reserved,
                PaymentStatus = PaymentStatuses.Completed
            };

            var storeMock = new Mock<IOrderProcessingStore>();
            storeMock.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var logger = new NullLogger<CompleteOrder>();

            var func = new CompleteOrder(storeMock.Object, logger);

            var ev = new PaymentAuthorizedEvent(order.RowKey, "op-1", 1m, "USD", DateTimeOffset.UtcNow);
            var msg = CreateMessage(ev);

            await func.RunAsync(msg, CancellationToken.None);

            storeMock.Verify(s => s.MarkOrderCompletedAsync(It.IsAny<OrderProcessingEntity>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        [TestCase(PaymentStatuses.NotStarted)]
        [TestCase(PaymentStatuses.Failed)]
        public void Payment_NotCompleted_TableDriven_Throws(string paymentStatus)
        {
            var order = new OrderProcessingEntity
            {
                RowKey = "ORD-T2",
                OrderStatus = OrderStatuses.Processing,
                InventoryStatus = InventoryStatuses.Reserved,
                PaymentStatus = paymentStatus
            };

            var storeMock = new Mock<IOrderProcessingStore>();
            storeMock.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var func = new CompleteOrder(storeMock.Object, new NullLogger<CompleteOrder>());

            var ev = new PaymentAuthorizedEvent(order.RowKey, "op-t2", 1m, "USD", DateTimeOffset.UtcNow);
            var msg = CreateMessage(ev);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await func.RunAsync(msg, CancellationToken.None));
        }

        [Test]
        [TestCase("reserved", "completed")]
        [TestCase("RESERVED", "COMPLETED")]
        public async Task CaseInsensitive_Statuses_AreAccepted(string invStatus, string payStatus)
        {
            var order = new OrderProcessingEntity
            {
                RowKey = "ORD-T3",
                OrderStatus = OrderStatuses.Processing,
                InventoryStatus = invStatus,
                PaymentStatus = payStatus
            };

            var storeMock = new Mock<IOrderProcessingStore>();
            storeMock.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var func = new CompleteOrder(storeMock.Object, new NullLogger<CompleteOrder>());

            var ev = new PaymentAuthorizedEvent(order.RowKey, "op-t3", 1m, "USD", DateTimeOffset.UtcNow);
            var msg = CreateMessage(ev);

            await func.RunAsync(msg, CancellationToken.None);

            storeMock.Verify(s => s.MarkOrderCompletedAsync(It.Is<OrderProcessingEntity>(o => o.RowKey == order.RowKey), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void InventoryNotReserved_Throws()
        {
            var order = new OrderProcessingEntity
            {
                RowKey = "ORD-2",
                OrderStatus = OrderStatuses.Processing,
                InventoryStatus = InventoryStatuses.NotStarted,
                PaymentStatus = PaymentStatuses.Completed
            };

            var storeMock = new Mock<IOrderProcessingStore>();
            storeMock.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var func = new CompleteOrder(storeMock.Object, new NullLogger<CompleteOrder>());

            var ev = new PaymentAuthorizedEvent(order.RowKey, "op-2", 1m, "USD", DateTimeOffset.UtcNow);
            var msg = CreateMessage(ev);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await func.RunAsync(msg, CancellationToken.None));
        }

        [Test]
        public void PaymentNotCompleted_Throws()
        {
            var order = new OrderProcessingEntity
            {
                RowKey = "ORD-3",
                OrderStatus = OrderStatuses.Processing,
                InventoryStatus = InventoryStatuses.Reserved,
                PaymentStatus = PaymentStatuses.NotStarted
            };

            var storeMock = new Mock<IOrderProcessingStore>();
            storeMock.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var func = new CompleteOrder(storeMock.Object, new NullLogger<CompleteOrder>());

            var ev = new PaymentAuthorizedEvent(order.RowKey, "op-3", 1m, "USD", DateTimeOffset.UtcNow);
            var msg = CreateMessage(ev);

            Assert.ThrowsAsync<InvalidOperationException>(async () => await func.RunAsync(msg, CancellationToken.None));
        }

        [Test]
        public async Task Success_CallsMarkOrderCompleted()
        {
            var order = new OrderProcessingEntity
            {
                RowKey = "ORD-4",
                OrderStatus = OrderStatuses.Processing,
                InventoryStatus = InventoryStatuses.Reserved,
                PaymentStatus = PaymentStatuses.Completed
            };

            var storeMock = new Mock<IOrderProcessingStore>();
            storeMock.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var func = new CompleteOrder(storeMock.Object, new NullLogger<CompleteOrder>());

            var ev = new PaymentAuthorizedEvent(order.RowKey, "op-4", 1m, "USD", DateTimeOffset.UtcNow);
            var msg = CreateMessage(ev);

            await func.RunAsync(msg, CancellationToken.None);

            storeMock.Verify(s => s.MarkOrderCompletedAsync(It.Is<OrderProcessingEntity>(o => o.RowKey == order.RowKey), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
