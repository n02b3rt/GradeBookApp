using GradeBookApp.Data;
using GradeBookApp.Data.Entities;
using GradeBookApp.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GradeBookApp.Services
{
    public class SubjectService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly UserManager<ApplicationUser> _userManager;

        public SubjectService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            UserManager<ApplicationUser> userManager)
        {
            _contextFactory = contextFactory;
            _userManager = userManager;
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

        public async Task<bool> AssignTeacherAsync(TeacherSubjectDto dto)
        {
            if (dto == null)
                throw new ArgumentException("Dane TeacherSubjectDto nie mogą być null.");

            if (string.IsNullOrWhiteSpace(dto.SubjectId))
                throw new ArgumentException("SubjectId nie może być pusty.");

            if (string.IsNullOrWhiteSpace(dto.TeacherId))
                throw new ArgumentException("TeacherId nie może być pusty.");

            if (dto.ClassId <= 0)
                throw new ArgumentException("ClassId musi być liczbą większą od zera.");

            using var context = _contextFactory.CreateDbContext();

            // 1) Czy istnieje dany przedmiot?
            var subjectExists = await context.Subjects
                .AsNoTracking()
                .AnyAsync(s => s.Id == dto.SubjectId);
            if (!subjectExists)
                return false;

            // 2) Czy istnieje użytkownik (nauczyciel) o takim ID?
            var user = await _userManager.FindByIdAsync(dto.TeacherId);
            if (user == null)
                throw new ArgumentException($"Nie znaleziono użytkownika o ID = {dto.TeacherId}.");

            // (opcjonalnie) sprawdzenie, czy ma rolę "Teacher":
            /*
            if (!await _userManager.IsInRoleAsync(user, "Teacher"))
                throw new ArgumentException("Podany użytkownik nie ma roli Teacher.");
            */

            // 3) Czy istnieje klasa o podanym ClassId?
            var classExists = await context.Classes
                .AsNoTracking()
                .AnyAsync(c => c.Id == dto.ClassId);
            if (!classExists)
                throw new ArgumentException($"Nie znaleziono klasy o ID = {dto.ClassId}.");

            // 4) Sprawdź duplikat
            var duplicate = await context.TeacherSubjects
                .AsNoTracking()
                .AnyAsync(ts =>
                    ts.SubjectId == dto.SubjectId &&
                    ts.TeacherId == dto.TeacherId &&
                    ts.ClassId == dto.ClassId);
            if (duplicate)
                throw new ArgumentException("Taki przypisany nauczyciel do przedmiotu i klasy już istnieje.");

            // 5) Tworzymy nowe przypisanie
            var entity = new TeacherSubject
            {
                Id = Guid.NewGuid().ToString(),
                SubjectId = dto.SubjectId,
                TeacherId = dto.TeacherId,
                ClassId = dto.ClassId
            };

            context.TeacherSubjects.Add(entity);
            await context.SaveChangesAsync();

            dto.Id = entity.Id; // zwróć klientowi wygenerowane Id
            return true;
        }

        public async Task<TeacherSubjectDto?> GetAssignmentBySubjectIdAsync(string subjectId)
        {
            if (string.IsNullOrWhiteSpace(subjectId))
                return null;

            using var context = _contextFactory.CreateDbContext();

            var entity = await context.TeacherSubjects
                .AsNoTracking()
                .FirstOrDefaultAsync(ts => ts.SubjectId == subjectId);

            if (entity == null)
                return null;

            return new TeacherSubjectDto
            {
                Id = entity.Id,
                SubjectId = entity.SubjectId,
                TeacherId = entity.TeacherId,
                ClassId = entity.ClassId
            };
        }

        public async Task<bool> UpdateAssignmentAsync(TeacherSubjectDto dto)
        {
            if (dto == null)
                throw new ArgumentException("Dane TeacherSubjectDto nie mogą być null.");

            if (string.IsNullOrWhiteSpace(dto.Id))
                throw new ArgumentException("Id przypisania nie może być puste.");

            if (string.IsNullOrWhiteSpace(dto.SubjectId))
                throw new ArgumentException("SubjectId nie może być pusty.");

            if (string.IsNullOrWhiteSpace(dto.TeacherId))
                throw new ArgumentException("TeacherId nie może być pusty.");

            if (dto.ClassId <= 0)
                throw new ArgumentException("ClassId musi być liczbą większą od zera.");

            using var context = _contextFactory.CreateDbContext();

            // 1) Pobierz istniejące przypisanie
            var entity = await context.TeacherSubjects
                .FirstOrDefaultAsync(ts => ts.Id == dto.Id);
            if (entity == null)
                return false;

            // 2) Weryfikacja przedmiotu
            var subjectExists = await context.Subjects
                .AsNoTracking()
                .AnyAsync(s => s.Id == dto.SubjectId);
            if (!subjectExists)
                throw new ArgumentException($"Nie znaleziono przedmiotu o ID = {dto.SubjectId}.");

            // 3) Weryfikacja użytkownika (nauczyciela)
            var user = await _userManager.FindByIdAsync(dto.TeacherId);
            if (user == null)
                throw new ArgumentException($"Nie znaleziono użytkownika o ID = {dto.TeacherId}.");

            // (opcjonalnie) sprawdź rolę "Teacher"
            /*
            if (!await _userManager.IsInRoleAsync(user, "Teacher"))
                throw new ArgumentException("Podany użytkownik nie ma roli Teacher.");
            */

            // 4) Weryfikacja klasy
            var classExists = await context.Classes
                .AsNoTracking()
                .AnyAsync(c => c.Id == dto.ClassId);
            if (!classExists)
                throw new ArgumentException($"Nie znaleziono klasy o ID = {dto.ClassId}.");

            // 5) Sprawdź duplikat (inny rekord, ale te same wartości)
            var duplicate = await context.TeacherSubjects
                .AsNoTracking()
                .AnyAsync(ts =>
                    ts.Id != dto.Id &&
                    ts.SubjectId == dto.SubjectId &&
                    ts.TeacherId == dto.TeacherId &&
                    ts.ClassId == dto.ClassId);
            if (duplicate)
                throw new ArgumentException("Istnieje już przypisanie z tymi samymi wartościami przedmiot, nauczyciel i klasa.");

            // 6) Nadpisz pola
            entity.TeacherId = dto.TeacherId;
            entity.ClassId = dto.ClassId;

            context.TeacherSubjects.Update(entity);
            await context.SaveChangesAsync();
            return true;
        }
    }
}
