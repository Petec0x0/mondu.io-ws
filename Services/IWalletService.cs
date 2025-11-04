using SimpleWalletSystem.Models;

public interface IWalletService
{
    Task<Wallet> TopUpAsync(int userId, decimal amount, string paymentRef);
    Task<Wallet> TransferAsync(int fromUserId, int toUserId, decimal amount);
    Task<decimal> GetBalanceAsync(int userId);
    Task<List<TransactionDto>> GetTransactionsAsync(int userId); // Changed to TransactionDto
}