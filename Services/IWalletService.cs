using SimpleWalletSystem.Models;

namespace SimpleWalletSystem.Services;

public interface IWalletService
{
    Task<Wallet> TopUpAsync(Guid userId, decimal amount, string paymentRef);
    Task<Wallet> TransferAsync(Guid fromUserId, Guid toUserId, decimal amount);
    Task<WalletBalanceDto> GetBalanceAsync(Guid userId);
    Task<IEnumerable<TransactionDto>> GetTransactionsAsync(Guid userId);
}

public class WalletBalanceDto
{
    public decimal Balance { get; set; }
    public string Currency { get; set; } = "USD";
    public DateTime LastUpdated { get; set; }
}