using GradeBookApp.Data;
using GradeBookApp.Data.Entities;
using GradeBookApp.Shared;
using Microsoft.EntityFrameworkCore;

namespace GradeBookApp.Services;

public class ClassService
{
    private readonly ApplicationDbContext _context;

    public ClassService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ClassDto>> GetClassesAsync()
    {
        var classes = await _context.Classes
            .Include(c => c.Teacher)
            .ToListAsync();

        return classes.Select(c => new ClassDto
        {
            Id = c.Id,
            Name = c.Name,
            Year = c.Year,
            TeacherId = c.TeacherId, // ← dodane!
            Teacher = c.Teacher == null ? null : new UserDto
            {
                Id = c.Teacher.Id,
                FirstName = c.Teacher.FirstName,
                LastName = c.Teacher.LastName,
                UserName = c.Teacher.UserName,
                Email = c.Teacher.Email ?? ""
            }
        }).ToList();
    }

    public async Task<ClassDto?> GetClassByIdAsync(int id)
    {
        var c = await _context.Classes
            .Include(c => c.Teacher)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (c == null) return null;

        return new ClassDto
        {
            Id = c.Id,
            Name = c.Name,
            Year = c.Year,
            TeacherId = c.TeacherId, // ← dodane!
            Teacher = c.Teacher == null ? null : new UserDto
            {
                Id = c.Teacher.Id,
                FirstName = c.Teacher.FirstName,
                LastName = c.Teacher.LastName,
                UserName = c.Teacher.UserName,
                Email = c.Teacher.Email ?? ""
            }
        };
    }

    public async Task<ClassDto> CreateClassAsync(ClassDto dto)
    {
        var entity = new Class
        {
            Name = dto.Name,
            Year = dto.Year,
            TeacherId = dto.TeacherId // ← zamiast dto.Teacher?.Id
        };

        _context.Classes.Add(entity);
        await _context.SaveChangesAsync();

        return await GetClassByIdAsync(entity.Id) ?? throw new Exception("Created class not found.");
    }

    public async Task<bool> UpdateClassAsync(ClassDto dto)
    {
        var entity = await _context.Classes.FindAsync(dto.Id);
        if (entity == null) return false;

        entity.Name = dto.Name;
        entity.Year = dto.Year;
        entity.TeacherId = dto.TeacherId;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteClassAsync(int id)
    {
        var entity = await _context.Classes.FindAsync(id);
        if (entity == null) return false;

        _context.Classes.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }
}
