using GradeBookApp.Data;
using GradeBookApp.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace GradeBookApp.Services;

public class SubjectService
{
    private readonly ApplicationDbContext _context;

    public SubjectService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<Subject>> GetSubjectsAsync()
    {
        return await _context.Subjects.ToListAsync();
    }

    public async Task<Subject?> GetSubjectByIdAsync(string id)
    {
        return await _context.Subjects.FindAsync(id);
    }

    public async Task<Subject> CreateSubjectAsync(Subject newSubject)
    {
        _context.Subjects.Add(newSubject);
        await _context.SaveChangesAsync();
        return newSubject;
    }

    public async Task<bool> UpdateSubjectAsync(Subject updatedSubject)
    {
        var existing = await _context.Subjects.FindAsync(updatedSubject.Id);
        if (existing == null) return false;

        existing.Name = updatedSubject.Name;
        existing.ShortName = updatedSubject.ShortName;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteSubjectAsync(string id)
    {
        var subject = await _context.Subjects.FindAsync(id);
        if (subject == null) return false;

        _context.Subjects.Remove(subject);
        await _context.SaveChangesAsync();
        return true;
    }
}