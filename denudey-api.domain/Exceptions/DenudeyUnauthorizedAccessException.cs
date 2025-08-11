using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.Exceptions
{
    public class DenudeyUnauthorizedAccessException : Exception
    {
        public DenudeyUnauthorizedAccessException(string message) : base(message) { }

        public DenudeyUnauthorizedAccessException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
