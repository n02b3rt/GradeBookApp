using GradeBookApp.Services;
using GradeBookApp.Shared;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GradeBookApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentClassesController : ControllerBase
{
    private readonly ClassService _classService;
    private readonly StudentDataService _studentDataService;

    public StudentClassesController(ClassService classService,StudentDataService studentDataService)
    {
        _classService = classService;
        _studentDataService = studentDataService;
    }

    // GET: api/studentclasses/{classId}/students
    [HttpGet("{classId}/students")]
    public async Task<ActionResult<List<string>>> GetStudents(int classId)
    {
        var cls = await _classService.GetClassByIdAsync(classId);
        if (cls == null) return NotFound();

        var studentIds = cls.StudentIds ?? new List<string>();
        return Ok(studentIds);
    }


    [HttpGet("{studentId}/grades")]
    public async Task<ActionResult<List<GradeDto>>> GetGradesForStudent(string studentId)
    {
        var grades = await _studentDataService.GetGradesByStudentIdAsync(studentId);
        return Ok(grades);
    }

    [HttpGet("{studentId}/attendance")]
    public async Task<ActionResult<List<AttendanceDto>>> GetAttendanceForStudent(string studentId)
    {
        var attendance = await _studentDataService.GetAttendanceByStudentIdAsync(studentId);
        return Ok(attendance);
    }

}