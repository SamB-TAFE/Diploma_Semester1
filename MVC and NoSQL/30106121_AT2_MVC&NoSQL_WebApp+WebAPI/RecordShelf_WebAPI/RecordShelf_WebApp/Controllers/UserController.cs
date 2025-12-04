using Microsoft.AspNetCore.Mvc;
using RecordShelf_WebApp.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using TagLib;

namespace RecordShelf_WebApp.Controllers
{
    public class UserController : Controller
    {
        private readonly HttpClient _api;
        private readonly ILogger<UserController> _logger;
        private readonly IWebHostEnvironment _env;

        public UserController(IHttpClientFactory httpClientFactory, ILogger<UserController> logger, IWebHostEnvironment env)
        {
            _api = httpClientFactory.CreateClient("RecordShelfApi");
            _logger = logger;
            _env = env;
        }

        // Helper: get JWT from session, or redirect to login
        private string? GetTokenOrRedirect(out IActionResult? redirectResult)
        {
            string? token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrWhiteSpace(token))
            {
                redirectResult = RedirectToAction("Login", "Account");
                return null;
            }

            redirectResult = null;
            return token;
        }

        private async Task<UserMeResponse?> GetCurrentUserAsync(string token)
        {

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "api/user/me");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                _logger.LogInformation("Calling api/user/me with token: {Token}", token);

                var response = await _api.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("GetCurrentUserAsync failed: {StatusCode} - {Body}", response.StatusCode, body);

                    TempData["UserApiError"] = $"api/user/me failed: {(int)response.StatusCode} {response.StatusCode}";
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("GetCurrentUserAsync JSON: {Json}", json);

                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var user = JsonSerializer.Deserialize<UserMeResponse>(json, options);

