using GradeBookApp.Components.Pages.Admin.ClassList;
using GradeBookApp.Services;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class TeachersController : ControllerBase
{
    private readonly UserService _userService;

    public TeachersController(UserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ClassList.UserDto>>> GetTeachers()
    {
        var teachers = await _userService.GetTeachersAsync();
        return Ok(teachers);
    }
}