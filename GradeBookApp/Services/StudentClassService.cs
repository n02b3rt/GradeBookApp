using GradeBookApp.Data;
using GradeBookApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GradeBookApp.Services;

public class StudentClassService
{
    private readonly ApplicationDbContext _context;

    public StudentClassService(ApplicationDbContext context)
    {
        _context = context;
    }

    // Pobierz uczniów przypisanych do klasy na podstawie ClassId w użytkownikach
    public async Task<List<ApplicationUser>> GetStudentsInClassAsync(int classId)
    {
        return await _context.Users
            .Where(u => u.ClassId == classId)
            .ToListAsync();
    }

    // Przypisz uczniów do klasy (ustaw ClassId w encji ApplicationUser)
    public async Task<bool> AssignStudentsToClass(int classId, List<string> studentIds)
    {
        var students = await _context.Users
            .Where(u => studentIds.Contains(u.Id))
            .ToListAsync();

        if (students.Count == 0) return false;

        // Odłącz wszystkich uczniów z tej klasy (czyli ustaw ClassId=null)
        var currentStudents = await _context.Users
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

        await _context.SaveChangesAsync();
        return true;
    }
}