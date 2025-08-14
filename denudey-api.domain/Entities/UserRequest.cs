using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.Models;

namespace Denudey.Api.Domain.Entities
{
    public class UserRequest
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        public Guid RequestorId { get; set; } // Points to RequestorSocial

        [Required]
        public Guid ProductId { get; set; } // Product in ElasticSearch

        [Required]
        public Guid CreatorId { get; set; } // Points to CreatorSocial

        [Required]
        [MaxLength(50)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string BodyPart { get; set; } = string.Empty; // Cached from Product to avoid lookups

        [MaxLength(50)]
        public string Text { get; set; } = string.Empty; // Custom text entered by requester

        [MaxLength(1000)]
        public string DeliveredImageUrl { get; set; } = string.Empty; // Cloudinary URL
        
        [Required]
        public decimal PriceAmount { get; set; } // Base price (3 gems)

        public decimal ExtraAmount { get; set; } // Extra charges for express delivery

        public decimal TotalAmount { get; set; }

        public decimal Tax { get; set; }


        [Required]
        public UserRequestStatus Status { get; set; }

        [Required]
        public DeadLine DeadLine { get; set; } // 3 days chosen by requester

        public DateTime? ExpectedDeliveredDate { get; set; }

        public DateTime? DeliveredDate { get; set; }


        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public DateTime ModifiedAt { get; set; }

        public DateTime AcceptedAt { get; set; }

        // AI Validation flags
        public bool? BodyPartValidated { get; set; }
        public bool? TextValidated { get; set; }
        public bool? ManualValidated { get; set; } // Override for AI failures

        // Navigation properties (if using EF Core)
        public virtual RequesterSocial Requester { get; set; }
        public virtual CreatorSocial Creator { get; set; }

        public string MainPhotoUrl { get; set; } = string.Empty;
    }
}
