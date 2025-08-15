using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.Models
{
    public class EntityNotFoundException: Exception
    {
        public EntityNotFoundException(string message):base(message)
        {
            
        }
    }
}
