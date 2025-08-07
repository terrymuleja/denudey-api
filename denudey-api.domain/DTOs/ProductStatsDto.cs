using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.DTOs
{
    public class ProductStatsDto
    {
        public Guid ProductId { get; set; }
        public int Views { get; set; }
        public int Likes { get; set; }
        public bool UserHasLiked { get; set; }
    }

}
