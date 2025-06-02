// ﻿GradeBookApp.Services/UserService.cs

using GradeBookApp.Data;
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
        private readonly ApplicationDbContext _context;

        public UserService(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        /// <summary>
        /// Pobiera wszystkich użytkowników i wypełnia DTO wraz z przypisanymi rolami.
        /// </summary>
        public async Task<List<UserDto>> GetUsersAsync()
        {
            // 1. Pobierz listę wszystkich ApplicationUser
            var users = await _userManager.Users.ToListAsync();

            // 2. For each user, pobieramy listę ról i wypełniamy DTO
            var result = new List<UserDto>();
            foreach (var u in users)
            {
                // Pobranie ról z Identity
                var roles = await _userManager.GetRolesAsync(u);

                // Mapa na UserDto
                var dto = new UserDto
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
                    ClassId = u.ClassId,
                    Roles = roles.ToList()
                };

                result.Add(dto);
            }

            return result;
        }

        /// <summary>
        /// Pobiera jednego użytkownika po Id i wypełnia DTO włącznie z rolami.
        /// </summary>
        public async Task<UserDto?> GetUserByIdAsync(string id)
        {
            // 1. Znajdź ApplicationUser po Id
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return null;

            // 2. Pobranie listy ról z Identity
            var roles = await _userManager.GetRolesAsync(user);

            // 3. Utworzenie DTO
            var userDto = new UserDto
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
                ClassId = user.ClassId,
                Roles = roles.ToList()
            };

            return userDto;
        }

        /// <summary>
        /// Tworzy nowego użytkownika z podanym hasłem i przypisuje mu domyślną rolę (np. "Student").
        /// </summary>
        public async Task<IdentityResult> CreateUserAsync(UserDto userDto, string password, string role = "Student")
        {
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

            // (opcjonalnie) zaktualizuj w DTO wygenerowane UserName i Email
            userDto.UserName = user.UserName;
            userDto.Email = user.Email;

            return IdentityResult.Success;
        }

        /// <summary>
        /// Aktualizuje dane użytkownika i (opcjonalnie) zmienia rolę.
        /// </summary>
        public async Task<IdentityResult> UpdateUserAsync(string id, UserDto userDto, string? role = null)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });

            // 1. Aktualizacja pól podstawowych
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

            // 2. Jeżeli przekazano parametr role, to zmieniamy dotychczasowe role na tę nową
            if (role != null)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                // jeśli aktualnie nie ma tej roli, to ją nadaj, usuwając wszystkie poprzednie
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

        /// <summary>
        /// Usuwa użytkownika.
        /// </summary>
        public async Task<IdentityResult> DeleteUserAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });

            return await _userManager.DeleteAsync(user);
        }

        /// <summary>
        /// Wyszukuje użytkowników według frazy (UserName, Email, FirstName, LastName).
        /// Role w wynikach nie są tu wypełniane (można dodać analogicznie jak w GetUsersAsync).
        /// </summary>
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

            var result = new List<UserDto>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                var dto = new UserDto
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
                    ClassId = u.ClassId,
                    Roles = roles.ToList()
                };
                result.Add(dto);
            }

            return result;
        }

        /// <summary>
        /// Pobiera użytkowników, którzy są w roli "Teacher".  
        /// Tu ręcznie ustawiamy w DTO listę ról jako ["Teacher"].
        /// </summary>
        public async Task<List<UserDto>> GetTeachersAsync()
        {
            var usersInRole = await _userManager.GetUsersInRoleAsync("Teacher");

            var result = usersInRole.Select(u => new UserDto
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
                EnrollmentDate = u.EnrollmentDate,
                HireDate = u.HireDate,
                ClassId = u.ClassId,
                Roles = new List<string> { "Teacher" }
            }).ToList();

            return result;
        }

        /// <summary>
        /// Pomocnicza metoda do wyliczania kolejnego numeru suffixu do unikalnego UserName.
        /// </summary>
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
                    max = System.Math.Max(max, number);
                }
            }

            return max + 1;
        }
    }
}
