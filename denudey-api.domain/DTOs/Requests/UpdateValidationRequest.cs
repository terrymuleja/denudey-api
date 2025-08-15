using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.DTOs.Requests
{
    public class UpdateValidationRequest
    {
        public Guid RequestId { get; set; }
        public string Status { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; }
        public bool RequiresHumanReview { get; set; }
        public DateTime ValidatedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
