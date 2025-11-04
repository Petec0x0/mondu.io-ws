namespace SimpleWalletSystem.Services;

public interface IPaymentService
{
    Task<PaymentResponse> InitializePaymentAsync(decimal amount, string currency, string returnUrl);
    Task<PaymentStatus> VerifyPaymentAsync(string transactionId);
}

public class PaymentService : IPaymentService
{
    private readonly ILogger<PaymentService> _logger;
    private readonly Dictionary<string, PaymentStatus> _mockPayments = new();

    public PaymentService(ILogger<PaymentService> logger)
    {
        _logger = logger;
    }

    public async Task<PaymentResponse> InitializePaymentAsync(decimal amount, string currency, string returnUrl)
    {
        // Simulate API call delay
        await Task.Delay(100);

        var transactionId = Guid.NewGuid().ToString();
        
        // Mock MPGS response
        var response = new PaymentResponse
        {
            Success = true,
            TransactionId = transactionId,
            PaymentUrl = $"{returnUrl}?transactionId={transactionId}",
            Message = "Payment initiated successfully"
        };

        // Store mock payment as pending
        _mockPayments[transactionId] = new PaymentStatus
        {
            TransactionId = transactionId,
            Success = true,
            Status = "Pending",
            Amount = amount,
            Currency = currency
        };

        _logger.LogInformation("Payment initiated: {TransactionId}, amount: {Amount}", transactionId, amount);
        return response;
    }

    public async Task<PaymentStatus> VerifyPaymentAsync(string transactionId)
    {
        await Task.Delay(150); // Simulate API call

        if (_mockPayments.TryGetValue(transactionId, out var status))
        {
            // Simulate payment completion
            if (status.Status == "Pending")
            {
                status.Status = "Completed";
                status.Success = true;
                status.Message = "Payment completed successfully";
            }

            _logger.LogInformation("Payment verified: {TransactionId}, status: {Status}", transactionId, status.Status);
            return status;
        }

        return new PaymentStatus
        {
            TransactionId = transactionId,
            Success = false,
            Status = "Failed",
            Message = "Transaction not found"
        };
    }
}

public class PaymentResponse
{
    public bool Success { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class PaymentStatus
{
    public string TransactionId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, Completed, Failed
    public string Message { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
}