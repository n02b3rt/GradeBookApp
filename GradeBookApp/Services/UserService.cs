// ﻿GradeBookApp.Services/UserService.cs

using GradeBookApp.Data;
using GradeBookApp.Data.Entities;
using GradeBookApp.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GradeBookApp.Services;

public class UserService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public UserService(UserManager<ApplicationUser> userManager,
                       IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _userManager = userManager;
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Pobiera wszystkich użytkowników i wypełnia DTO wraz z przypisanymi rolami.
    /// </summary>
    public async Task<List<UserDto>> GetUsersAsync()
    {
        // 1. Pobierz listę wszystkich ApplicationUser
        var users = await _userManager.Users.ToListAsync();

        // 2. Dla każdego user pobieramy role i wypełniamy DTO
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
    /// Pobiera jednego użytkownika po Id i wypełnia DTO włącznie z rolami.
    /// </summary>
    public async Task<UserDto?> GetUserByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return null;

        var roles = await _userManager.GetRolesAsync(user);
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
            ClassId = user.ClassId,
            Roles = roles.ToList()
        };
    }

    /// <summary>
    /// Tworzy nowego użytkownika z podanym hasłem i przypisuje mu domyślną rolę (np. "Student").
    /// </summary>
    public async Task<IdentityResult> CreateUserAsync(UserDto userDto, string password, string role = "Student")
    {
        if (string.IsNullOrWhiteSpace(userDto.FirstName) || string.IsNullOrWhiteSpace(userDto.LastName))
        {
            return IdentityResult.Failed(new IdentityError
            {
                Description = "Imię i nazwisko są wymagane do wygenerowania loginu i emaila."
            });
        }

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
                if (!removeResult.Succeeded) return removeResult;

                var addResult = await _userManager.AddToRoleAsync(user, role);
                if (!addResult.Succeeded) return addResult;
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
            return await GetUsersAsync();

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
            result.Add(new UserDto
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
            });
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
            EnrollmentDate = u.EnrollmentDate,
            HireDate = u.HireDate,
            ClassId = u.ClassId,
            Roles = new List<string> { "Teacher" }
        }).ToList();
    }

    /// <summary>
    /// Pomocnicza metoda do wyliczania kolejnego numeru suffixu do unikalnego UserName.
    /// </summary>
    public async Task<int> GetNextUsernameSuffixAsync(string baseUsername)
    {
        using var context = _contextFactory.CreateDbContext();
        var existing = await context.Users
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
