using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.DTOs.Gems
{
    public class TransferGemsRequest
    {

        public Guid ToUserId { get; set; }
        public decimal Amount { get; set; }
    }
}
