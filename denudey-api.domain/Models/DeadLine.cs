using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.Models
{
    public enum DeadLine
    {
        ThreeDays = 0,      // 3-5 days, base price
        Express48h = 1,    // +1 bean, 48 hours
        Express24h = 2     // +2 beans, 24 hours
    }
}
