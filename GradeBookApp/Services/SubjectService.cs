using GradeBookApp.Data;
using GradeBookApp.Data.Entities;
using GradeBookApp.Shared;
using Microsoft.EntityFrameworkCore;

namespace GradeBookApp.Services
{
    public class SubjectService
    {
        private readonly ApplicationDbContext _context;

        public SubjectService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<SubjectDto>> GetSubjectsAsync()
        {
            return await _context.Subjects
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
            var s = await _context.Subjects.FindAsync(id);
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
            var name = dto.Name.Trim().ToLower();
            var shortName = dto.ShortName.Trim().ToLower();

            var exists = await _context.Subjects
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

            _context.Subjects.Add(entity);
            await _context.SaveChangesAsync();

            dto.Id = entity.Id;
            return dto;
        }


        public async Task<bool> UpdateSubjectAsync(SubjectDto dto)
        {
            var entity = await _context.Subjects.FindAsync(dto.Id);
            if (entity == null)
                return false;

            // 🔐 Sprawdź czy inny przedmiot ma już taką nazwę lub skrót
            var conflict = await _context.Subjects
                .AnyAsync(s =>
                    s.Id != dto.Id &&
                    (s.Name.ToLower() == dto.Name.Trim().ToLower() ||
                     s.ShortName.ToLower() == dto.ShortName.Trim().ToLower())
                );

            if (conflict)
                throw new ArgumentException("Przedmiot o podanej nazwie lub skrócie już istnieje.");

            // 📝 Aktualizacja wartości
            entity.Name = dto.Name;
            entity.ShortName = dto.ShortName;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteSubjectAsync(string id)
        {
            var entity = await _context.Subjects.FindAsync(id);
            if (entity == null) return false;

            _context.Subjects.Remove(entity);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
