using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.Models
{
    public enum WalletTransactionType
    {
        Credit = 1,     // Money added
        Debit = 2,      // Money deducted
        Transfer = 3,   // Money transferred
        Conversion = 4, // Currency conversion
        Refund = 5,     // Money refunded
        Purchase = 6,   // Purchase made
        Earning = 7     // Money earned
    }
}
