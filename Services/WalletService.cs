using Microsoft.EntityFrameworkCore;
using SimpleWalletSystem.Models;

namespace SimpleWalletSystem.Services;

public class WalletService : IWalletService
{
    private readonly AppDbContext _context;
    private readonly ILogger<WalletService> _logger;

    public WalletService(AppDbContext context, ILogger<WalletService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<WalletBalanceDto> GetBalanceAsync(Guid userId)
    {
        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId);
        
        if (wallet == null)
            throw new ArgumentException("Wallet not found");

        return new WalletBalanceDto
        {
            Balance = wallet.Balance,
            Currency = wallet.Currency,
            LastUpdated = DateTime.UtcNow
        };
    }

    public async Task<IEnumerable<TransactionDto>> GetTransactionsAsync(Guid userId)
    {
        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null)
            return new List<TransactionDto>();

        var transactions = await _context.Transactions
            .Where(t => t.WalletId == wallet.Id)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                Amount = t.Amount,
                BalanceBefore = t.BalanceBefore,
                BalanceAfter = t.BalanceAfter,
                Type = t.Type,
                Status = t.Status,
                Description = t.Description,
                Reference = t.Reference,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return transactions;
    }

    public async Task<Wallet> TopUpAsync(Guid userId, decimal amount, string paymentRef)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero");

        var wallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == userId);

        if (wallet == null)
            throw new ArgumentException("Wallet not found");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Create transaction record
            var walletTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Amount = amount,
                BalanceBefore = wallet.Balance,
                BalanceAfter = wallet.Balance + amount,
                Type = "TopUp",
                Status = "Completed",
                Description = $"Top up via MPGS - Ref: {paymentRef}",
                Reference = paymentRef
            };

            // Update wallet balance
            wallet.Balance += amount;

            await _context.Transactions.AddAsync(walletTransaction);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Top up completed for user {UserId}, amount: {Amount}", userId, amount);
            return wallet;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during top up for user {UserId}", userId);
            throw;
        }
    }

    public async Task<Wallet> TransferAsync(Guid fromUserId, Guid toUserId, decimal amount)
    {
        if (amount <= 0)
            throw new ArgumentException("Amount must be greater than zero");

        if (fromUserId == toUserId)
            throw new InvalidOperationException("Cannot transfer to yourself");

        // Get both wallets
        var fromWallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == fromUserId);
        var toWallet = await _context.Wallets
            .FirstOrDefaultAsync(w => w.UserId == toUserId);

        if (fromWallet == null || toWallet == null)
            throw new ArgumentException("One or both wallets not found");

        // Check if same tenant
        if (fromWallet.TenantId != toWallet.TenantId)
            throw new InvalidOperationException("Can only transfer within same tenant");

        if (fromWallet.Balance < amount)
            throw new InvalidOperationException("Insufficient balance");

        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            // Double-check balance within transaction
            fromWallet = await _context.Wallets
                .FirstAsync(w => w.UserId == fromUserId);
                
            if (fromWallet.Balance < amount)
                throw new InvalidOperationException("Insufficient balance");

            var transferRef = Guid.NewGuid().ToString();

            // Debit from sender
            var debitTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                WalletId = fromWallet.Id,
                Amount = -amount,
                BalanceBefore = fromWallet.Balance,
                BalanceAfter = fromWallet.Balance - amount,
                Type = "Transfer",
                Status = "Completed",
                Description = $"Transfer to user {toUserId}",
                Reference = transferRef
            };

            // Credit to receiver
            var creditTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                WalletId = toWallet.Id,
                Amount = amount,
                BalanceBefore = toWallet.Balance,
                BalanceAfter = toWallet.Balance + amount,
                Type = "Transfer",
                Status = "Completed",
                Description = $"Transfer from user {fromUserId}",
                Reference = transferRef
            };

            // Update balances
            fromWallet.Balance -= amount;
            toWallet.Balance += amount;

            await _context.Transactions.AddRangeAsync(debitTransaction, creditTransaction);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Transfer completed from {FromUserId} to {ToUserId}, amount: {Amount}", 
                fromUserId, toUserId, amount);

            return fromWallet;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error during transfer from {FromUserId} to {ToUserId}", fromUserId, toUserId);
            throw;
        }
    }
}