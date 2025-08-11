using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.Models;

namespace Denudey.Api.Domain.DTOs
{
    public class WalletTransactionDto
    {
        public Guid UserId { get; set; }
        public WalletTransactionType Type { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public Guid? RelatedEntityId { get; set; }
        public string RelatedEntityType { get; set; } = string.Empty;
    }
}
