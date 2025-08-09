using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.Entities
{
    public class EpisodeView
    {
        [Key]
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public int EpisodeId { get; set; }
        public Guid RequesterId { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation property
        public RequesterSocial Requester { get; set; } = null!;
    }

}
