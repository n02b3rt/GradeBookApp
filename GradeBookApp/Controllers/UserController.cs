using GradeBookApp.Shared;
using GradeBookApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;    // potrzebne dla ToListAsync()
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GradeBookApp.Api.Controllers
{
    [Route("api/users")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserController(
            UserService userService,
            RoleManager<IdentityRole> roleManager)
        {
            _userService = userService;
            _roleManager = roleManager;
        }

        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<List<UserDto>>> GetUsers()
        {
            var users = await _userService.GetUsersAsync();
            return Ok(users);
        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> GetUser(string id)
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null) 
                return NotFound();
            return Ok(user);
        }

        // GET: api/users/roles
        // Zwraca listę wszystkich ról w systemie, np. ["Admin","Teacher","Student"]
        [HttpGet("roles")]
        [AllowAnonymous]
        public async Task<ActionResult<List<string>>> GetAllRoles()
        {
            var roles = await _roleManager.Roles
                .Select(r => r.Name!)
                .ToListAsync();
            return Ok(roles);
        }

        public class CreateUserRequest
        {
            public UserDto User { get; set; } = null!;
            public string Password { get; set; } = null!;
            public string Role { get; set; } = "Student"; // Domyślna rola
        }

        // POST: api/users
        [HttpPost]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            var result = await _userService.CreateUserAsync(request.User, request.Password, request.Role);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return BadRequest(new { Errors = errors });
            }

            // Pobierz dokładne dane nowo utworzonego użytkownika (żeby mieć wygenerowane Id itp.)
            var createdUser = await _userService.GetUserByIdAsync(request.User.Id);
            return CreatedAtAction(nameof(GetUser), new { id = createdUser?.Id }, createdUser);
        }

        // PUT: api/users/{id}?role={rola}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(
            string id, 
            [FromBody] UserDto dto, 
            [FromQuery] string? role = null)
        {
            var result = await _userService.UpdateUserAsync(id, dto, role);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return BadRequest(new { Errors = errors });
            }

            return Ok();
        }

        // DELETE: api/users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var result = await _userService.DeleteUserAsync(id);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return BadRequest(new { Errors = errors });
            }

            return Ok();
        }
    }
}
