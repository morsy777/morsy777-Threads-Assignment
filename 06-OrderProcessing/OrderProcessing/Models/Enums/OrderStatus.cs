namespace OrderProcessing.Models.Enums;

public enum OrderStatus
{
    Pending,
    Validated,
    Rejected,
    PaymentFailed,
    Completed,
    Failed,
    Cancelled
}