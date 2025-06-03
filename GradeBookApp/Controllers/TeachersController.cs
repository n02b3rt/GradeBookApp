using GradeBookApp.Data.Entities;    
using GradeBookApp.Shared;           
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GradeBookApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]         
    public class TeachersController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public TeachersController(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult<List<UserDto>>> Get()
        {
            // 1) Pobierz wszystkich użytkowników w roli "Teacher"
            var teacherUsers = await _userManager.GetUsersInRoleAsync("Teacher");

            // 2) Zamapuj na UserDto
            var result = new List<UserDto>();
            foreach (var user in teacherUsers)
            {
                // pobierz wszystkie role dla tego usera (zazwyczaj: ["Teacher"], 
                // ale mogą być dodatkowe, jeśli przypisałeś)
                var roles = await _userManager.GetRolesAsync(user);

                result.Add(new UserDto
                {
                    Id             = user.Id,
                    UserName       = user.UserName ?? string.Empty,
                    Email          = user.Email ?? string.Empty,
                    FirstName      = user.FirstName,
                    LastName       = user.LastName,
                    BirthDate      = user.BirthDate,
                    Gender         = user.Gender,
                    Address        = user.Address,
                    PESEL          = user.PESEL,
                    EnrollmentDate = user.EnrollmentDate,
                    HireDate       = user.HireDate,
                    ClassId        = user.ClassId,
                    Roles          = roles.ToList()
                });
            }

            return Ok(result);
        }
    }
}
