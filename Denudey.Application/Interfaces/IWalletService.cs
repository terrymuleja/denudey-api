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
        Task<decimal> GetBeanBalanceAsync(Guid userId);
        Task<decimal> GetUsdBalanceAsync(Guid userId);
        Task<bool> HasSufficientBeansAsync(Guid userId, decimal amount);

        // BEAN operations
        Task<bool> AddBeansAsync(Guid userId, decimal amount);
        Task<bool> DeductBeansAsync(Guid userId, decimal amount);
        Task<bool> TransferBeansAsync(Guid fromUserId, Guid toUserId, decimal amount);

        // USD operations
        Task<bool> AddUsdAsync(Guid userId, decimal amount);
        Task<bool> DeductUsdAsync(Guid userId, decimal amount);

        // CONVERSION operations
        Task<bool> ConvertBeansToUsdAsync(Guid userId, decimal beanAmount);
        Task<bool> ConvertUsdToBeansAsync(Guid userId, decimal usdAmount);
        Task<decimal> GetBeanToUsdRateAsync();

        // TRANSACTION history
        Task<IEnumerable<WalletTransaction>> GetTransactionHistoryAsync(Guid userId);
        Task<WalletTransaction> CreateTransactionAsync(WalletTransactionDto transaction);
    }

    
}