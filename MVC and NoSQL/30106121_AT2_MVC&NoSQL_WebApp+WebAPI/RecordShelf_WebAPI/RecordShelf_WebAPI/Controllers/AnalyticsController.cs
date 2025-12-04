using Microsoft.AspNetCore.Mvc;
using RecordShelf_WebAPI.Models;
using RecordShelf_WebAPI.Services;

namespace RecordShelf_WebAPI.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AnalyticsController : Controller
    {
        private readonly AnalyticsService _analService;

        public AnalyticsController(AnalyticsService analService) =>
        _analService = analService;

        // GET_ALL v1: AnalyticsController
        [HttpGet]
        [MapToApiVersion("1.0")]
        public async Task<List<Analytics>> Get()
        {
            return await _analService.GetAllAsync();
        }

        // GET_ALL v2: AnalyticsController
        [HttpGet]
        [MapToApiVersion("2.0")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> Getv2()
        {
            var data = await _analService.GetAllAsync();

            return new
            {
                version = "2.0",
                count = data.Count,
                message = "V2 analytics endpoint hit!",
                data
            };
        }


        // GET_SINGLE v1: AnalyticsController
        [HttpGet("{analId:length(24)}")]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult<Analytics>> GetSingle(string analId)
        {
            var anal = await _analService.GetSingleAsync(analId);

            if (anal is null)
            {
                return NotFound();
            }

            return anal;
        }


        // GET_SINGLE v2: AnalyticsController
        [HttpGet("{analId:length(24)}")]
        [MapToApiVersion("2.0")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> GetSinglev2(string analId)
        {
            var anal = await _analService.GetSingleAsync(analId);

            if (anal is null)
            {
                return NotFound();
            }

            return new
            {
                version = "2.0",
                message = "V2 single analytics endpoint hit!",
                data = anal
            };
        }

        // GET_BY_AUDIO: AnalyticsController
        [HttpGet("by-audio/{audioId:length(24)}")]
        public async Task<ActionResult<Analytics>> GetByAudioId(string audioId)
        {
            var anal = await _analService.GetByAudioIdAsync(audioId);

            if (anal is null)
            {
                return NotFound();
            }

            return anal;
        }

        // CREATE: AnalyticsController
        [HttpPost]
        public async Task<IActionResult> Create(Analytics newAnal)
        {
            await _analService.CreateAsync(newAnal);

            return CreatedAtAction(nameof(Get), new { id = newAnal.AnalyticsId }, newAnal);
        }

        // UPDATE: AnalyticsController
        [HttpPut("{id:length(24)}")]
        public async Task<IActionResult> Update(string id, Analytics updatedAnal)
        {
            var anal = await _analService.GetSingleAsync(id);

            if (anal is null)
            {
                return NotFound();
            }

            updatedAnal.AnalyticsId = anal.AnalyticsId;

            await _analService.UpdateAsync(id, updatedAnal);

            return Ok();
        }


        // DELETE: AnalyticsController
        [HttpDelete("{id:length(24)}")]
        public async Task<IActionResult> Delete(string id)
        {
            var anal = await _analService.GetSingleAsync(id);

            if (anal is null)
            {
                return NotFound();
            }

            await _analService.RemoveAsync(id);

            return NoContent();
        }
    }
}
