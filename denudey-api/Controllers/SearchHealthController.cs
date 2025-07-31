using Elastic.Clients.Elasticsearch;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Denudey.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SearchHealthController : ControllerBase
    {
        private readonly ElasticsearchClient _elastic;

        public SearchHealthController(ElasticsearchClient elastic)
        {
            _elastic = elastic;
        }

        [HttpGet("ping")]
        public async Task<IActionResult> Ping()
        {
            var result = await _elastic.PingAsync();
            return Ok(new { success = result.IsValidResponse });
        }
    }
}
