using Microsoft.AspNetCore.Mvc;
using RecordShelf_WebApp.Models;
using System.Diagnostics;
using System.Text.Json;

namespace RecordShelf_WebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HttpClient _api;

        public HomeController(ILogger<HomeController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _api = httpClientFactory.CreateClient("RecordShelfApi");
        }

        public async Task<IActionResult> Index(string? searchTerm, string? searchType)
        {
            try
            {
                string url;

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    searchType = string.IsNullOrWhiteSpace(searchType)
                        ? "name"
                        : searchType.ToLowerInvariant();

                    switch (searchType)
                    {
                        case "artist":
                            url = $"api/audio/search/artist?artistSearch={Uri.EscapeDataString(searchTerm)}";
                            break;

                        case "tag":
                            url = $"api/audio/search/tag?tagSearch={Uri.EscapeDataString(searchTerm)}";
                            break;

                        default:
                            url = $"api/audio/search/name?nameSearch={Uri.EscapeDataString(searchTerm)}";
                            break;
                    };
                }
                else
                {
                    url = "api/audio";
                }
                // Call your Web API: GET /api/audio
                var response = await _api.GetAsync(url);
                response.EnsureSuccessStatusCode();


                var json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var audios = JsonSerializer.Deserialize<List<AudioViewModel>>(json, options)
                             ?? new List<AudioViewModel>();

                var analyticsList = await _api.GetFromJsonAsync<List<AnalyticsViewModel>>("api/analytics")
                   ?? new List<AnalyticsViewModel>();

                var analyticsByAudioId = analyticsList
                .Where(a => !string.IsNullOrWhiteSpace(a.AudioId))
                .ToDictionary(a => a.AudioId, a => a);

                var model = audios.Select(a =>
                {
                    analyticsByAudioId.TryGetValue(a.AudioId ?? string.Empty, out var anal);

                    return new AudioDetailsViewModel
                    {
                        Audio = a,
                        Analytics = anal ?? new AnalyticsViewModel
                        {
                            AudioId = a.AudioId ?? string.Empty,
                            ListenCount = 0,
                            LikeCount = 0
                        }
                    };
                }).ToList();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching audio list from API");
                // If something goes wrong, show an empty list
                return View(new List<AudioDetailsViewModel>());
            }
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
