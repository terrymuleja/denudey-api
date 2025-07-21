using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.DTOs
{
    public class CreateProductDto
    {
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public string MainPhotoUrl { get; set; } = string.Empty;
        public List<string> SecondaryPhotoUrls { get; set; } = new();
        public string BodyPart { get; set; } = string.Empty; // torso, belly, legs, arms
        public List<string> DeliveryOptions { get; set; } = new(); // "3d", "48h", "24h"
    }

}
