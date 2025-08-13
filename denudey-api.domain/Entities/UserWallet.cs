using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.Entities
{
    public class UserWallet
    {
        [Key]
        public Guid UserId { get; set; }

        [Required]
        public decimal GemBalance { get; set; }

        [Required]
        public decimal UsdBalance { get; set; }

        [Required]
        public DateTime LastUpdated { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }
    }
}
