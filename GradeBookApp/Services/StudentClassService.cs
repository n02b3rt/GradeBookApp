using GradeBookApp.Data;
using GradeBookApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GradeBookApp.Services;

public class StudentClassService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public StudentClassService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    // Pobierz uczniów przypisanych do klasy na podstawie ClassId w użytkownikach
    public async Task<List<ApplicationUser>> GetStudentsInClassAsync(int classId)
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.Users
            .Where(u => u.ClassId == classId)
            .ToListAsync();
    }

    // Przypisz uczniów do klasy (ustaw ClassId w encji ApplicationUser)
    public async Task<bool> AssignStudentsToClass(int classId, List<string> studentIds)
    {
        using var context = _contextFactory.CreateDbContext();

        var students = await context.Users
            .Where(u => studentIds.Contains(u.Id))
            .ToListAsync();

        if (!students.Any()) return false;

        // Odłącz wszystkich uczniów z tej klasy (czyli ustaw ClassId = null)
        var currentStudents = await context.Users
            .Where(u => u.ClassId == classId)
            .ToListAsync();

        foreach (var student in currentStudents)
        {
            student.ClassId = null;
        }

        // Przypisz nową klasę do wybranych uczniów
        foreach (var student in students)
        {
            student.ClassId = classId;
        }

        await context.SaveChangesAsync();
        return true;
    }
}