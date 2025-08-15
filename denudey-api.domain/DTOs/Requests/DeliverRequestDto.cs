using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.DTOs.Requests
{
    public class DeliverRequestDto
    {
        public string DeliveryNote { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
    }
}
