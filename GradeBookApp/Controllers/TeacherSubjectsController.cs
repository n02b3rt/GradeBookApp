using GradeBookApp.Data.Entities;
using GradeBookApp.Filters;
using GradeBookApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace GradeBookApp.Controllers;

[RequireRole("Admin", "Teacher")]
[ApiController]
[Route("api/[controller]")]
public class TeacherSubjectsController : ControllerBase
{
    private readonly TeacherSubjectService _teacherSubjectService;

    public TeacherSubjectsController(TeacherSubjectService teacherSubjectService)
    {
        _teacherSubjectService = teacherSubjectService;
    }

    // GET: api/teachersubjects
    [HttpGet]
    public async Task<ActionResult<List<TeacherSubject>>> GetTeacherSubjects()
    {
        var list = await _teacherSubjectService.GetTeacherSubjectsAsync();
        return Ok(list);
    }

    // POST: api/teachersubjects
    [HttpPost]
    public async Task<IActionResult> AssignTeacher([FromBody] AssignTeacherSubjectDto dto)
    {
        var success = await _teacherSubjectService.AssignTeacherToSubjectAndClass(dto.TeacherId, dto.SubjectId, dto.ClassId);
        if (!success) return Conflict("Assignment already exists");
        return Ok();
    }

    // DELETE: api/teachersubjects/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> RemoveAssignment(string id)
    {
        var success = await _teacherSubjectService.RemoveTeacherSubjectAssignment(id);
        if (!success) return NotFound();
        return NoContent();
    }
}

// DTO do przypisywania nauczyciela do przedmiotu i klasy
public class AssignTeacherSubjectDto
{
    public string TeacherId { get; set; } = string.Empty;
    public string SubjectId { get; set; } = string.Empty;
    public int ClassId { get; set; }
}