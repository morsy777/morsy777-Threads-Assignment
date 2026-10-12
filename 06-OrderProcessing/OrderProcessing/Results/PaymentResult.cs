using OrderProcessing.Models.Enums;

namespace OrderProcessing.Models.Results;

// I perfer to use Result<T> instead of creating 
// a separate result for each type
public class PaymentResult
{
    public bool Success { get; set; }
    public string? TransactionId { get; set; }
    public PaymentStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;

    public PaymentResult() { }
}