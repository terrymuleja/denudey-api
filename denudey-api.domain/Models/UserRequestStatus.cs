using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.Models
{
    public enum UserRequestStatus
    {
        Pending = 0,
        Accepted = 1,
        Delivered = 2,
        Validated = 3,
        Paid = 4,
        Dispute = 5,
        Cancelled = 6,
        Expired = 7
    }
}
