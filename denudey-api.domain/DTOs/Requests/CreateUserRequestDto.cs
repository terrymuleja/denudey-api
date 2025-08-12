using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.Models;

namespace Denudey.Api.Domain.DTOs.Requests
{
    public class CreateUserRequestDto
    {
        public Guid RequestorId { get; set; }
        public Guid ProductId { get; set; }
        public string? Text { get; set; }
        public DeadLine DeadLine { get; set; } = DeadLine.ThreeDays;
    }
}
