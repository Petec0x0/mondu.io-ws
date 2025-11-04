using Microsoft.AspNetCore.Mvc;
using SimpleWalletSystem.Models;
using SimpleWalletSystem.Services;

namespace SimpleWalletSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WalletController : ControllerBase
{
    private readonly IWalletService _walletService;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<WalletController> _logger;

    public WalletController(
        IWalletService walletService, 
        IPaymentService paymentService,
        ILogger<WalletController> logger)
    {
        _walletService = walletService;
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpGet("balance/{userId}")]
    public async Task<ActionResult<WalletBalanceDto>> GetBalance(Guid userId)
    {
        try
        {
            var balance = await _walletService.GetBalanceAsync(userId);
            return Ok(balance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("transactions/{userId}")]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetTransactions(Guid userId)
    {
        try
        {
            var transactions = await _walletService.GetTransactionsAsync(userId);
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transactions for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("topup/initiate")]
    public async Task<ActionResult> InitiateTopUp([FromBody] TopUpRequest request)
    {
        try
        {
            // In real app, get return URL from config
            var returnUrl = $"{Request.Scheme}://{Request.Host}/api/wallet/topup/confirm";
            
            var transactionId = await _paymentService.InitializePayment(
                request.Amount, 
                "USD", 
                returnUrl);
            
            return Ok(new { 
                success = true,
                transactionId = transactionId,
                message = "Payment initiated successfully",
                paymentUrl = $"{returnUrl}?transactionId={transactionId}&userId={request.UserId}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating top up");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("topup/confirm")]
    public async Task<ActionResult> ConfirmTopUp([FromQuery] string transactionId, [FromQuery] Guid userId)
    {
        try
        {
            // Verify payment first
            var paymentStatus = await _paymentService.VerifyPayment(transactionId);
            
            if (!paymentStatus.Success || paymentStatus.Status != "Completed")
            {
                return BadRequest(new { error = "Payment verification failed: " + paymentStatus.Message });
            }

            // For demo, use a fixed amount since our mock doesn't store amount
            var amount = 100.00m; // In real scenario, get this from paymentStatus
            var wallet = await _walletService.TopUpAsync(userId, amount, transactionId);
            
            return Ok(new { 
                message = "Top up completed successfully", 
                balance = wallet.Balance,
                transactionId = transactionId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming top up for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("transfer")]
    public async Task<ActionResult> Transfer([FromBody] TransferRequest request)
    {
        try
        {
            var wallet = await _walletService.TransferAsync(request.FromUserId, request.ToUserId, request.Amount);
            
            return Ok(new { 
                message = "Transfer completed successfully", 
                newBalance = wallet.Balance 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring from {FromUserId} to {ToUserId}", 
                request.FromUserId, request.ToUserId);
            return BadRequest(new { error = ex.Message });
        }
    }
}

// Request DTOs
public class TopUpRequest
{
    public decimal Amount { get; set; }
    public Guid UserId { get; set; }
}

public class TransferRequest
{
    public Guid FromUserId { get; set; }
    public Guid ToUserId { get; set; }
    public decimal Amount { get; set; }
}