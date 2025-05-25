using GradeBookApp.Services;
using GradeBookApp.Shared; 
using Microsoft.AspNetCore.Mvc;

namespace GradeBookApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentClassesController : ControllerBase
{
    private readonly ClassService _classService;

    public StudentClassesController(ClassService classService)
    {
        _classService = classService;
    }

    // // POST: api/studentclasses/{classId}/students
    // [HttpPost("{classId}/students")]
    // public async Task<IActionResult> AssignStudents(int classId, [FromBody] List<string> studentIds)
    // {
    //     var success = await _classService.AssignStudentsToClass(classId, studentIds);
    //     if (!success) return NotFound();
    //     return NoContent();
    // }

    // GET: api/studentclasses/{classId}/students
    [HttpGet("{classId}/students")]
    public async Task<ActionResult<List<string>>> GetStudents(int classId)
    {
        var cls = await _classService.GetClassByIdAsync(classId);
        if (cls == null) return NotFound();

        var studentIds = cls.StudentIds ?? new List<string>();
        return Ok(studentIds);
    }
}