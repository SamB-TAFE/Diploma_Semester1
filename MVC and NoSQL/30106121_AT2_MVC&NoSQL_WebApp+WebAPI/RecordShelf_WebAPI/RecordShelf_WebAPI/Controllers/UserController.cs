using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RecordShelf_WebAPI.Models;
using RecordShelf_WebAPI.Services;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace RecordShelf_WebAPI.Controllers
{
    [ApiController]
    [Route("api/user")]
    [Produces("application/json")]
    [Authorize]

    public class UserController : Controller
    {

        private readonly UsersService _usersService;

        public UserController(UsersService usersService) 
        {
            _usersService = usersService;
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        // GET api/user/me
        [HttpGet("me")]
        public async Task<ActionResult<User?>> GetMe()
        {
            Console.WriteLine("GetMe hit. Authenticated = " + (User.Identity?.IsAuthenticated ?? false));

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            Console.WriteLine("UserId from claims = " + (userId ?? "<null>"));

            //var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            User user = await _usersService.GetSingleAsync(userId);
            if (user == null)
                return NotFound();

            return user;
        }


        // PUT api/user/me
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateUserRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var user = await _usersService.GetSingleAsync(userId);
            if (user == null)
                return NotFound();

            user.Username = request.Username;
            user.Email = request.Email;

            await _usersService.UpdateAsync(userId, user);
            return NoContent();
        }


        // DELETE: api/user/me
        [HttpDelete("me")]
        public async Task<IActionResult> DeleteMe()
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized();

            var user = await _usersService.GetSingleAsync(userId);
            if (user == null)
                return NotFound();

            // TODO (later): also delete user's audios + analytics
            await _usersService.RemoveAsync(userId);
            return NoContent();
        }

        public class UpdateUserRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
        }
    }
}
