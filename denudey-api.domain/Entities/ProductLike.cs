using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.Entities
{
    public class ProductLike
    {
        public Guid UserId { get; set; }
        public Guid ProductId { get; set; }
        public Guid CreatorId { get; set; }
        public string CreatorUsername { get; set; } = string.Empty;
        public string CreatorProfileImageUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }



}
