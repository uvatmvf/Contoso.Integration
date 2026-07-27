using System;
using System.Collections.Generic;
using System.Text;

namespace Contoso.InventoryFunctions.Contracts
{
    public sealed record AuthorizePaymentRequest(
        string OrderId,
        string PaymentMethodId,
        decimal Amount);
}
