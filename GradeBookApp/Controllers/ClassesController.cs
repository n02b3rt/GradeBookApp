using GradeBookApp.Shared;
using GradeBookApp.Services;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GradeBookApp.Filters;

namespace GradeBookApp.Controllers
{
    [RequireRole("Admin", "Teacher")]
    [ApiController]
    [Route("api/classes")]
    public class ClassesController : ControllerBase
    {
        private readonly ClassService _classService;

        public ClassesController(ClassService classService)
        {
            _classService = classService;
        }

        // GET: api/classes
        [HttpGet]
        public async Task<ActionResult<List<ClassDto>>> GetClasses()
        {
            var classes = await _classService.GetClassesAsync();
            return Ok(classes);
        }

        // GET: api/classes/count
        [HttpGet("count")]
        public async Task<IActionResult> GetClassesCount()
        {
            var classes = await _classService.GetClassesAsync();
            int count = classes?.Count ?? 0;
            return Ok(count);
        }

        // GET: api/classes/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ClassDto?>> GetClass(int id)
        {
            var cls = await _classService.GetClassByIdAsync(id);
            if (cls == null) return NotFound();
            return Ok(cls);
        }

        // POST: api/classes
        [HttpPost]
        public async Task<ActionResult<ClassDto>> CreateClassAsync([FromBody] ClassDto newClassDto)
        {
            try
            {
                var createdClass = await _classService.CreateClassAsync(newClassDto);
                return CreatedAtAction(nameof(GetClass), new { id = createdClass.Id }, createdClass);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // PUT: api/classes/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateClass(int id, [FromBody] ClassDto updatedClass)
        {
            if (id != updatedClass.Id)
                return BadRequest("Id mismatch");

            var result = await _classService.UpdateClassAsync(updatedClass);
            if (!result) return NotFound();

            return NoContent();
        }

        // DELETE: api/classes/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClass(int id)
        {
            var result = await _classService.DeleteClassAsync(id);
            if (!result) return NotFound();

            return NoContent();
        }
        
        // GET: api/classes/simple
        [HttpGet("simple")]
        public async Task<ActionResult<List<object>>> GetSimple()
        {
            var classes = await _classService.GetClassesAsync();
            return Ok(classes.Select(c => new { c.Id, c.Name }));
        }
    }
}
