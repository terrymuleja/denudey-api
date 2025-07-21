using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.Entities
{
    public class Demand
    {
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public Guid RequestedBy { get; set; } // user ID of the requester
        public ApplicationUser Requester { get; set; } = null!; // User who made the request
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public string? DeliveryUrl { get; set; } // URL of final file/image
        public DateTime? DeliveredAt { get; set; }

        public string? Notes { get; set; } // Optional request notes from user

        public string? DeliveryDeadlineOption { get; set; } // "3d", "48h", "24h"
        public DateTime? DeadlineAt { get; set; } // Calculated when created

        public bool IsDelivered => DeliveredAt.HasValue;
    }

}
