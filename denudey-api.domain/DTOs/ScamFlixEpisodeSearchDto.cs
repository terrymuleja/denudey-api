using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.DTOs
{
    public class ScamFlixEpisodeSearchDto
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();

        public string ImageUrl { get; set; } = string.Empty;

        public Guid CreatorId { get; set; }
        public string CreatorUsername { get; set; } = string.Empty;
        public string CreatorAvatarUrl { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        // Stats – merged from Stats DB at runtime
        public int Likes { get; set; }
        public int Views { get; set; }
        public bool UserHasLiked { get; set; }
    }

}
