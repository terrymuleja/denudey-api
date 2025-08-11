using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.DTOs.Beans
{
    public class AddBeansRequest
    {

        public decimal Amount { get; set; }
        public string? Description { get; set; }
    }

}
