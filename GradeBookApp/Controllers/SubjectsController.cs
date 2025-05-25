using GradeBookApp.Data;
using GradeBookApp.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GradeBookApp.Controllers
{
    [ApiController]
    [Route("api/subjects")]  // Możesz na sztywno ustawić endpoint
    public class SubjectsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SubjectsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<Subject>>> GetSubjects()
        {
            var subjects = await _context.Subjects.ToListAsync();
            return Ok(subjects);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Subject?>> GetSubject(string id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null) return NotFound();
            return Ok(subject);
        }

        [HttpPost]
        public async Task<ActionResult<Subject>> CreateSubject(Subject newSubject)
        {
            _context.Subjects.Add(newSubject);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(GetSubject), new { id = newSubject.Id }, newSubject);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSubject(string id, Subject updatedSubject)
        {
            if (id != updatedSubject.Id) return BadRequest("Id mismatch");

            var existing = await _context.Subjects.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Name = updatedSubject.Name;
            existing.ShortName = updatedSubject.ShortName;
            // ... inne pola

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSubject(string id)
        {
            var subject = await _context.Subjects.FindAsync(id);
            if (subject == null) return NotFound();

            _context.Subjects.Remove(subject);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
