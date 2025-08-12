using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.DTOs.Requests
{
    public class UpdateRequestStatusDto
    {
        public string Status { get; set; } = string.Empty;
        public string? CreatorResponse { get; set; }
    }
}
