using GradeBookApp.Data.Entities;
using GradeBookApp.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GradeBookApp.Data;

namespace GradeBookApp.Services
{
    public class UserService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        
        public UserService(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        // Pobierz listę użytkowników
        public async Task<List<UserDto>> GetUsersAsync()
        {
            var users = await _userManager.Users.ToListAsync();

            return users.Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName!,
                Email = u.Email!,
                FirstName = u.FirstName!,
                LastName = u.LastName!,
                BirthDate = u.BirthDate,
                Gender = u.Gender,
                Address = u.Address,
                PESEL = u.PESEL,
                EnrollmentDate = u.EnrollmentDate,
                HireDate = u.HireDate,
                ClassId = u.ClassId
            }).ToList();
        }

        // Pobierz użytkownika po Id
        public async Task<UserDto?> GetUserByIdAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                FirstName = user.FirstName!,
                LastName = user.LastName!,
                BirthDate = user.BirthDate,
                Gender = user.Gender,
                Address = user.Address,
                PESEL = user.PESEL,
                EnrollmentDate = user.EnrollmentDate,
                HireDate = user.HireDate,
                ClassId = user.ClassId
            };
        }

        // Utwórz nowego użytkownika (z hasłem i domyślną rolą np. "Student")
        public async Task<IdentityResult> CreateUserAsync(UserDto userDto, string password, string role = "Student")
        {
            // Sprawdź poprawność imienia i nazwiska
            if (string.IsNullOrWhiteSpace(userDto.FirstName) || string.IsNullOrWhiteSpace(userDto.LastName))
            {
                return IdentityResult.Failed(new IdentityError { Description = "Imię i nazwisko są wymagane do wygenerowania loginu i emaila." });
            }

            // Generowanie UserName i Email
            string first = new string(userDto.FirstName.Where(char.IsLetterOrDigit).ToArray());
            string last = new string(userDto.LastName.Where(char.IsLetterOrDigit).ToArray());
            string baseUsername = $"{char.ToLowerInvariant(first[0])}{last.ToLowerInvariant()}";
            int suffix = await GetNextUsernameSuffixAsync(baseUsername);
            string generatedUserName = $"{baseUsername}{suffix}";
            string generatedEmail = $"{generatedUserName}@school.local";

            var user = new ApplicationUser
            {
                UserName = generatedUserName,
                Email = generatedEmail,
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                BirthDate = userDto.BirthDate,
                Gender = userDto.Gender,
                Address = userDto.Address,
                PESEL = userDto.PESEL,
                EnrollmentDate = userDto.EnrollmentDate,
                HireDate = userDto.HireDate,
                ClassId = userDto.ClassId
            };

            var result = await _userManager.CreateAsync(user, password);
            if (!result.Succeeded)
                return result;

            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
                return roleResult;

            // Aktualizuj dane DTO, żeby frontend mógł je dostać (opcjonalnie)
            userDto.UserName = user.UserName;
            userDto.Email = user.Email;

            return IdentityResult.Success;
        }


        // Aktualizuj istniejącego użytkownika
        public async Task<IdentityResult> UpdateUserAsync(string id, UserDto userDto, string? role = null)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });

            user.UserName = userDto.UserName;
            user.Email = userDto.Email;
            user.FirstName = userDto.FirstName;
            user.LastName = userDto.LastName;
            user.BirthDate = userDto.BirthDate;
            user.Gender = userDto.Gender;
            user.Address = userDto.Address;
            user.PESEL = userDto.PESEL;
            user.EnrollmentDate = userDto.EnrollmentDate;
            user.HireDate = userDto.HireDate;
            user.ClassId = userDto.ClassId;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return result;

            if (role != null)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (!currentRoles.Contains(role))
                {
                    var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                    if (!removeResult.Succeeded)
                        return removeResult;

                    var addResult = await _userManager.AddToRoleAsync(user, role);
                    if (!addResult.Succeeded)
                        return addResult;
                }
            }

            return IdentityResult.Success;
        }


        // Usuń użytkownika
        public async Task<IdentityResult> DeleteUserAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });

            return await _userManager.DeleteAsync(user);
        }

        // Wyszukaj użytkowników wg zapytania (opcjonalne)
        public async Task<List<UserDto>> SearchUsersAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return await GetUsersAsync();
            }

            query = query.ToLower();

            var users = await _userManager.Users
                .Where(u => u.UserName!.ToLower().Contains(query)
                            || u.Email!.ToLower().Contains(query)
                            || u.FirstName!.ToLower().Contains(query)
                            || u.LastName!.ToLower().Contains(query))
                .ToListAsync();

            return users.Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName!,
                Email = u.Email!,
                FirstName = u.FirstName!,
                LastName = u.LastName!
            }).ToList();
        }

        public async Task<List<UserDto>> GetTeachersAsync()
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync("Teacher");

            return usersInRole.Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName ?? "",
                Email = u.Email ?? "",
                FirstName = u.FirstName,
                LastName = u.LastName,
                BirthDate = u.BirthDate,
                Gender = u.Gender,
                Address = u.Address,
                PESEL = u.PESEL,
                HireDate = u.HireDate,
                ClassId = u.ClassId,
                Roles = new List<string> { "Teacher" } // domyślna rola
            }).ToList();
        }
        
        public async Task<int> GetNextUsernameSuffixAsync(string baseUsername)
        {
            var existing = await _context.Users
                .Where(u => u.UserName.StartsWith(baseUsername))
                .Select(u => u.UserName)
                .ToListAsync();

            int max = 0;
            foreach (var name in existing)
            {
                var suffix = name.Substring(baseUsername.Length);
                if (int.TryParse(suffix, out int number))
                {
                    max = Math.Max(max, number);
                }
            }

            return max + 1;
        }

    }
}
