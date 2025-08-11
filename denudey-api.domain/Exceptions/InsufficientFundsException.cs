using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.Exceptions
{
    public class InsufficientFundsException : Exception
    {
        public InsufficientFundsException(string message) : base(message) { }

        public InsufficientFundsException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
