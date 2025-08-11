using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.Exceptions
{
    public class DenudeyInvalidOperationException : Exception
    {
        public DenudeyInvalidOperationException(string message) : base(message) { }

        public DenudeyInvalidOperationException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
