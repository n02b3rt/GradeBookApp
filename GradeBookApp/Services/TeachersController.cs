using GradeBookApp.Services;
using GradeBookApp.Shared;
using Microsoft.AspNetCore.Mvc;

namespace GradeBookApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TeachersController : ControllerBase
{
    private readonly UserService _userService;

    public TeachersController(UserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Zwraca listę użytkowników z rolą "Teacher".
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetTeachers()
    {
        var teachers = await _userService.GetTeachersAsync();
        return Ok(teachers);
    }
}