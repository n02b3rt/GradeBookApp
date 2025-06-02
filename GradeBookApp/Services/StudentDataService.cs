using GradeBookApp.Data;
using GradeBookApp.Shared;
using Microsoft.EntityFrameworkCore;

namespace GradeBookApp.Services;

public class StudentDataService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public StudentDataService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<GradeDto>> GetGradesByStudentIdAsync(string studentId)
    {
        await using var context = _contextFactory.CreateDbContext();

        return await context.Grades
            .Include(g => g.Subject)
            .Where(g => g.StudentId == studentId)
            .Select(g => new GradeDto
            {
                Id = g.Id,
                Value = g.Value,
                Weight = g.Weight,
                Description = g.Description,
                DateGiven = g.DateGiven,
                StudentId = g.StudentId,
                SubjectId = g.SubjectId,
                TeacherId = g.TeacherId
            })
            .ToListAsync();
    }

    public async Task<List<AttendanceDto>> GetAttendanceByStudentIdAsync(string studentId)
    {
        await using var context = _contextFactory.CreateDbContext();

        return await context.Attendances 
            .Include(a => a.Subject)
            .Where(a => a.StudentId == studentId)
            .Select(a => new AttendanceDto
            {
                Id = a.Id,
                StudentId = a.StudentId,
                SubjectId = a.SubjectId,
                Date = a.Date,
                Status = a.Status,
                Note = a.Note
            })
            .ToListAsync();
    }
}