                _logger.LogInformation("Deserialized user: UserId={UserId}, Username={Username}, Email={Email}",
                    user?.UserId, user?.Username, user?.Email);

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling api/user/me");
                return null;
            }
        }

        public async Task<IActionResult> MyProjects(string? searchTerm, string? searchType)
        {
            var token = GetTokenOrRedirect(out var redirect);
            if (token == null) return redirect!;

            var user = await GetCurrentUserAsync(token);
            if (user == null)
            {
                // If we can't get the user, force re-login
                return View(new List<AudioViewModel>());
            }

            // Call GET api/audio/by-user/{userId}
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

                        case "tag":
                            url = $"api/audio/search/user-tag?userId={user.UserId}&tagSearch={Uri.EscapeDataString(searchTerm)}";
                            break;

                        default:
                            url = $"api/audio/search/user-name?userId={user.UserId}&nameSearch={Uri.EscapeDataString(searchTerm)}";
                            break;
                    };

                    ViewBag.SearchTerm = searchTerm;
                    ViewBag.SearchType = searchType;
                }
                else
                {
                    url = $"api/audio/by-user/{user.UserId}";
                    ViewBag.SearchTerm = "";
                    ViewBag.SearchType = "name";
                }
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _api.SendAsync(request);
                response.EnsureSuccessStatusCode();

                var json = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

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

                ViewBag.Username = HttpContext.Session.GetString("Username"); ; // just for display in the view

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user audios");
                ViewBag.Username = user.Username;
                return View(new List<AudioDetailsViewModel>());
            }
        }

        // GET: /User/AccountSettings
        [HttpGet]
        public async Task<IActionResult> AccountSettings()
        {

            var token = GetTokenOrRedirect(out var redirect);
            if (token == null) return redirect!;

            var user = await GetCurrentUserAsync(token);
            if (user == null)
            {
                TempData["UserApiError"] = "Could not load your account; please log in again.";
                return RedirectToAction("Login", "Account");
            }

            var model = new AccountSettingsViewModel
            {
                Username = user.Username,
                Email = user.Email
            };

            return View(model);
        }

        // POST: /User/AccountSettings
        [HttpPost]
        public async Task<IActionResult> AccountSettings(AccountSettingsViewModel model)
        {
            var token = GetTokenOrRedirect(out var redirect);
            if (token == null) return redirect!;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Put, "api/user/me");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var payload = new
                {
                    model.Username,
                    model.Email
                };

                var jsonBody = JsonSerializer.Serialize(payload);
                request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

                var response = await _api.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(string.Empty, "Failed to update account settings.");
                    return View(model);
                }

                ViewBag.SuccessMessage = "Account updated successfully.";
                HttpContext.Session.SetString("Username", model.Username);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating account settings");
                ModelState.AddModelError(string.Empty, "An error occurred while updating your account.");
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> UploadAudio(string audioTitle, string artist, string tags, string filePath, int durationSeconds)
        {

            // Make sure user is logged in
            var token = GetTokenOrRedirect(out var redirect);
            if (token == null) return redirect!;

            if (String.IsNullOrWhiteSpace(filePath))
            {
                // For now just reload the page; you could add proper error messages later
                TempData["UploadError"] = "Please select a file.";
                return RedirectToAction("MyProjects");
            }

            try
            {
                // Get current user (needed for UserId)
                var user = await GetCurrentUserAsync(token);

                if (user == null)
                {
                    TempData["UploadError"] = "Could not identify user.";
                    return RedirectToAction("MyProjects");
                }

                // Convert tags string --> List<string>
                var tagList = string.IsNullOrWhiteSpace(tags)
                    ? new List<string>()
                    : tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                          .Select(t => t.Trim())
                          .ToList();

                var relativePath = filePath.TrimStart('/');
                var fullPath = Path.Combine(_env.WebRootPath, relativePath);

                if (System.IO.File.Exists(fullPath))
                {
                    try
                    {
                        var tfile = TagLib.File.Create(fullPath);
                        durationSeconds = (int)tfile.Properties.Duration.TotalSeconds;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to read duration for {Path}", fullPath);
                        // leave durationSeconds = 0
                    }
                }

                // Build Audio object to send to API
                var audioObject = new
                {
                    AudioTitle = audioTitle,
                    Artist = user.Username,
                    Tags = tagList,
                    UserId = user.UserId,
                    FilePath = filePath,      // local file path
                    UploadDate = DateTime.UtcNow,
                    DurationSeconds = durationSeconds,
                };

                var request = new HttpRequestMessage(HttpMethod.Post, "api/audio");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                request.Content = new StringContent(
                    JsonSerializer.Serialize(audioObject),
                    Encoding.UTF8,
                    "application/json");

                var response = await _api.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("UploadAudio failed: {StatusCode} - {Body}",
                        response.StatusCode, body);

                    TempData["UploadError"] = $"Failed to upload audio. API said: {response.StatusCode}";
                    return RedirectToAction("MyProjects");
                }

                var createdAudio = await response.Content.ReadFromJsonAsync<AudioViewModel>();

                if (createdAudio == null || string.IsNullOrWhiteSpace(createdAudio.AudioId))
                {
                    _logger.LogWarning("Audio upload succeeded but could not read created audio or AudioId.");
                    TempData["UploadSuccess"] = "Audio added, but analytics not initialised.";
                    return RedirectToAction("MyProjects");
                }

                _logger.LogInformation("Created audio from API: AudioId={AudioId}, Title={Title}",
                    createdAudio.AudioId, createdAudio.AudioTitle);

                var analyticsObject = new
                {
                    UserId = user.UserId,
                    AudioId = createdAudio.AudioId,
                    ListenCount = 0,
                    LikeCount = 0,
                };

                var analyticsResponse = await _api.PostAsJsonAsync("api/analytics", analyticsObject);

                if (!analyticsResponse.IsSuccessStatusCode)
                {
                    var body = await analyticsResponse.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to create analytics for audio {AudioId}. Status: {StatusCode}, Body: {Body}",
                        createdAudio.AudioId,
                        analyticsResponse.StatusCode,
                        body);

                    TempData["UploadSuccess"] = "Audio added, but analytics not initialised.";
                    return RedirectToAction("MyProjects");
                }

                TempData["UploadSuccess"] = "Audio added!";
                return RedirectToAction("MyProjects");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Upload failed");
                TempData["UploadError"] = "Unexpected error.";
                return RedirectToAction("MyProjects");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAudio(string audioId)
        {
            var token = GetTokenOrRedirect(out var redirect);
            if (token == null) return redirect!;

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Delete, $"api/audio/{audioId}");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var response = await _api.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    TempData["UserApiError"] = $"Failed to delete audio. API said: {response.StatusCode}";
                    return RedirectToAction("MyProjects");
                }

                TempData["UploadSuccess"] = "Audio deleted successfully.";
                return RedirectToAction("MyProjects");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting audio");
                TempData["UserApiError"] = "Unexpected error deleting audio.";
                return RedirectToAction("MyProjects");
            }
        }

        private class UserMeResponse
        {
            public string? UserId { get; set; }
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }
    }
}
