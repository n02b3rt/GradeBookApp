using GradeBookApp.Data;
using GradeBookApp.Data.Entities;
using GradeBookApp.Shared;
using Microsoft.EntityFrameworkCore;

namespace GradeBookApp.Services;

public class SubjectService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public SubjectService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<SubjectDto>> GetSubjectsAsync()
    {
        using var context = _contextFactory.CreateDbContext();
        return await context.Subjects
            .Select(s => new SubjectDto
            {
                Id = s.Id,
                Name = s.Name,
                ShortName = s.ShortName
            })
            .ToListAsync();
    }

    public async Task<SubjectDto?> GetSubjectByIdAsync(string id)
    {
        using var context = _contextFactory.CreateDbContext();
        var s = await context.Subjects.FindAsync(id);
        if (s == null) return null;

        return new SubjectDto
        {
            Id = s.Id,
            Name = s.Name,
            ShortName = s.ShortName
        };
    }

    public async Task<SubjectDto> CreateSubjectAsync(SubjectDto dto)
    {
        using var context = _contextFactory.CreateDbContext();
        var name = dto.Name.Trim().ToLower();
        var shortName = dto.ShortName.Trim().ToLower();

        var exists = await context.Subjects
            .AnyAsync(s =>
                s.Name.ToLower() == name ||
                s.ShortName.ToLower() == shortName
            );

        if (exists)
            throw new ArgumentException("Przedmiot o podanej nazwie lub skrócie już istnieje.");

        var entity = new Subject
        {
            Id = Guid.NewGuid().ToString(),
            Name = dto.Name,
            ShortName = dto.ShortName
        };

        context.Subjects.Add(entity);
        await context.SaveChangesAsync();

        dto.Id = entity.Id;
        return dto;
    }

    public async Task<bool> UpdateSubjectAsync(SubjectDto dto)
    {
        using var context = _contextFactory.CreateDbContext();
        var entity = await context.Subjects.FindAsync(dto.Id);
        if (entity == null) return false;

        var conflict = await context.Subjects
            .AnyAsync(s =>
                s.Id != dto.Id &&
                (s.Name.ToLower() == dto.Name.Trim().ToLower() ||
                 s.ShortName.ToLower() == dto.ShortName.Trim().ToLower())
            );

        if (conflict)
            throw new ArgumentException("Przedmiot o podanej nazwie lub skrócie już istnieje.");

        entity.Name = dto.Name;
        entity.ShortName = dto.ShortName;

        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteSubjectAsync(string id)
    {
        using var context = _contextFactory.CreateDbContext();
        var entity = await context.Subjects.FindAsync(id);
        if (entity == null) return false;

        context.Subjects.Remove(entity);
        await context.SaveChangesAsync();
        return true;
    }
}
