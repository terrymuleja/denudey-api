using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.DTOs.Requests
{
    public class UserRequestResponseDto
    {
        public Guid Id { get; set; }
        public Guid RequestorId { get; set; }
        public Guid ProductId { get; set; }
        public Guid CreatorId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string BodyPart { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string DeliveredImageUrl { get; set; } = string.Empty;
        public decimal PriceAmount { get; set; }
        public decimal ExtraAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal Tax { get; set; }
        public string Status { get; set; } = string.Empty;
        public string DeadLine { get; set; } = string.Empty;
        public DateTime? ExpectedDeliveredDate { get; set; }
        public DateTime? DeliveredDate { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public string ValidationStatus { get; set; } = string.Empty;
    }
}
