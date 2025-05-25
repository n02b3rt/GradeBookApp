using GradeBookApp.Data.Entities;
using GradeBookApp.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GradeBookApp.Services
{
    public class UserService
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public UserService(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
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
            var user = new ApplicationUser
            {
                UserName = userDto.Email, // ustawiamy username na email dla spójności logowania
                Email = userDto.Email,
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

            // Dodaj rolę
            var roleResult = await _userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
                return roleResult;

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

        // Pobierz nauczycieli (tylko użytkownicy z rolą Teacher)
        public async Task<List<UserDto>> GetTeachersAsync()
        {
            var users = await _userManager.Users.ToListAsync();
            var teachers = new List<UserDto>();

            foreach (var user in users)
            {
                if (await _userManager.IsInRoleAsync(user, "Teacher"))
                {
                    teachers.Add(new UserDto
                    {
                        Id = user.Id,
                        UserName = user.UserName!,
                        Email = user.Email!,
                        FirstName = user.FirstName!,
                        LastName = user.LastName!
                    });
                }
            }
            return teachers;
        }
    }
}
