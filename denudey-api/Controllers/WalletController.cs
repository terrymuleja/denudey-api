using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Denudey.Api.Application.Interfaces;
using Denudey.Api.Domain.DTOs;
using Denudey.Api.Domain.Entities;
using Denudey.Api.Domain.Exceptions;
using Denudey.Api.Domain.Models;
using Denudey.Api.Domain.DTOs.Gems;
using Denudey.Application.Interfaces;

namespace Denudey.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WalletController(
        IWalletService walletService,
        ISocialService socialService,
        ILogger<WalletController> logger) : DenudeyControlerBase(socialService, logger)
    {
        
        
        #region Wallet Management

        /// <summary>
        /// Create a new wallet for a user
        /// </summary>
        [HttpPost("create")]
        public async Task<ActionResult<UserWallet>> CreateWallet()
        {
            var userId = GetUserId();
            try
            {
                
                if (userId == Guid.Empty)
                {
                    return BadRequest(new { message = "Valid UserId is required" });
                }

                var wallet = await walletService.CreateWalletAsync(userId);
                return Ok(wallet);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error creating wallet for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while creating the wallet" });
            }
        }

        /// <summary>
        /// Get wallet information for a user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<UserWallet>> GetWallet()
        {
            var userId = GetUserId();
            try
            {
                var wallet = await walletService.GetWalletAsync(userId);
                return Ok(wallet);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving wallet for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while retrieving the wallet" });
            }
        }

        #endregion

        #region Gem Operations

        /// <summary>
        /// Get gem balance for a user
        /// </summary>
        [HttpGet("gems/balance")]
        public async Task<ActionResult<object>> GetGemBalance()
        {
            var userId = GetUserId();
            try
            {
                var balance = await walletService.GetGemBalanceAsync(userId);
                return Ok(new { balance });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving gem balance for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while retrieving gem balance" });
            }
        }

        /// <summary>
        /// Check if user has sufficient gems
        /// </summary>
        [HttpGet("gems/sufficient")]
        public async Task<ActionResult<object>> HasSufficientGems([FromQuery] decimal amount)
        {
            var userId = GetUserId();
            try
            {
                var hasSufficient = await walletService.HasSufficientGemsAsync(userId, amount);
                return Ok(new { hasSufficient });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking sufficient gems for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while checking gem balance" });
            }
        }

        /// <summary>
        /// Add gems to user wallet
        /// </summary>
        [HttpPost("gems/add")]
        public async Task<ActionResult<object>> AddGems([FromBody] AddGemsRequest request)
        {
            var userId = GetUserId();
            try
            {
                if (request.Amount <= 0)
                {
                    return BadRequest(new { message = "Amount must be positive" });
                }
                var descripton = "Manual add";
                var success = await walletService.AddGemsAsync(userId, request.Amount, descripton);
                return Ok(new { success });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding gems for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while adding gems" });
            }
        }

        /// <summary>
        /// Deduct gems from user wallet
        /// </summary>
        [HttpPost("gems/deduct")]
        public async Task<ActionResult<object>> DeductGems([FromBody] DeductGemsRequest request)
        {
            var userId = GetUserId();
            try
            {
                if (request.Amount <= 0)
                {
                    return BadRequest(new { message = "Amount must be positive" });
                }

                var success = await walletService.DeductGemsAsync(userId, request.Amount);
                return Ok(new { success });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InsufficientFundsException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deducting gems for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while deducting gems" });
            }
        }

        /// <summary>
        /// Transfer gems between users
        /// </summary>
        [HttpPost("gems/transfer")]
        public async Task<ActionResult<object>> TransferGems([FromBody] TransferGemsRequest request)
        {
            var userId = GetUserId();
            try
            {
                if (request.Amount <= 0)
                {
                    return BadRequest(new { message = "Amount must be positive" });
                }

                if (userId == request.ToUserId)
                {
                    return BadRequest(new { message = "Cannot transfer to the same user" });
                }

                var success = await walletService.TransferGemsAsync(userId, request.ToUserId, request.Amount);
                return Ok(new { success });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InsufficientFundsException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error transferring gems from {FromUserId} to {ToUserId}",
                    userId, request.ToUserId);
                return StatusCode(500, new { message = "An error occurred while transferring gems" });
            }
        }

        #endregion

        #region USD Operations

        /// <summary>
        /// Get USD balance for a user
        /// </summary>
        [HttpGet("usd/balance")]
        public async Task<ActionResult<object>> GetUsdBalance()
        {
            var userId = GetUserId();
            try
            {
                var balance = await walletService.GetUsdBalanceAsync(userId);
                return Ok(new { balance });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving USD balance for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while retrieving USD balance" });
            }
        }

        /// <summary>
        /// Add USD to user wallet
        /// </summary>
        [HttpPost("usd/add")]
        public async Task<ActionResult<object>> AddUsd([FromBody] AddUsdRequest request)
        {
            var userId = GetUserId();
            try
            {
                if (request.Amount <= 0)
                {
                    return BadRequest(new { message = "Amount must be positive" });
                }

                var success = await walletService.AddUsdAsync(userId, request.Amount);
                return Ok(new { success });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error adding USD for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while adding USD" });
            }
        }

        /// <summary>
        /// Deduct USD from user wallet
        /// </summary>
        [HttpPost("usd/deduct")]
        public async Task<ActionResult<object>> DeductUsd([FromBody] DeductUsdRequest request)
        {
            var userId = GetUserId();
            try
            {
                if (request.Amount <= 0)
                {
                    return BadRequest(new { message = "Amount must be positive" });
                }

                var success = await walletService.DeductUsdAsync(userId, request.Amount);
                return Ok(new { success });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InsufficientFundsException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error deducting USD for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while deducting USD" });
            }
        }

        #endregion

        #region Conversion Operations

        /// <summary>
        /// Convert gems to USD
        /// </summary>
        [HttpPost("convert/gems-to-usd")]
        public async Task<ActionResult<object>> ConvertGemsToUsd([FromBody] ConvertGemsToUsdRequest request)
        {
            var userId = GetUserId();
            try
            {
                if (request.GemAmount <= 0)
                {
                    return BadRequest(new { message = "Gem amount must be positive" });
                }

                var success = await walletService.ConvertGemsToUsdAsync(userId, request.GemAmount);
                return Ok(new { success });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InsufficientFundsException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error converting gems to USD for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while converting gems to USD" });
            }
        }

        /// <summary>
        /// Convert USD to gems
        /// </summary>
        [HttpPost("convert/usd-to-gems")]
        public async Task<ActionResult<object>> ConvertUsdToGems([FromBody] ConvertUsdToGemsRequest request)
        {
            var userId = GetUserId();
            try
            {
                if (request.UsdAmount <= 0)
                {
                    return BadRequest(new { message = "USD amount must be positive" });
                }

                var success = await walletService.ConvertUsdToGemsAsync(userId, request.UsdAmount);
                return Ok(new { success });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (InsufficientFundsException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error converting USD to gems for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while converting USD to gems" });
            }
        }

        /// <summary>
        /// Get current gem to USD exchange rate
        /// </summary>
        [HttpGet("exchange-rate/gem-to-usd")]
        public async Task<ActionResult<object>> GetGemToUsdRate()
        {
            try
            {
                var rate = await walletService.GetGemsToUsdRateAsync();
                return Ok(new { rate });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving gem to USD rate");
                return StatusCode(500, new { message = "An error occurred while retrieving exchange rate" });
            }
        }

        #endregion

        #region Transaction History

        /// <summary>
        /// Get transaction history for a user
        /// </summary>
        [HttpGet("transactions")]
        public async Task<ActionResult<IEnumerable<WalletTransaction>>> GetTransactionHistory()
        {
            var userId = GetUserId();
            try
            {
                var transactions = await walletService.GetTransactionHistoryAsync(userId);
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrieving transaction history for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while retrieving transaction history" });
            }
        }

        #endregion

        #region Credit Pack Purchase

        /// <summary>
        /// Purchase a credit pack
        /// </summary>
        [HttpPost("purchase/credit-pack")]
        public async Task<ActionResult<object>> PurchaseCreditPack([FromBody] PurchaseCreditPackRequest request)
        {
            var userId = GetUserId();
            try
            {
                // Define credit packs (in a real app, this might come from a configuration service)
                var creditPacks = new Dictionary<string, (decimal price, int totalGems, string name)>
                {
                    { "starter", (5.00m, 11, "Starter Pack") },
                    { "popular", (10.00m, 23, "Popular Pack") },
                    { "vip", (20.00m, 48, "VIP Pack") }
                };

                if (!creditPacks.ContainsKey(request.PackId.ToLower()))
                {
                    return BadRequest(new { message = "Invalid credit pack selected" });
                }

                var pack = creditPacks[request.PackId.ToLower()];

                // In a real app, you would process payment here
                // For now, we'll just add the gems directly
                var description = $"Purchased {pack.name} - ${pack.price:F2}";
                var success = await walletService.AddGemsAsync(userId, pack.totalGems, description);

                if (success)
                {
                    // Create a transaction record for the purchase
                    await walletService.CreateTransactionAsync(new WalletTransactionDto
                    {
                        UserId = userId,
                        Type = WalletTransactionType.Credit,
                        Amount = pack.totalGems,
                        Currency = "BEAN",
                        Description = description
                    });
                }

                return Ok(new { success });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error purchasing credit pack for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while purchasing credit pack" });
            }
        }

        #endregion
    }
    
   
    
   

    

    

    

   

    
}