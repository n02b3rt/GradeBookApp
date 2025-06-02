// Controllers/TeacherActionsController.cs

using GradeBookApp.Filters;
using GradeBookApp.Shared;
using GradeBookApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace GradeBookApp.Controllers;

[RequireRole("Admin", "Teacher")]
[ApiController]
[Route("api/teacher")]
public class TeacherActionsController : ControllerBase
{
    private readonly TeacherActionsService _teacherService;
    private readonly TeacherActionsService _teacherActionsService;
    
    public TeacherActionsController(TeacherActionsService teacherService, TeacherActionsService teacherActionsService)
    {
        _teacherService = teacherService;
        _teacherActionsService = teacherActionsService;
    }

    [HttpPost("attendance")]
    public async Task<IActionResult> AddAttendance([FromBody] AttendanceDto dto)
    {
        await _teacherService.AddAttendanceAsync(dto);
        return Ok();
    }

    [HttpPost("attendance/bulk")]
    public async Task<IActionResult> AddBulkAttendance([FromBody] List<AttendanceDto> dtos)
    {
        await _teacherService.AddBulkAttendanceAsync(dtos);
        return Ok();
    }

    [HttpPost("grades")]
    public async Task<IActionResult> AddGrade([FromBody] GradeDto dto)
    {
        await _teacherService.AddGradeAsync(dto);
        return Ok();
    }

    [HttpPost("grades/bulk")]
    public async Task<IActionResult> AddBulkGrades([FromBody] List<GradeDto> dtos)
    {
        await _teacherService.AddBulkGradesAsync(dtos);
        return Ok();
    }

    [HttpGet("classes/{classId}/students")]
    public async Task<ActionResult<List<UserDto>>> GetStudentsInClass(int classId)
    {
        var students = await _teacherService.GetStudentsInClassAsync(classId);
        return Ok(students);
    }

    [HttpGet("{teacherId}/subjects")]
    public async Task<ActionResult<List<TeacherSubjectDto>>> GetSubjectsForTeacher(string teacherId)
    {
        var subjects = await _teacherService.GetSubjectsForTeacherAsync(teacherId);
        return Ok(subjects);
    }

    [HttpGet("{teacherId}/classes")]
    public async Task<ActionResult<List<ClassDto>>> GetClassesForTeacher(string teacherId)
    {
        var classes = await _teacherService.GetClassesForTeacherAsync(teacherId);
        return Ok(classes);
    }
    
    [HttpGet("{classId}/grades/{subjectId}")]
    public async Task<ActionResult<Dictionary<string, List<GradeDto>>>> GetGradesForClassSubject(int classId, string subjectId)
    {
        var teacherId = User.FindFirst("sub")?.Value;
        if (teacherId == null)
            return Unauthorized();

        var result = await _teacherActionsService.GetGradesForClassAndSubjectByTeacherAsync(classId, subjectId, teacherId);
        return Ok(result);
    }

}
