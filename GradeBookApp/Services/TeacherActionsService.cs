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
        if (string.IsNullOrWhiteSpace(dto.StudentId) ||
            string.IsNullOrWhiteSpace(dto.SubjectId) ||
            string.IsNullOrWhiteSpace(dto.TeacherId) ||
            dto.Value <= 0 || dto.Weight <= 0)
        {
            if (string.IsNullOrWhiteSpace(dto.StudentId))
                throw new ArgumentException("Missing StudentId");
            if (string.IsNullOrWhiteSpace(dto.SubjectId))
                throw new ArgumentException("Missing SubjectId");
            if (string.IsNullOrWhiteSpace(dto.TeacherId))
                throw new ArgumentException("Missing TeacherId");
            if (dto.Value < 1 || dto.Value > 6)
                throw new ArgumentException($"Invalid grade value: {dto.Value}");
            if (dto.Weight < 1 || dto.Weight > 3)
                throw new ArgumentException($"Invalid weight: {dto.Weight}");

        }

        var entity = new Grade
        {
            StudentId = dto.StudentId,
            SubjectId = dto.SubjectId,
            TeacherId = dto.TeacherId,
            Value = dto.Value,
            Description = dto.Description ?? "",
            Weight = dto.Weight,
            DateGiven = dto.DateGiven.Kind == DateTimeKind.Utc
                ? dto.DateGiven
                : DateTime.SpecifyKind(dto.DateGiven, DateTimeKind.Utc)
        };

        _db.Grades.Add(entity);
        await _db.SaveChangesAsync();
    }


    public async Task AddBulkGradesAsync(List<GradeDto> dtos)
    {
        if (dtos == null || !dtos.Any())
            throw new ArgumentException("Grade list cannot be null or empty.");

        var entities = dtos.Select(dto => new Grade
        {
            StudentId = dto.StudentId,
            SubjectId = dto.SubjectId,
            TeacherId = dto.TeacherId,
            Value = dto.Value <= 0 ? 0 : dto.Value, // zabezpieczenie przed zerem
            Description = string.IsNullOrWhiteSpace(dto.Description) ? "Brak opisu" : dto.Description,
            Weight = dto.Weight <= 0 ? 1 : dto.Weight,
            DateGiven = dto.DateGiven.Kind == DateTimeKind.Utc ? dto.DateGiven : dto.DateGiven.ToUniversalTime()
        });

        await _db.Grades.AddRangeAsync(entities);
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
    
    public async Task<List<AttendanceDto>> GetAttendanceForClassAndSubjectAsync(int classId, string subjectId)
    {
        var studentIds = await _db.Users
            .Where(u => u.ClassId == classId)
            .Select(u => u.Id)
            .ToListAsync();

        var attendance = await _db.Attendances
            .Where(a => a.SubjectId == subjectId && studentIds.Contains(a.StudentId))
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

        return attendance;
    }

    public async Task UpdateAttendanceBulkAsync(List<AttendanceDto> dtos)
    {
        foreach (var dto in dtos)
        {
            var existing = await _db.Attendances
                .FirstOrDefaultAsync(a =>
                    a.Id == dto.Id &&
                    a.StudentId == dto.StudentId &&
                    a.SubjectId == dto.SubjectId &&
                    a.Date == dto.Date);

            if (existing is not null)
            {
                existing.Status = dto.Status;
                existing.Note = dto.Note;
            }
        }

        await _db.SaveChangesAsync();
    }

    public async Task<GradeDto?> GetGradeByIdAsync(int gradeId)
    {
        var grade = await _db.Grades.FirstOrDefaultAsync(g => g.Id == gradeId);
        if (grade == null) return null;

        // Zakładamy, że Grade ma klasę przypisaną przez nawigację z Usera
        var student = await _db.Users.FirstOrDefaultAsync(u => u.Id == grade.StudentId);

        return new GradeDto
        {
            Id = grade.Id,
            StudentId = grade.StudentId,
            SubjectId = grade.SubjectId,
            TeacherId = grade.TeacherId,
            Value = grade.Value,
            Weight = grade.Weight,
            Description = grade.Description,
            DateGiven = grade.DateGiven,
            ClassId = student?.ClassId ?? 0 // <-- Tutaj ClassId!
        };
    }

    
    public async Task UpdateGradeAsync(GradeDto dto)
    {
        var grade = await _db.Grades.FirstOrDefaultAsync(g => g.Id == dto.Id);

        if (grade is null)
            throw new ArgumentException($"Ocena o ID {dto.Id} nie istnieje.");

        grade.Value = dto.Value;
        grade.Weight = dto.Weight;
        grade.Description = dto.Description;
        grade.DateGiven = dto.DateGiven;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteGradeAsync(int gradeId)
    {
        var grade = await _db.Grades.FirstOrDefaultAsync(g => g.Id == gradeId);

        if (grade is null)
            throw new ArgumentException($"Ocena o ID {gradeId} nie istnieje.");

        _db.Grades.Remove(grade);
        await _db.SaveChangesAsync();
    }

}
