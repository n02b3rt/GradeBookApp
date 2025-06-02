using GradeBookApp.Data;
using GradeBookApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GradeBookApp.Services;

public class TeacherSubjectService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public TeacherSubjectService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    // Pobierz wszystkie przypisania nauczycieli do przedmiotów i klas
    public async Task<List<TeacherSubject>> GetTeacherSubjectsAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.TeacherSubjects
            .Include(ts => ts.Teacher)
            .Include(ts => ts.Subject)
            .Include(ts => ts.Class)
            .ToListAsync();
    }

    // Przypisz nauczyciela do przedmiotu i klasy
    public async Task<bool> AssignTeacherToSubjectAndClass(string teacherId, string subjectId, int classId)
    {
        using var context = _contextFactory.CreateDbContext();
        var exists = await context.TeacherSubjects.AnyAsync(ts =>
            ts.TeacherId == teacherId && ts.SubjectId == subjectId && ts.ClassId == classId);
        if (exists) return false;

        var ts = new TeacherSubject
        {
            TeacherId = teacherId,
            SubjectId = subjectId,
            ClassId = classId
        };

        context.TeacherSubjects.Add(ts);
        await context.SaveChangesAsync();
        return true;
    }

    // Usuń przypisanie
    public async Task<bool> RemoveTeacherSubjectAssignment(string id)
    {
        using var context = _contextFactory.CreateDbContext();
        var ts = await context.TeacherSubjects.FindAsync(id);
        if (ts == null) return false;

        context.TeacherSubjects.Remove(ts);
        await context.SaveChangesAsync();
        return true;
    }
}