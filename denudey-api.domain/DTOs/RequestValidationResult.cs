using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.DTOs
{
    public class RequestValidationResult
    {
        public bool BodyPartValid { get; set; }
        public bool TextValid { get; set; }
        public bool? ManualOverride { get; set; }
        public bool IsValid => BodyPartValid && TextValid;
    }
}
