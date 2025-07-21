using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.Entities
{
    public class Product
    {
        public Guid Id { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public List<string> Tags { get; set; } = new();
        public string MainPhotoUrl { get; set; }
        public List<string> SecondaryPhotoUrls { get; set; } = new();
        public string BodyPart { get; set; }
        public List<string> DeliveryOptions { get; set; } = new();
        public decimal FeePerDelivery { get; set; }

        public bool IsPublished { get; set; } = false;
        public bool IsExpired { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

        public Guid CreatedBy { get; set; }
        public ApplicationUser Creator { get; set; } = null!; // User ID of the model who created this product

        public List<Demand> Demands { get; set; } = new();  // many demands reference this product
    }

}
