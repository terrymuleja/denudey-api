using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.Exceptions
{
    public class DenudeyNotFoundException : Exception
    {
        public DenudeyNotFoundException(string message) : base(message) { }

        public DenudeyNotFoundException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
