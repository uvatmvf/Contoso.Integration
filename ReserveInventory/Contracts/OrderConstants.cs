namespace Contoso.InventoryFunctions.Contracts;

public static class InventoryStatuses
{
    public const string NotStarted = "NotStarted";
    public const string Reserved = "Reserved";
}

public static class PaymentStatuses
{
    public const string NotStarted = "NotStarted";
    public const string Completed = "Completed";
    public const string Failed = "Failed";
}

public static class OrderStatuses
{
    public const string Pending = "Pending";
    public const string Processing = "Processing";
    public const string Completed = "Completed";
}