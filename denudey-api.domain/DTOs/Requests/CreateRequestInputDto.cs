using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.DTOs.Requests
{
    public class CreateRequestInputDto
    {
        public Guid ProductId { get; set; } = Guid.Empty;
        public Guid CreatorId { get; set; } = Guid.Empty;
        public string? Message { get; set; }
        public string SelectedDeadline { get; set; } = string.Empty;
    }
}
