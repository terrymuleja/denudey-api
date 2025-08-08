using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.DTOs
{
    public class ProductDetailsDto
    {
        public Guid Id { get; set; }
        public string ProductName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public List<string> Tags { get; set; } = new List<string>();
        public string MainPhotoUrl { get; set; } = string.Empty;
        public List<string> SecondaryPhotoUrls { get; set; } = new List<string>();

        public string BodyPart { get; set; } = string.Empty;

        public List<string> DeliveryOptions { get; set; } = new();

        public decimal FeePerDelivery { get; set; }

        public bool IsPublished { get; set; }
        public bool IsExpired { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
        public Guid CreatedBy { get; set; } = Guid.Empty;

        public string CreatorUsername { get; set; } = string.Empty;

        public string CreatorAvatarUrl { get; set; } = string.Empty;       

        public int Views { get; set; }
        public int Likes { get; set; }
        public bool HasUserLiked { get; set; }
    }
}
