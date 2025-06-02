using GradeBookApp.Services;
using GradeBookApp.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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

    // GET: api/subjects
    [HttpGet]
    public async Task<ActionResult<List<SubjectDto>>> GetAll()
    {
        var subjects = await _subjectService.GetSubjectsAsync();
        return Ok(subjects);
    }

    // GET: api/subjects/count
    [HttpGet("count")]
    public async Task<IActionResult> GetSubjectsCount()
    {
        var subjects = await _subjectService.GetSubjectsAsync();
        int count = subjects?.Count ?? 0;
        return Ok(count);
    }

    // GET: api/subjects/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<SubjectDto>> GetById(string id)
    {
        var subject = await _subjectService.GetSubjectByIdAsync(id);
        if (subject == null) return NotFound();
        return Ok(subject);
    }

    // POST: api/subjects
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
        catch (DbUpdateException)
        {
            return BadRequest("❗ Wystąpił błąd bazy danych – być może przedmiot już istnieje.");
        }
    }

    // PUT: api/subjects/{id}
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

    // DELETE: api/subjects/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _subjectService.DeleteSubjectAsync(id);
        if (!deleted) return NotFound();

        return NoContent();
    }
}
