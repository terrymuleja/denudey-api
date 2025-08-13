using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.DTOs
{
    public class UserWalletDto
    {
        public Guid UserId { get; set; }
        public decimal GemBalance { get; set; }
        public decimal UsdBalance { get; set; }
    }
}
