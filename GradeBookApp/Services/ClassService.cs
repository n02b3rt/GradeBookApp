using GradeBookApp.Data;
using GradeBookApp.Data.Entities;
using GradeBookApp.Shared;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GradeBookApp.Services
{
    public class ClassService
    {
        private readonly ApplicationDbContext _context;

        public ClassService(ApplicationDbContext context)
        {
            _context = context;
        }

        private static ClassDto MapToDto(Class cls) => new ClassDto
        {
            Id = cls.Id,
            Name = cls.Name,
            Year = cls.Year,
            TeacherId = cls.TeacherId,
            StudentIds = new List<string>() // lub pomiń jeśli niepotrzebne
        };

        private Class MapToEntity(ClassDto dto)
        {
            return new Class
            {
                Id = dto.Id,
                Name = dto.Name,
                Year = dto.Year,
                TeacherId = dto.TeacherId,
            };
        }

        public async Task<List<ClassDto>> GetClassesAsync()
        {
            var classes = await _context.Classes
                .Include(c => c.Teacher)
                .ToListAsync();

            return classes.Select(MapToDto).ToList();
        }

        public async Task<ClassDto?> GetClassByIdAsync(int id)
        {
            var cls = await _context.Classes
                .Include(c => c.Teacher)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (cls == null) return null;
            return MapToDto(cls);
        }

        public async Task<ClassDto> CreateClassAsync(ClassDto newClassDto)
        {
            var teacher = await _context.Users.FirstOrDefaultAsync(u => u.Id == newClassDto.TeacherId);
            if (teacher == null)
                throw new ArgumentException("Wybrany wychowawca nie istnieje (z kontekstu).");

            var newClass = new Class
            {
                Name = newClassDto.Name,
                Year = newClassDto.Year,
                TeacherId = newClassDto.TeacherId
            };

            _context.Classes.Add(newClass);
            await _context.SaveChangesAsync();

            newClassDto.Id = newClass.Id;
            return newClassDto;
        }

        public async Task<bool> UpdateClassAsync(ClassDto updatedClassDto)
        {
            var existing = await _context.Classes.FindAsync(updatedClassDto.Id);
            if (existing == null) return false;

            existing.Name = updatedClassDto.Name;
            existing.Year = updatedClassDto.Year;
            existing.TeacherId = updatedClassDto.TeacherId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteClassAsync(int id)
        {
            var cls = await _context.Classes.FindAsync(id);
            if (cls == null) return false;

            _context.Classes.Remove(cls);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
