using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;
using RecordShelf_WebAPI.Models;
using RecordShelf_WebAPI.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RecordShelf_WebAPI.Controllers
{
    [ApiController]
    [Route("api/auth")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly UsersService _users;
        private readonly IConfiguration _cfg;

        public AuthController(UsersService users, IConfiguration cfg)
        {
            _users = users;
            _cfg = cfg;
        }

        public record RegisterDto(string Username, string Email, string Password);
        public record LoginDto(string EmailOrUsername, string Password);
        public record AuthResponse(string AccessToken, string Username);

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (await _users.ExistsByUsernameAsync(dto.Username) || await _users.ExistsByEmailAsync(dto.Email))
                return Conflict("Username or Email already in use.");


            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };
            await _users.CreateAsync(user);
            return StatusCode(201);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<AuthResponse>> Login(LoginDto dto)
        {
            var user = await _users.FindByEmailOrUsernameAsync(dto.EmailOrUsername);
            if (user is null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                return Unauthorized();

            var token = CreateJwt(user);

            

            return new AuthResponse(token,user.Username);
        }

        private string CreateJwt(User user)
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.UserId!),
                new(JwtRegisteredClaimNames.Email, user.Email),
                new("username", user.Username),
            };

            const string keyString = "Snoop Doggy Dogg Ate A Biscuit and Smoked Some Weed";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _cfg["Jwt:Issuer"],
                audience: _cfg["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(_cfg["Jwt:AccessTokenMinutes"]!)),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
