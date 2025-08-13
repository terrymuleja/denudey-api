using System;
using System.Threading.Tasks;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Domain.Entities;

namespace Denudey.Api.Application.Interfaces
{
    public interface IWalletService
    {
        // READ operations
        Task<UserWallet> CreateWalletAsync(Guid userId);
        Task<UserWallet> GetWalletAsync(Guid userId);
        Task<decimal> GetGemBalanceAsync(Guid userId);
        Task<decimal> GetUsdBalanceAsync(Guid userId);
        Task<bool> HasSufficientGemsAsync(Guid userId, decimal amount);

        // BEAN operations
        Task<bool> AddGemsAsync(Guid userId, decimal amount, string description);
        Task<bool> DeductGemsAsync(Guid userId, decimal amount);
        Task<bool> TransferGemsAsync(Guid fromUserId, Guid toUserId, decimal amount);

        // USD operations
        Task<bool> AddUsdAsync(Guid userId, decimal amount);
        Task<bool> DeductUsdAsync(Guid userId, decimal amount);

        // CONVERSION operations
        Task<bool> ConvertGemsToUsdAsync(Guid userId, decimal gemsAmount);
        Task<bool> ConvertUsdToGemsAsync(Guid userId, decimal usdAmount);
        Task<decimal> GetGemsToUsdRateAsync();

        // TRANSACTION history
        Task<IEnumerable<WalletTransaction>> GetTransactionHistoryAsync(Guid userId);
        Task<WalletTransaction> CreateTransactionAsync(WalletTransactionDto transaction);
    }

    
}