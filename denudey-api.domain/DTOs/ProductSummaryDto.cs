using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.DTOs
{
    public class ProductSummaryDto
    {
        public Guid Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string BodyPart { get; set; } = string.Empty;
        public string MainPhotoUrl { get; set; } = string.Empty;

        public List<string> Tags { get; set; } = new List<string>();

        public bool IsPublished { get; set; }
        public bool IsExpired { get; set; }
        public string CreatorUsername { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ModifiedAt { get; set; }
    }

}
