using GradeBookApp.Data;
using GradeBookApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GradeBookApp.Services;

public class TeacherSubjectService
{
    private readonly ApplicationDbContext _context;

    public TeacherSubjectService(ApplicationDbContext context)
    {
        _context = context;
    }

    // Pobierz wszystkie przypisania nauczycieli do przedmiotów i klas
    public async Task<List<TeacherSubject>> GetTeacherSubjectsAsync()
    {
        return await _context.TeacherSubjects
            .Include(ts => ts.Teacher)
            .Include(ts => ts.Subject)
            .Include(ts => ts.Class)
            .ToListAsync();
    }

    // Przypisz nauczyciela do przedmiotu i klasy
    public async Task<bool> AssignTeacherToSubjectAndClass(string teacherId, string subjectId, int classId)
    {
        // Sprawdź, czy już istnieje
        var exists = await _context.TeacherSubjects.AnyAsync(ts =>
            ts.TeacherId == teacherId && ts.SubjectId == subjectId && ts.ClassId == classId);
        if (exists) return false;

        var ts = new TeacherSubject
        {
            TeacherId = teacherId,
            SubjectId = subjectId,
            ClassId = classId
        };

        _context.TeacherSubjects.Add(ts);
        await _context.SaveChangesAsync();
        return true;
    }

    // Usuń przypisanie
    public async Task<bool> RemoveTeacherSubjectAssignment(string id)
    {
        var ts = await _context.TeacherSubjects.FindAsync(id);
        if (ts == null) return false;

        _context.TeacherSubjects.Remove(ts);
        await _context.SaveChangesAsync();
        return true;
    }
}