using GradeBookApp.Data.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace GradeBookApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            // Wypisujemy, z jaką bazą kontekst się łączy:
            var dbName = this.Database.GetDbConnection().Database;
            Console.WriteLine($"[ApplicationDbContext] Konstruktor: łączyć się będę z bazą => {dbName}");
        }

        public DbSet<Attendance> Attendances { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<Subject> Subjects { get; set; }
        public DbSet<TeacherSubject> TeacherSubjects { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Konfiguracje relacji:
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Class)
                .WithMany()
                .HasForeignKey(u => u.ClassId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Class>()
                .HasOne(c => c.Teacher)
                .WithMany()
                .HasForeignKey(c => c.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TeacherSubject>()
                .HasKey(ts => new { ts.TeacherId, ts.SubjectId, ts.ClassId });

            builder.Entity<TeacherSubject>()
                .HasOne(ts => ts.Teacher)
                .WithMany()
                .HasForeignKey(ts => ts.TeacherId);

            builder.Entity<TeacherSubject>()
                .HasOne(ts => ts.Subject)
                .WithMany(s => s.TeacherSubjects)
                .HasForeignKey(ts => ts.SubjectId);

            builder.Entity<TeacherSubject>()
                .HasOne(ts => ts.Class)
                .WithMany()
                .HasForeignKey(ts => ts.ClassId);

            builder.Entity<Grade>()
                .HasOne(g => g.Student)
                .WithMany(u => u.Grades)
                .HasForeignKey(g => g.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Grade>()
                .HasOne(g => g.Teacher)
                .WithMany()
                .HasForeignKey(g => g.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Grade>()
                .HasOne(g => g.Subject)
                .WithMany(s => s.Grades)
                .HasForeignKey(g => g.SubjectId);

            builder.Entity<Attendance>()
                .HasOne(a => a.Student)
                .WithMany(u => u.Attendances)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Attendance>()
                .HasOne(a => a.Subject)
                .WithMany(s => s.Attendances)
                .HasForeignKey(a => a.SubjectId);
        }
    }
}
