using GradeBookApp.Data.Entities;
using GradeBookApp.Filters;
using GradeBookApp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GradeBookApp.Controllers
{
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

        // GET: api/teachersubjects/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<TeacherSubject>> GetById(string id)
        {
            var entity = await _teacherSubjectService.GetByIdAsync(id);
            if (entity == null)
                return NotFound();
            return Ok(entity);
        }

        // POST: api/teachersubjects
        [HttpPost]
        public async Task<IActionResult> AssignTeacher([FromBody] AssignTeacherSubjectDto dto)
        {
            // 1. Walidacja podstawowa
            if (string.IsNullOrWhiteSpace(dto.SubjectId) || string.IsNullOrWhiteSpace(dto.TeacherId))
                return BadRequest("SubjectId i TeacherId muszą być wypełnione.");

            // 2. Sprawdzenie istnienia przedmiotu
            if (!await _teacherSubjectService.SubjectExistsAsync(dto.SubjectId))
                return NotFound($"Nie znaleziono przedmiotu o ID = {dto.SubjectId}.");

            // 3. Sprawdzenie istnienia nauczyciela
            if (!await _teacherSubjectService.TeacherExistsAsync(dto.TeacherId))
                return NotFound($"Nie znaleziono nauczyciela o ID = {dto.TeacherId}.");

            // 4. Sprawdzenie duplikatu przypisania
            if (await _teacherSubjectService.ExistsAsync(dto.SubjectId, dto.TeacherId, dto.ClassId))
                return Conflict("Przypisanie o tych samych wartościach już istnieje.");

            // 5. Utworzenie przypisania
            var newId = await _teacherSubjectService.AssignTeacherToSubjectAndClass(dto.TeacherId, dto.SubjectId, dto.ClassId);
            if (string.IsNullOrEmpty(newId))
                return StatusCode(500, "Nieoczekiwany błąd przy tworzeniu przypisania.");

            // 6. Zwrócenie 201 Created z lokalizacją do GET by id
            return CreatedAtAction(
                nameof(GetById),
                new { id = newId },
                dto
            );
        }

        // DELETE: api/teachersubjects/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveAssignment(string id)
        {
            var success = await _teacherSubjectService.RemoveTeacherSubjectAssignment(id);
            if (!success)
                return NotFound();
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
}
