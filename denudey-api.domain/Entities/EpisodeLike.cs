using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.Entities
{
    public class EpisodeLike
    {
        public Guid UserId { get; set; }
        public int EpisodeId { get; set; }
        public Guid RequesterId { get; set; }        
        public DateTime CreatedAt { get; set; }

        // Navigation property
        public RequesterSocial Requester { get; set; } = null!;
    }



}
