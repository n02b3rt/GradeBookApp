using GradeBookApp.Shared;
using GradeBookApp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GradeBookApp.Controllers
{
    [ApiController]
    [Route("api/classes")]
    public class ClassesController : ControllerBase
    {
        private readonly ClassService _classService;

        public ClassesController(ClassService classService)
        {
            _classService = classService;
        }

        [HttpGet]
        public async Task<ActionResult<List<ClassDto>>> GetClasses()
        {
            var classes = await _classService.GetClassesAsync();
            return Ok(classes);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ClassDto?>> GetClass(int id)
        {
            var cls = await _classService.GetClassByIdAsync(id);
            if (cls == null) return NotFound();
            return Ok(cls);
        }

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

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateClass(int id, [FromBody] ClassDto updatedClass)
        {
            if (id != updatedClass.Id)
                return BadRequest("Id mismatch");

            var result = await _classService.UpdateClassAsync(updatedClass);
            if (!result) return NotFound();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClass(int id)
        {
            var result = await _classService.DeleteClassAsync(id);
            if (!result) return NotFound();

            return NoContent();
        }
    }
}
