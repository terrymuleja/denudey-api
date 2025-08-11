using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.Models;

namespace Denudey.Api.Domain.Entities
{
    public class WalletTransaction
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        public WalletTransactionType Type { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = string.Empty; // "BEAN", "USD", "BEAN_TO_USD", etc.

        [Required]
        [MaxLength(255)]
        public string Description { get; set; } = string.Empty;

        public Guid? RelatedEntityId { get; set; } // UserRequest ID, etc.

        [MaxLength(50)]
        public string RelatedEntityType { get; set; } = string.Empty; // "UserRequest", "Purchase", etc.

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
