using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.DTOs.Requests
{
    public class GetRequestsResponse
    {
        public bool Success { get; set; }
        public List<UserRequestResponseDto> Requests { get; set; } = new();
        public PaginationDto Pagination { get; set; } = new();
    }
}
