using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.DTOs
{
    public class ProductDto
    {
        public Guid Id { get; set; }
        public Guid CreatorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string BodyPart { get; set; } = string.Empty;
        public decimal BasePrice { get; set; }
        public bool IsActive { get; set; }
    }
}
