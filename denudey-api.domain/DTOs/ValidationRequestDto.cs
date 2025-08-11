using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.DTOs
{
    public class ValidationRequestDto
    {
        public Guid RequestId { get; set; }
        public RequestValidationResult ValidationResult { get; set; } = new();
    }
}
