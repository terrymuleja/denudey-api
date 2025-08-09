using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.DTOs
{
    public class ProductActionDto
    {
        public Guid ProductId { get; set; }
        public Guid UserId { get; set; }
        public Guid CreatorId { get; set; }
      
    }
}
