using GradeBookApp.Data;
using GradeBookApp.Data.Entities;
using GradeBookApp.Shared;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GradeBookApp.Services;

public class ClassService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public ClassService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<ClassDto>> GetClassesAsync()
    {
        await using var context = _contextFactory.CreateDbContext();
        // Log aktywnej bazy i connection stringa
        var dbName = context.Database.GetDbConnection().Database;
        var connStr = context.Database.GetDbConnection().ConnectionString;
        Console.WriteLine($"[ClassService] GetClassesAsync: Database = {dbName}, ConnectionString = {connStr}");

        var classes = await context.Classes
            .Include(c => c.Teacher)
            .ToListAsync();

        return classes.Select(c => new ClassDto
        {
            Id = c.Id,
            Name = c.Name,
            Year = c.Year,
            TeacherId = c.TeacherId,
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
        await using var context = _contextFactory.CreateDbContext();
        var dbName = context.Database.GetDbConnection().Database;
        var connStr = context.Database.GetDbConnection().ConnectionString;
        Console.WriteLine($"[ClassService] GetClassByIdAsync: Database = {dbName}, ConnectionString = {connStr}");

        var c = await context.Classes
            .Include(c => c.Teacher)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (c == null) return null;

        return new ClassDto
        {
            Id = c.Id,
            Name = c.Name,
            Year = c.Year,
            TeacherId = c.TeacherId,
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
        await using var context = _contextFactory.CreateDbContext();
        var dbName = context.Database.GetDbConnection().Database;
        var connStr = context.Database.GetDbConnection().ConnectionString;
        Console.WriteLine($"[ClassService] CreateClassAsync: Database = {dbName}, ConnectionString = {connStr}");

        var entity = new Class
        {
            Name = dto.Name,
            Year = dto.Year,
            TeacherId = dto.TeacherId
        };

        context.Classes.Add(entity);
        await context.SaveChangesAsync();

        return await GetClassByIdAsync(entity.Id)
               ?? throw new Exception("Created class not found.");
    }

    public async Task<bool> UpdateClassAsync(ClassDto dto)
    {
        await using var context = _contextFactory.CreateDbContext();
        var dbName = context.Database.GetDbConnection().Database;
        var connStr = context.Database.GetDbConnection().ConnectionString;
        Console.WriteLine($"[ClassService] UpdateClassAsync: Database = {dbName}, ConnectionString = {connStr}");

        var entity = await context.Classes.FindAsync(dto.Id);
        if (entity == null) return false;

        entity.Name = dto.Name;
        entity.Year = dto.Year;
        entity.TeacherId = dto.TeacherId;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteClassAsync(int id)
    {
        await using var context = _contextFactory.CreateDbContext();
        var dbName = context.Database.GetDbConnection().Database;
        var connStr = context.Database.GetDbConnection().ConnectionString;
        Console.WriteLine($"[ClassService] DeleteClassAsync: Database = {dbName}, ConnectionString = {connStr}");

        var entity = await context.Classes.FindAsync(id);
        if (entity == null) return false;

        context.Classes.Remove(entity);
        await context.SaveChangesAsync();
        return true;
    }
}