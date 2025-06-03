using GradeBookApp.Data;
using GradeBookApp.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GradeBookApp.Services
{
    public class TeacherSubjectService
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly UserManager<ApplicationUser> _userManager;

        public TeacherSubjectService(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            UserManager<ApplicationUser> userManager)
        {
            _contextFactory = contextFactory;
            _userManager = userManager;
        }

        // Pobiera wszystkie przypisania (TeacherSubject)
        public async Task<List<TeacherSubject>> GetTeacherSubjectsAsync()
        {
            using var ctx = _contextFactory.CreateDbContext();
            return await ctx.TeacherSubjects
                            .AsNoTracking()
                            .ToListAsync();
        }

        // Pobiera jedno przypisanie po Id
        public async Task<TeacherSubject?> GetByIdAsync(string id)
        {
            using var ctx = _contextFactory.CreateDbContext();
            return await ctx.TeacherSubjects
                            .AsNoTracking()
                            .FirstOrDefaultAsync(ts => ts.Id == id);
        }

        // Sprawdza, czy w bazie istnieje przedmiot o danym Id
        public async Task<bool> SubjectExistsAsync(string subjectId)
        {
            using var ctx = _contextFactory.CreateDbContext();
            return await ctx.Subjects
                            .AsNoTracking()
                            .AnyAsync(s => s.Id == subjectId);
        }

        // Sprawdza, czy w Identity istnieje użytkownik o danym Id
        public async Task<bool> TeacherExistsAsync(string teacherId)
        {
            var user = await _userManager.FindByIdAsync(teacherId);
            return user != null;
        }

        // Sprawdza, czy istnieje przypisanie z tymi samymi SubjectId, TeacherId i ClassId
        public async Task<bool> ExistsAsync(string subjectId, string teacherId, int classId)
        {
            using var ctx = _contextFactory.CreateDbContext();
            return await ctx.TeacherSubjects
                            .AsNoTracking()
                            .AnyAsync(ts =>
                                ts.SubjectId == subjectId &&
                                ts.TeacherId == teacherId &&
                                ts.ClassId == classId);
        }

        /// <summary>
        /// Tworzy nowe przypisanie Teacher–Subject–Class i zwraca jego Id.
        /// Zakłada, że przed wykonaniem tej metody sprawdzono:
        ///     - czy Subject o subjectId istnieje (SubjectExistsAsync),
        ///     - czy użytkownik (teacher) istnieje (TeacherExistsAsync),
        ///     - czy nie istnieje duplikat (ExistsAsync).
        /// </summary>
        public async Task<string> AssignTeacherToSubjectAndClass(string teacherId, string subjectId, int classId)
        {
            using var ctx = _contextFactory.CreateDbContext();

            var entity = new TeacherSubject
            {
                Id = Guid.NewGuid().ToString(),
                TeacherId = teacherId,
                SubjectId = subjectId,
                ClassId = classId
            };

            ctx.TeacherSubjects.Add(entity);
            await ctx.SaveChangesAsync();
            return entity.Id;
        }

        // Usuwa przypisanie po Id
        public async Task<bool> RemoveTeacherSubjectAssignment(string id)
        {
            using var ctx = _contextFactory.CreateDbContext();
            var entity = await ctx.TeacherSubjects.FindAsync(id);
            if (entity == null)
                return false;

            ctx.TeacherSubjects.Remove(entity);
            await ctx.SaveChangesAsync();
            return true;
        }
    }
}
