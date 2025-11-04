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
    public async Task<ActionResult<decimal>> GetBalance(int userId)
    {
        try
        {
            var balance = await _walletService.GetBalanceAsync(userId);
            return Ok(new { UserId = userId, Balance = balance });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting balance for user {UserId}", userId);
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("transactions/{userId}")]
    public async Task<ActionResult<List<TransactionDto>>> GetTransactions(int userId) // Changed to TransactionDto
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
    public async Task<ActionResult<PaymentResponse>> InitiateTopUp([FromBody] TopUpRequest request)
    {
        try
        {
            // In real app, get return URL from config
            var returnUrl = $"{Request.Scheme}://{Request.Host}/api/wallet/topup/confirm";

            var response = await _paymentService.InitializePaymentAsync(
                request.Amount,
                "USD",
                returnUrl);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating top up");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("topup/confirm")]
    public async Task<ActionResult> ConfirmTopUp([FromQuery] string transactionId, [FromQuery] int userId)
    {
        try
        {
            // Verify payment first
            var paymentStatus = await _paymentService.VerifyPaymentAsync(transactionId);

            if (!paymentStatus.Success || paymentStatus.Status != "Completed")
            {
                return BadRequest(new { error = "Payment verification failed" });
            }

            var wallet = await _walletService.TopUpAsync(userId, paymentStatus.Amount, transactionId);

            return Ok(new
            {
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

            return Ok(new
            {
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
    public int UserId { get; set; }
}

public class TransferRequest
{
    public int FromUserId { get; set; }
    public int ToUserId { get; set; }
    public decimal Amount { get; set; }
}