using GradeBookApp.Data.Entities;
using GradeBookApp.Shared;
using Microsoft.AspNetCore.Identity;

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
            var users = _userManager.Users.ToList(); // jeśli EF Core - możesz zrobić ToListAsync() z DbContext

            var userDtos = users.Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName
            }).ToList();

            return await Task.FromResult(userDtos);
        }

        // Pobierz użytkownika po Id
        public async Task<UserDto?> GetUserByIdAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return null;

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName
            };
        }

        // Utwórz nowego użytkownika (z hasłem)
        public async Task<IdentityResult> CreateUserAsync(UserDto userDto, string password)
        {
            var user = new ApplicationUser
            {
                UserName = userDto.UserName,
                Email = userDto.Email,
                FirstName = userDto.FirstName,
                LastName = userDto.LastName
            };

            var result = await _userManager.CreateAsync(user, password);
            return result;
        }

        // Aktualizuj istniejącego użytkownika
        public async Task<IdentityResult> UpdateUserAsync(string id, UserDto userDto)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return IdentityResult.Failed();

            user.UserName = userDto.UserName;
            user.Email = userDto.Email;
            user.FirstName = userDto.FirstName;
            user.LastName = userDto.LastName;

            var result = await _userManager.UpdateAsync(user);
            return result;
        }

        // Usuń użytkownika
        public async Task<IdentityResult> DeleteUserAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return IdentityResult.Failed();

            var result = await _userManager.DeleteAsync(user);
            return result;
        }
        public async Task<List<UserDto>> SearchUsersAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return await GetUsersAsync();
            }

            query = query.ToLower();

            var users = _userManager.Users
                .Where(u => u.UserName.ToLower().Contains(query)
                            || u.Email.ToLower().Contains(query)
                            || u.FirstName.ToLower().Contains(query)
                            || u.LastName.ToLower().Contains(query))
                .ToList();

            // Zamapuj ApplicationUser na UserDto
            var userDtos = users.Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName
            }).ToList();

            return userDtos;
        }

    }
}
