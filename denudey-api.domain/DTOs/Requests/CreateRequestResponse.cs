using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.DTOs.Requests
{
    public class CreateRequestResponse
    {
        public bool Success { get; set; }
        public UserRequestResponseDto Request { get; set; } = new();
    }

}
