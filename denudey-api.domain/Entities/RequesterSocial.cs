using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.Entities
{
    public class RequesterSocial
    {
        public Guid RequesterId { get; set; } = Guid.Empty;
        public string? ProfileImageUrl { get; set; }
        public string? Username { get; set; }

        public string Email { get; set; } = string.Empty;

        public string CountryCode { get; set; } = string.Empty ;
        public string Phone { get; set; } = string.Empty;

        public bool IsPrivate { get; set; }
        public string? Bio { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public ICollection<EpisodeView> EpisodeViews { get; set; } = new List<EpisodeView>();
        public ICollection<EpisodeLike> EpisodeLikes { get; set; } = new List<EpisodeLike>();

    }
}
