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
        public DateTime CreatedAt { get; set; }

        // Navigation property
        public CreatorSocial Creator { get; set; } = null!;
    }



}
