using GradeBookApp.Services;
using GradeBookApp.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GradeBookApp.Controllers;

[ApiController]
[Route("api/subjects")]
public class SubjectsController : ControllerBase
{
    private readonly SubjectService _subjectService;

    public SubjectsController(SubjectService subjectService)
    {
        _subjectService = subjectService;
    }

    [HttpGet]
    public async Task<ActionResult<List<SubjectDto>>> GetAll()
    {
        var subjects = await _subjectService.GetSubjectsAsync();
        return Ok(subjects);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SubjectDto>> GetById(string id)
    {
        var subject = await _subjectService.GetSubjectByIdAsync(id);
        if (subject == null) return NotFound();
        return Ok(subject);
    }

    [HttpPost]
    public async Task<ActionResult<SubjectDto>> Create([FromBody] SubjectDto dto)
    {
        try
        {
            var created = await _subjectService.CreateSubjectAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest($"❗ {ex.Message}");
        }
        catch (DbUpdateException dbEx)
        {
            return BadRequest("❗ Wystąpił błąd bazy danych – być może przedmiot już istnieje.");
        }
    }


    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] SubjectDto dto)
    {
        if (id != dto.Id) return BadRequest("ID nie zgadza się z obiektem.");

        try
        {
            var updated = await _subjectService.UpdateSubjectAsync(dto);
            if (!updated) return NotFound();

            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest($"❗ {ex.Message}");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _subjectService.DeleteSubjectAsync(id);
        if (!deleted) return NotFound();

        return NoContent();
    }
}