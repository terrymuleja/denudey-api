using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Denudey.Api.Application.Interfaces;

using Denudey.Api.Services.Infrastructure.DbContexts;
using Denudey.Api.Domain.Exceptions;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Domain.Models;

namespace Denudey.Api.Application.Services
{
    public class WalletService : IWalletService
    {
        private readonly StatsDbContext _context;
        private readonly ILogger<WalletService> _logger;

        // Bean to USD conversion rate (you can make this configurable)
        private const decimal BEAN_TO_USD_RATE = 0.33m; // 1 bean = $0.33

        public WalletService(StatsDbContext context, ILogger<WalletService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region READ Operations

        public async Task<UserWallet> CreateWalletAsync(Guid userId)
        {
            try
            {
                // Check if wallet already exists
                var existingWallet = await _context.UserWallets.FirstOrDefaultAsync(w => w.UserId == userId);
                if (existingWallet != null)
                {
                    _logger.LogInformation("Wallet already exists for user {UserId}, returning existing wallet", userId);
                    return existingWallet;
                }

                // Create new wallet
                var wallet = new UserWallet
                {
                    UserId = userId,
                    BeanBalance = 0m,
                    UsdBalance = 0m,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow
                };

                _context.UserWallets.Add(wallet);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created new wallet for user {UserId}", userId);
                return wallet;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating wallet for user {UserId}", userId);
                throw;
            }
        }
        public async Task<UserWallet> GetWalletAsync(Guid userId)
        {
            UserWallet? wallet = new UserWallet();
            try
            {
                wallet = await _context.UserWallets.FirstOrDefaultAsync(w => w.UserId == userId);
                if (wallet == null)
                {
                    wallet = await this.CreateWalletAsync(userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw;
            }            

            return wallet;
        }

        public async Task<decimal> GetBeanBalanceAsync(Guid userId)
        {
            var wallet = await GetWalletAsync(userId);
            return wallet.BeanBalance;
        }

        public async Task<decimal> GetUsdBalanceAsync(Guid userId)
        {
            var wallet = await GetWalletAsync(userId);
            return wallet.UsdBalance;
        }

        public async Task<bool> HasSufficientBeansAsync(Guid userId, decimal amount)
        {
            var balance = await GetBeanBalanceAsync(userId);
            return balance >= amount;
        }

        #endregion

        #region BEAN Operations

        public async Task<bool> AddBeansAsync(Guid userId, decimal amount, string description)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be positive", nameof(amount));
            }

            try
            {
                var wallet = await GetWalletAsync(userId);
                wallet.BeanBalance += amount;
                wallet.LastUpdated = DateTime.UtcNow;

                // Create transaction record
                await CreateTransactionAsync(new WalletTransactionDto
                {
                    UserId = userId,
                    Type = WalletTransactionType.Credit,
                    Amount = amount,
                    Currency = "BEAN",
                    Description = description,
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation("Added {Amount} beans to user {UserId}. New balance: {Balance}",
                    amount, userId, wallet.BeanBalance);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding {Amount} beans to user {UserId}", amount, userId);
                return false;
            }
        }

        public async Task<bool> DeductBeansAsync(Guid userId, decimal amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be positive", nameof(amount));
            }

            try
            {
                var wallet = await GetWalletAsync(userId);

                if (wallet.BeanBalance < amount)
                {
                    throw new InsufficientFundsException($"Insufficient bean balance. Required: {amount}, Available: {wallet.BeanBalance}");
                }

                wallet.BeanBalance -= amount;
                wallet.LastUpdated = DateTime.UtcNow;

                // Create transaction record
                await CreateTransactionAsync(new WalletTransactionDto
                {
                    UserId = userId,
                    Type = WalletTransactionType.Debit,
                    Amount = amount,
                    Currency = "BEAN",
                    Description = "Beans deducted from wallet"
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation("Deducted {Amount} beans from user {UserId}. New balance: {Balance}",
                    amount, userId, wallet.BeanBalance);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deducting {Amount} beans from user {UserId}", amount, userId);
                return false;
            }
        }

        public async Task<bool> TransferBeansAsync(Guid fromUserId, Guid toUserId, decimal amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be positive", nameof(amount));
            }

            try
            {
                // Use a transaction to ensure atomicity
                using var transaction = await _context.Database.BeginTransactionAsync();

                // Deduct from sender
                var fromWallet = await GetWalletAsync(fromUserId);
                if (fromWallet.BeanBalance < amount)
                {
                    throw new InsufficientFundsException($"Insufficient bean balance for transfer. Required: {amount}, Available: {fromWallet.BeanBalance}");
                }

                fromWallet.BeanBalance -= amount;
                fromWallet.LastUpdated = DateTime.UtcNow;

                // Add to receiver
                var toWallet = await GetWalletAsync(toUserId);
                toWallet.BeanBalance += amount;
                toWallet.LastUpdated = DateTime.UtcNow;

                // Create transaction records
                await CreateTransactionAsync(new WalletTransactionDto
                {
                    UserId = fromUserId,
                    Type = WalletTransactionType.Transfer,
                    Amount = -amount, // Negative for sender
                    Currency = "BEAN",
                    Description = $"Transfer to user {toUserId}"
                });

                await CreateTransactionAsync(new WalletTransactionDto
                {
                    UserId = toUserId,
                    Type = WalletTransactionType.Transfer,
                    Amount = amount, // Positive for receiver
                    Currency = "BEAN",
                    Description = $"Transfer from user {fromUserId}"
                });

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("Transferred {Amount} beans from user {FromUserId} to user {ToUserId}",
                    amount, fromUserId, toUserId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error transferring {Amount} beans from user {FromUserId} to user {ToUserId}",
                    amount, fromUserId, toUserId);
                return false;
            }
        }

        #endregion

        #region USD Operations

        public async Task<bool> AddUsdAsync(Guid userId, decimal amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be positive", nameof(amount));
            }

            try
            {
                var wallet = await GetWalletAsync(userId);
                wallet.UsdBalance += amount;
                wallet.LastUpdated = DateTime.UtcNow;

                // Create transaction record
                await CreateTransactionAsync(new WalletTransactionDto
                {
                    UserId = userId,
                    Type = WalletTransactionType.Credit,
                    Amount = amount,
                    Currency = "USD",
                    Description = "USD added to wallet"
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation("Added ${Amount} to user {UserId}. New balance: ${Balance}",
                    amount, userId, wallet.UsdBalance);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding ${Amount} to user {UserId}", amount, userId);
                return false;
            }
        }

        public async Task<bool> DeductUsdAsync(Guid userId, decimal amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentException("Amount must be positive", nameof(amount));
            }

            try
            {
                var wallet = await GetWalletAsync(userId);

                if (wallet.UsdBalance < amount)
                {
                    throw new InsufficientFundsException($"Insufficient USD balance. Required: ${amount}, Available: ${wallet.UsdBalance}");
                }

                wallet.UsdBalance -= amount;
                wallet.LastUpdated = DateTime.UtcNow;

                // Create transaction record
                await CreateTransactionAsync(new WalletTransactionDto
                {
                    UserId = userId,
                    Type = WalletTransactionType.Debit,
                    Amount = amount,
                    Currency = "USD",
                    Description = "USD deducted from wallet"
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation("Deducted ${Amount} from user {UserId}. New balance: ${Balance}",
                    amount, userId, wallet.UsdBalance);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deducting ${Amount} from user {UserId}", amount, userId);
                return false;
            }
        }

        #endregion

        #region CONVERSION Operations

        public async Task<bool> ConvertBeansToUsdAsync(Guid userId, decimal beanAmount)
        {
            if (beanAmount <= 0)
            {
                throw new ArgumentException("Bean amount must be positive", nameof(beanAmount));
            }

            try
            {
                var wallet = await GetWalletAsync(userId);

                if (wallet.BeanBalance < beanAmount)
                {
                    throw new InsufficientFundsException($"Insufficient bean balance for conversion. Required: {beanAmount}, Available: {wallet.BeanBalance}");
                }

                var usdAmount = beanAmount * BEAN_TO_USD_RATE;

                wallet.BeanBalance -= beanAmount;
                wallet.UsdBalance += usdAmount;
                wallet.LastUpdated = DateTime.UtcNow;

                // Create transaction record
                await CreateTransactionAsync(new WalletTransactionDto
                {
                    UserId = userId,
                    Type = WalletTransactionType.Conversion,
                    Amount = beanAmount,
                    Currency = "BEAN_TO_USD",
                    Description = $"Converted {beanAmount} beans to ${usdAmount:F2}"
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation("Converted {BeanAmount} beans to ${UsdAmount} for user {UserId}",
                    beanAmount, usdAmount, userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting {BeanAmount} beans to USD for user {UserId}", beanAmount, userId);
                return false;
            }
        }

        public async Task<bool> ConvertUsdToBeansAsync(Guid userId, decimal usdAmount)
        {
            if (usdAmount <= 0)
            {
                throw new ArgumentException("USD amount must be positive", nameof(usdAmount));
            }

            try
            {
                var wallet = await GetWalletAsync(userId);

                if (wallet.UsdBalance < usdAmount)
                {
                    throw new InsufficientFundsException($"Insufficient USD balance for conversion. Required: ${usdAmount}, Available: ${wallet.UsdBalance}");
                }

                var beanAmount = usdAmount / BEAN_TO_USD_RATE;

                wallet.UsdBalance -= usdAmount;
                wallet.BeanBalance += beanAmount;
                wallet.LastUpdated = DateTime.UtcNow;

                // Create transaction record
                await CreateTransactionAsync(new WalletTransactionDto
                {
                    UserId = userId,
                    Type = WalletTransactionType.Conversion,
                    Amount = usdAmount,
                    Currency = "USD_TO_BEAN",
                    Description = $"Converted ${usdAmount:F2} to {beanAmount} beans"
                });

                await _context.SaveChangesAsync();

                _logger.LogInformation("Converted ${UsdAmount} to {BeanAmount} beans for user {UserId}",
                    usdAmount, beanAmount, userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting ${UsdAmount} to beans for user {UserId}", usdAmount, userId);
                return false;
            }
        }

        public async Task<decimal> GetBeanToUsdRateAsync()
        {
            // In a real app, this might come from a configuration service or external API
            return await Task.FromResult(BEAN_TO_USD_RATE);
        }

        #endregion

        #region TRANSACTION History

        public async Task<IEnumerable<WalletTransaction>> GetTransactionHistoryAsync(Guid userId)
        {
            return await _context.WalletTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<WalletTransaction> CreateTransactionAsync(WalletTransactionDto transactionDto)
        {
            var transaction = new WalletTransaction
            {
                Id = Guid.NewGuid(),
                UserId = transactionDto.UserId,
                Type = transactionDto.Type,
                Amount = transactionDto.Amount,
                Currency = transactionDto.Currency,
                Description = transactionDto.Description,
                RelatedEntityId = transactionDto.RelatedEntityId,
                RelatedEntityType = transactionDto.RelatedEntityType,
                CreatedAt = DateTime.UtcNow
            };

            _context.WalletTransactions.Add(transaction);
            // Note: SaveChanges is called by the calling method

            return transaction;
        }

        #endregion
    }
}