using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denudey.Api.Domain.Entities
{
    public class EpisodeView
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public int EpisodeId { get; set; }
        public ScamflixEpisode Episode { get; set; }
        public DateTime ViewedAt { get; set; }
    }
}
