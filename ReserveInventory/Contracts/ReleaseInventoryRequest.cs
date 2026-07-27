using System;
using System.Collections.Generic;
using System.Text;

namespace Contoso.InventoryFunctions.Contracts
{
    public sealed record ReleaseInventoryRequest(
        string OrderId,
        string ReservationId);
}
