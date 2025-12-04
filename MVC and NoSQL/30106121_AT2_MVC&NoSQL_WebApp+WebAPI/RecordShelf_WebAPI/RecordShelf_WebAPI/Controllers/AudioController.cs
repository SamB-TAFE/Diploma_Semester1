using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecordShelf_WebAPI.Models;
using RecordShelf_WebAPI.Services;
using System.Security.Claims;

namespace RecordShelf_WebAPI.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("2.0")]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AudioController : Controller
    {
        private readonly AudiosService _audiosService;
        private readonly AnalyticsService _analService;

        public AudioController(AudiosService audiosService, AnalyticsService analyticsService)
        {
            _audiosService = audiosService;
            _analService = analyticsService;
        }


        // GET_ALL v1: AudioController
        [HttpGet]
        [AllowAnonymous]
        [MapToApiVersion("1.0")]
        public async Task<List<Audio>> GetAll()
        {
            return await _audiosService.GetAllAsync();
        }

        // GET_ALL v2: AudioController
        [HttpGet]
        [AllowAnonymous]
        [MapToApiVersion("2.0")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> GetAllv2()
        {
            var audios = await _audiosService.GetAllAsync();

            return new
            {
                version = "2.0",
                count = audios.Count,
                message = "V2 audio endpoint hit!",
                data = audios
            };
        }

        // GET_SINGLE v1: AudioController
        [HttpGet("{id:length(24)}")]
        [AllowAnonymous]
        [MapToApiVersion("1.0")]
        public async Task<ActionResult<Audio>> GetbyID(string id)
        {
            var audio = await _audiosService.GetSingleAsync(id);

            if (audio is null)
            {
                return NotFound();
            }

            return audio;
        }

        // GET_SINGLE v2: AudioController
        [HttpGet("{id:length(24)}")]
        [AllowAnonymous]
        [MapToApiVersion("2.0")]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<object> GetbyIDv2(string id)
        {
            var audio = await _audiosService.GetSingleAsync(id);

            if (audio is null)
            {
                return NotFound();
            }

            return new
            {
                version = "2.0",
                message = "V2 single audio endpoint hit!",
                data = audio
            };
        }



        // GET_ALL_FROM_USER: AudioController
        [HttpGet("by-user/{userId:length(24)}")]
        [AllowAnonymous]
        public async Task<List<Audio>> GetUserAudios(string userId)
        {
            return await _audiosService.GetUsersAudiosAsync(userId);
        }

        // CREATE: AudioController
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create(Audio newAudio)
        {
            Console.WriteLine("Create Audio hit");
            Console.WriteLine($"ModelState valid = {ModelState.IsValid}");

            if (!ModelState.IsValid)
            {
                var errors = string.Join(" | ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage));

                Console.WriteLine("ModelState errors: " + errors);
                return BadRequest(ModelState);
            }

            await _audiosService.CreateAsync(newAudio);

            return CreatedAtAction(nameof(GetbyID), new { id = newAudio.AudioId }, newAudio);
        }

        // UPDATE: AudioController

        [HttpPut("{id:length(24)}")]
        [Authorize]
        public async Task<IActionResult> Update(string id, Audio updatedAudio)
        {
            var audio = await _audiosService.GetSingleAsync(id);

            if (audio is null)
            {
                return NotFound();
            }

            updatedAudio.AudioId = audio.AudioId;

            await _audiosService.UpdateAsync(id, updatedAudio);

            return NoContent();
        }


        // DELETE: AudioController
        [HttpDelete("{id:length(24)}")]
        [Authorize]
        public async Task<IActionResult> Delete(string id)
        {
            var audio = await _audiosService.GetSingleAsync(id);

            if (audio is null)
            {
                return NotFound();
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != audio.UserId)
                return Forbid();

            await _audiosService.RemoveAsync(id);
            await _analService.RemovebyAudioIdAsync(id);

            return NoContent();
        }

        // SEARCH ACTIONS //

        // SEARCH_BY_NAME: AudioController
        [HttpGet("search/name")]
        [AllowAnonymous]
        public async Task<List<Audio>> SearchByName([FromQuery] string? nameSearch = null)
        {
            return await _audiosService.SearchbyNameAsync(nameSearch);
        }

        // SEARCH_BY_ARTIST: AudioController
        [HttpGet("search/artist")]
        [AllowAnonymous]
        public async Task<List<Audio>> SearchByArtist([FromQuery] string? artistSearch = null)
        {
            return await _audiosService.SearchbyArtistAsync(artistSearch);
        }

        // SEARCH_BY_TAGS: AudioController
        [HttpGet("search/tag")]
        [AllowAnonymous]
        public async Task<List<Audio>> SearchByTag([FromQuery] string? tagSearch = null)
        {
            return await _audiosService.SearchbyTagsAsync(tagSearch);
        }

        // SEARCH_USERS_BY_NAME: AudioController
        [HttpGet("search/user-name")]
        [AllowAnonymous]
        public async Task<List<Audio>> SearchUserAudiosByName(string userId, [FromQuery] string? nameSearch = null)
        {
            return await _audiosService.SearchUsersAudiosbyNameAsync(userId, nameSearch);
        }

        // SEARCH_USERS_BY_TAGS: AudioController
        [HttpGet("search/user-tag")]
        [AllowAnonymous]
        public async Task<List<Audio>> SearchUserAudiosByTag(string userId, [FromQuery] string? tagSearch = null)
        {
            return await _audiosService.SearchUsersAudiosbyTagAsync(userId, tagSearch);
        }
    }
}
