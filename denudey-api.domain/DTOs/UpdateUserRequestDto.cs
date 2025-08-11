using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Denudey.Api.Domain.Models;

namespace Denudey.Api.Domain.DTOs
{
    public class UpdateUserRequestDto
    {
        public string? Text { get; set; }
        public DeadLine? DeadLine { get; set; }
    }
}
