// Services/TeacherActionsService.cs
using GradeBookApp.Data;
using GradeBookApp.Data.Entities;
using GradeBookApp.Shared;
using Microsoft.EntityFrameworkCore;

namespace GradeBookApp.Services;

public class TeacherActionsService
{
    private readonly ApplicationDbContext _db;

    public TeacherActionsService(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task AddAttendanceAsync(AttendanceDto dto)
    {
        var entity = new Attendance
        {
            StudentId = dto.StudentId,
            SubjectId = dto.SubjectId,
            Date = dto.Date,
            Status = dto.Status,
            Note = dto.Note
        };
        _db.Attendances.Add(entity);
        await _db.SaveChangesAsync();
    }

    public async Task AddBulkAttendanceAsync(List<AttendanceDto> dtos)
    {
        var entities = dtos.Select(dto => new Attendance
        {
            StudentId = dto.StudentId,
            SubjectId = dto.SubjectId,
            Date = dto.Date,
            Status = dto.Status,
            Note = dto.Note
        });
        _db.Attendances.AddRange(entities);
        await _db.SaveChangesAsync();
    }

    public async Task SaveAttendanceBulkAsync(List<AttendanceDto> dtos)
    {
        await AddBulkAttendanceAsync(dtos);
    }

    public async Task AddGradeAsync(GradeDto dto)
    {
        var entity = new Grade
        {
            StudentId = dto.StudentId,
            SubjectId = dto.SubjectId,
            TeacherId = dto.TeacherId,
            Value = dto.Value,
            Description = dto.Description,
            Weight = dto.Weight,
            DateGiven = dto.DateGiven
        };
        _db.Grades.Add(entity);
        await _db.SaveChangesAsync();
    }

    public async Task AddBulkGradesAsync(List<GradeDto> dtos)
    {
        var entities = dtos.Select(dto => new Grade
        {
            StudentId = dto.StudentId,
            SubjectId = dto.SubjectId,
            TeacherId = dto.TeacherId,
            Value = dto.Value,
            Description = dto.Description,
            Weight = dto.Weight,
            DateGiven = dto.DateGiven
        });
        _db.Grades.AddRange(entities);
        await _db.SaveChangesAsync();
    }

    public async Task SaveGradesBulkAsync(List<GradeDto> dtos)
    {
        await AddBulkGradesAsync(dtos);
    }

    public async Task<List<UserDto>> GetStudentsInClassAsync(int classId)
    {
        return await _db.Users
            .Where(u => u.ClassId == classId)
            .Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                BirthDate = u.BirthDate,
                Gender = u.Gender,
                Address = u.Address,
                PESEL = u.PESEL,
                EnrollmentDate = u.EnrollmentDate,
                HireDate = u.HireDate,
                ClassId = u.ClassId
            })
            .ToListAsync();
    }

    public async Task<List<TeacherSubjectDto>> GetTeacherSubjectsAsync(string teacherId)
    {
        return await _db.TeacherSubjects
            .Where(ts => ts.TeacherId == teacherId)
            .Select(ts => new TeacherSubjectDto
            {
                TeacherId = ts.TeacherId,
                SubjectId = ts.SubjectId,
                ClassId = ts.ClassId,
                Id = ts.Id
            })
            .ToListAsync();
    }

    public async Task<List<SubjectDto>> GetSubjectsForTeacherAsync(string teacherId)
    {
        return await _db.TeacherSubjects
            .Where(ts => ts.TeacherId == teacherId)
            .Select(ts => new SubjectDto
            {
                Id = ts.Subject.Id,
                Name = ts.Subject.Name,
                ShortName = ts.Subject.ShortName
            })
            .Distinct()
            .ToListAsync();
    }

    public async Task<List<ClassDto>> GetClassesForTeacherAsync(string teacherId)
    {
        return await _db.Classes
            .Where(c => _db.TeacherSubjects.Any(ts => ts.TeacherId == teacherId && ts.ClassId == c.Id))
            .Select(c => new ClassDto
            {
                Id = c.Id,
                Name = c.Name,
                Year = c.Year,
                TeacherId = c.TeacherId
            })
            .ToListAsync();
    }

    public async Task<List<GradeDto>> GetGradesForStudentSubjectAsync(string studentId, string subjectId)
    {
        return await _db.Grades
            .Where(g => g.StudentId == studentId && g.SubjectId == subjectId)
            .Select(g => new GradeDto
            {
                Id = g.Id,
                StudentId = g.StudentId,
                SubjectId = g.SubjectId,
                TeacherId = g.TeacherId,
                Value = g.Value,
                Weight = g.Weight,
                Description = g.Description,
                DateGiven = g.DateGiven
            })
            .ToListAsync();
    }
    
    public async Task<Dictionary<string, List<GradeDto>>> GetGradesForClassAndSubjectByTeacherAsync(int classId, string subjectId, string teacherId)
    {
        // Pobierz uczniów z danej klasy
        var students = await _db.Users
            .Where(u => u.ClassId == classId)
            .ToListAsync();

        var studentIds = students.Select(s => s.Id).ToList();

        // Pobierz tylko te oceny, które nauczyciel wystawił dla tego przedmiotu w tej klasie
        var grades = await _db.Grades
            .Where(g => studentIds.Contains(g.StudentId)
                        && g.SubjectId == subjectId
                        && g.TeacherId == teacherId)
            .ToListAsync();

        // Grupowanie: StudentId => List<GradeDto>
        return grades
            .GroupBy(g => g.StudentId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new GradeDto
                {
                    Id = x.Id,
                    StudentId = x.StudentId,
                    SubjectId = x.SubjectId,
                    TeacherId = x.TeacherId,
                    Value = x.Value,
                    Weight = x.Weight,
                    Description = x.Description,
                    DateGiven = x.DateGiven
                }).ToList()
            );
    }

}
