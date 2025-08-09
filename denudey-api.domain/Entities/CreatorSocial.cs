using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.Entities
{
    public class CreatorSocial
    {
        public Guid CreatorId { get; set; } = Guid.Empty;
        public string? ProfileImageUrl { get; set; }
        public string? Username { get; set; }
        public string? Bio { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        
        public ICollection<ProductView> ProductViews { get; set; } = new List<ProductView>();
        public ICollection<ProductLike> ProductLikes { get; set; } = new List<ProductLike>();
    }
}
