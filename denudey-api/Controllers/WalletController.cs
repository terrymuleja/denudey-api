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
using Denudey.Api.Domain.DTOs.Beans;

namespace Denudey.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class WalletController : DenudeyControlerBase
    {
        private readonly IWalletService _walletService;
        private readonly ILogger<WalletController> _logger;

        public WalletController(IWalletService walletService, ILogger<WalletController> logger)
        {
            _walletService = walletService;
            _logger = logger;
        }

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

                var wallet = await _walletService.CreateWalletAsync(userId);
                return Ok(wallet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating wallet for user {UserId}", userId);
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
                var wallet = await _walletService.GetWalletAsync(userId);
                return Ok(wallet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving wallet for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while retrieving the wallet" });
            }
        }

        #endregion

        #region Bean Operations

        /// <summary>
        /// Get bean balance for a user
        /// </summary>
        [HttpGet("beans/balance")]
        public async Task<ActionResult<object>> GetBeanBalance()
        {
            var userId = GetUserId();
            try
            {
                var balance = await _walletService.GetBeanBalanceAsync(userId);
                return Ok(new { balance });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bean balance for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while retrieving bean balance" });
            }
        }

        /// <summary>
        /// Check if user has sufficient beans
        /// </summary>
        [HttpGet("beans/sufficient")]
        public async Task<ActionResult<object>> HasSufficientBeans([FromQuery] decimal amount)
        {
            var userId = GetUserId();
            try
            {
                var hasSufficient = await _walletService.HasSufficientBeansAsync(userId, amount);
                return Ok(new { hasSufficient });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking sufficient beans for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while checking bean balance" });
            }
        }

        /// <summary>
        /// Add beans to user wallet
        /// </summary>
        [HttpPost("beans/add")]
        public async Task<ActionResult<object>> AddBeans([FromBody] AddBeansRequest request)
        {
            var userId = GetUserId();
            try
            {
                if (request.Amount <= 0)
                {
                    return BadRequest(new { message = "Amount must be positive" });
                }

                var success = await _walletService.AddBeansAsync(userId, request.Amount);
                return Ok(new { success });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding beans for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while adding beans" });
            }
        }

        /// <summary>
        /// Deduct beans from user wallet
        /// </summary>
        [HttpPost("beans/deduct")]
        public async Task<ActionResult<object>> DeductBeans([FromBody] DeductBeansRequest request)
        {
            var userId = GetUserId();
            try
            {
                if (request.Amount <= 0)
                {
                    return BadRequest(new { message = "Amount must be positive" });
                }

                var success = await _walletService.DeductBeansAsync(userId, request.Amount);
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
                _logger.LogError(ex, "Error deducting beans for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while deducting beans" });
            }
        }

        /// <summary>
        /// Transfer beans between users
        /// </summary>
        [HttpPost("beans/transfer")]
        public async Task<ActionResult<object>> TransferBeans([FromBody] TransferBeansRequest request)
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

                var success = await _walletService.TransferBeansAsync(userId, request.ToUserId, request.Amount);
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
                _logger.LogError(ex, "Error transferring beans from {FromUserId} to {ToUserId}",
                    userId, request.ToUserId);
                return StatusCode(500, new { message = "An error occurred while transferring beans" });
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
                var balance = await _walletService.GetUsdBalanceAsync(userId);
                return Ok(new { balance });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving USD balance for user {UserId}", userId);
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

                var success = await _walletService.AddUsdAsync(userId, request.Amount);
                return Ok(new { success });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding USD for user {UserId}", userId);
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

                var success = await _walletService.DeductUsdAsync(userId, request.Amount);
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
                _logger.LogError(ex, "Error deducting USD for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while deducting USD" });
            }
        }

        #endregion

        #region Conversion Operations

        /// <summary>
        /// Convert beans to USD
        /// </summary>
        [HttpPost("convert/beans-to-usd")]
        public async Task<ActionResult<object>> ConvertBeansToUsd([FromBody] ConvertBeansToUsdRequest request)
        {
            var userId = GetUserId();
            try
            {
                if (request.BeanAmount <= 0)
                {
                    return BadRequest(new { message = "Bean amount must be positive" });
                }

                var success = await _walletService.ConvertBeansToUsdAsync(userId, request.BeanAmount);
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
                _logger.LogError(ex, "Error converting beans to USD for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while converting beans to USD" });
            }
        }

        /// <summary>
        /// Convert USD to beans
        /// </summary>
        [HttpPost("convert/usd-to-beans")]
        public async Task<ActionResult<object>> ConvertUsdToBeans([FromBody] ConvertUsdToBeansRequest request)
        {
            var userId = GetUserId();
            try
            {
                if (request.UsdAmount <= 0)
                {
                    return BadRequest(new { message = "USD amount must be positive" });
                }

                var success = await _walletService.ConvertUsdToBeansAsync(userId, request.UsdAmount);
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
                _logger.LogError(ex, "Error converting USD to beans for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while converting USD to beans" });
            }
        }

        /// <summary>
        /// Get current bean to USD exchange rate
        /// </summary>
        [HttpGet("exchange-rate/bean-to-usd")]
        public async Task<ActionResult<object>> GetBeanToUsdRate()
        {
            try
            {
                var rate = await _walletService.GetBeanToUsdRateAsync();
                return Ok(new { rate });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving bean to USD rate");
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
                var transactions = await _walletService.GetTransactionHistoryAsync(userId);
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction history for user {UserId}", userId);
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
                var creditPacks = new Dictionary<string, (decimal price, int totalBeans, string name)>
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
                // For now, we'll just add the beans directly
                var description = $"Purchased {pack.name} - ${pack.price:F2}";
                var success = await _walletService.AddBeansAsync(userId, pack.totalBeans, description);

                if (success)
                {
                    // Create a transaction record for the purchase
                    await _walletService.CreateTransactionAsync(new WalletTransactionDto
                    {
                        UserId = userId,
                        Type = WalletTransactionType.Credit,
                        Amount = pack.totalBeans,
                        Currency = "BEAN",
                        Description = description
                    });
                }

                return Ok(new { success });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error purchasing credit pack for user {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while purchasing credit pack" });
            }
        }

        #endregion
    }
    
   
    
   

    

    

    

   

    
}