
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecordShelf_WebApp.Models;
using System.Threading.Tasks;

namespace RecordShelf_WebApp.Controllers
{
    public class AudioController : Controller
    {
        private readonly ILogger<AudioController> _logger;
        private readonly HttpClient _api;

        public AudioController(ILogger<AudioController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _api = httpClientFactory.CreateClient("RecordShelfApi");
        }


        public async Task<IActionResult> Listen(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Audio id is required.");

            AudioViewModel? audio;
            AnalyticsViewModel? analytics;


            var response = await _api.GetAsync($"api/audio/{id}");
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return NotFound();

                return StatusCode((int)response.StatusCode, "Error fetching audio from API.");
            }

            audio = await response.Content.ReadFromJsonAsync<AudioViewModel>();
            var analResponse = await _api.GetAsync($"api/analytics/by-audio/{id}");
            if (!analResponse.IsSuccessStatusCode)
            {
                if (analResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return NotFound();

                return StatusCode((int)analResponse.StatusCode, "Error fetching analytics from API.");
            }
            analytics = await analResponse.Content.ReadFromJsonAsync<AnalyticsViewModel>();

            if (analytics != null)
            {
                analytics.ListenCount++;

                var updateResponse =
                    await _api.PutAsJsonAsync($"api/analytics/{analytics.AnalyticsId}", analytics);

                if (!updateResponse.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to increment listen count for audio {AudioId}. Status: {StatusCode}",
                        id, updateResponse.StatusCode);
                    // Don't break the page if analytics fails
                }
            }

            if (audio == null || analytics == null)
                return NotFound();

            var vm = new AudioDetailsViewModel
            {
                Audio = audio,
                Analytics = analytics
            };

            return View(vm); // Views/Audio/Listen.cshtml
        }

        [HttpPost]
        public async Task<IActionResult> Like(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return BadRequest("Audio id required.");

            AnalyticsViewModel? analytics;

            var analResponse = await _api.GetAsync($"api/analytics/by-audio/{id}");
            if (analResponse.IsSuccessStatusCode)
            {
                analytics = await analResponse.Content.ReadFromJsonAsync<AnalyticsViewModel>();
                if (analytics == null)
                    return StatusCode(500, "Failed to deserialize analytics from API.");

                analytics.LikeCount++;

                var updateResponse =
                    await _api.PutAsJsonAsync($"api/analytics/{analytics.AnalyticsId}", analytics);

                if (!updateResponse.IsSuccessStatusCode)
                    return StatusCode((int)updateResponse.StatusCode, "Error updating analytics.");

            }
            else
            {
                return StatusCode((int)analResponse.StatusCode, "Error fetching analytics.");
            }

            return Json(new
            {
                success = true,
                newLikeCount = analytics!.LikeCount
            });
        }
    }
}
