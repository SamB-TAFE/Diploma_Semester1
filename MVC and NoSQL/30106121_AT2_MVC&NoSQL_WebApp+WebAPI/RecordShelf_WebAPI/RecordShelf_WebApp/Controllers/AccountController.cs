using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using RecordShelf_WebApp.Models;

namespace RecordShelf_WebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly HttpClient _api;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IHttpClientFactory httpClientFactory, ILogger<AccountController> logger)
        {
            _api = httpClientFactory.CreateClient("RecordShelfApi");
            _logger = logger;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var response = await _api.PostAsJsonAsync("api/auth/register", new
                {
                    Username = model.Username,
                    Email = model.Email,
                    Password = model.Password
                });

                _logger.LogInformation("Register POST to {Url} returned {StatusCode}",
                response.RequestMessage?.RequestUri,
                response.StatusCode);

                if (response.IsSuccessStatusCode)
                {
                    // On success, redirect to Login page
                    return RedirectToAction("Login");
                }

                var errorText = await response.Content.ReadAsStringAsync();
                ModelState.AddModelError(string.Empty, $"Registration failed: {errorText}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                ModelState.AddModelError(string.Empty, "An error occurred while registering.");
            }

            return View(model);
        }


        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var response = await _api.PostAsJsonAsync("api/auth/login", new
                {
                    EmailOrUsername = model.EmailOrUsername,
                    Password = model.Password
                });

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return View(model);
                }

                var json = await response.Content.ReadAsStringAsync();

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var authResponse = JsonSerializer.Deserialize<AuthResponseDto>(json, options);

                if (authResponse == null || string.IsNullOrWhiteSpace(authResponse.AccessToken))
                {
                    ModelState.AddModelError(string.Empty, "Invalid response from server.");
                    return View(model);
                }

                HttpContext.Session.SetString("JwtToken", authResponse.AccessToken);
                HttpContext.Session.SetString("Username", authResponse.Username);

                // TODO: later redirect to "My Projects" page; for now, go home
                return RedirectToAction("MyProjects", "User");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                ModelState.AddModelError(string.Empty, $"Login error: {ex.Message}");
                return View(model);
            }
        }

        private class AuthResponseDto
        {
            public string AccessToken { get; set; } = string.Empty;

            public string Username { get; set; } = string.Empty;
        }

        // GET: /Account/Logout
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("JwtToken");
            HttpContext.Session.Remove("Username");
            return RedirectToAction("Index", "Home");
        }

    }
}
