using GradeBookApp.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GradeBookApp.Data.Seed;

public static class DbSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
    {
        var roles = new[] { "Admin", "Teacher", "Student" };
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        if (await db.Users.AnyAsync()) return;

        // === ADMIN ===
        var admin = new ApplicationUser
        {
            UserName = "admin@school.local",
            Email = "admin@school.local",
            EmailConfirmed = true,
            FirstName = "Adam",
            LastName = "Admin",
            BirthDate = new DateOnly(1980, 1, 1)
        };
        var adminResult = await userManager.CreateAsync(admin, "Admin123!");
        if (adminResult.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }
        else
        {
            foreach (var error in adminResult.Errors)
                Console.WriteLine($"❌ Admin error: {error.Description}");
        }

        // === TEACHERS ===
        var teacher1 = new ApplicationUser
        {
            UserName = "math@school.local",
            Email = "math@school.local",
            EmailConfirmed = true,
            FirstName = "Maria",
            LastName = "Matematyczna",
            HireDate = new DateOnly(2010, 9, 1)
        };
        var teacher2 = new ApplicationUser
        {
            UserName = "bio@school.local",
            Email = "bio@school.local",
            EmailConfirmed = true,
            FirstName = "Bartek",
            LastName = "Biologiczny",
            HireDate = new DateOnly(2015, 9, 1)
        };
        await userManager.CreateAsync(teacher1, "Math123!");
        await userManager.CreateAsync(teacher2, "Bio123!");
        await userManager.AddToRoleAsync(teacher1, "Teacher");
        await userManager.AddToRoleAsync(teacher2, "Teacher");

        await db.SaveChangesAsync();

        // === CLASSES ===
        var classes = new[]
        {
            new Class { Name = "4A", Year = 2024, TeacherId = teacher1.Id },
            new Class { Name = "5B", Year = 2024, TeacherId = teacher2.Id },
            new Class { Name = "6A", Year = 2024, TeacherId = teacher1.Id },
            new Class { Name = "8B", Year = 2024, TeacherId = teacher2.Id }
        };
        db.Classes.AddRange(classes);
        await db.SaveChangesAsync();

        // === STUDENTS ===
        var students = new List<ApplicationUser>();
        int studentCounter = 1;
        foreach (var c in classes)
        {
            for (int i = 0; i < 5; i++)
            {
                var student = new ApplicationUser
                {
                    UserName = $"student{studentCounter}@school.local",
                    Email = $"student{studentCounter}@school.local",
                    EmailConfirmed = true,
                    FirstName = $"Uczeń{studentCounter}",
                    LastName = "Klasowy",
                    ClassId = c.Id,
                    EnrollmentDate = new DateOnly(2022, 9, 1),
                    BirthDate = new DateOnly(2010, 1, 1).AddYears(-(c.Year - 2024))
                };
                var studentResult = await userManager.CreateAsync(student, "Student123!");
                if (studentResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(student, "Student");
                    students.Add(student);
                }
                else
                {
                    foreach (var error in studentResult.Errors)
                        Console.WriteLine($"❌ Student {student.UserName} error: {error.Description}");
                }

                studentCounter++;
            }
        }

        await db.SaveChangesAsync();

        // === SUBJECTS ===
        var subjects = new[]
        {
            new Subject { Id = "MATH", Name = "Matematyka", ShortName = "MATH" },
            new Subject { Id = "BIO", Name = "Biologia", ShortName = "BIO" },
            new Subject { Id = "HIST", Name = "Historia", ShortName = "HIST" },
            new Subject { Id = "POL", Name = "Polski", ShortName = "POL" },
        };
        db.Subjects.AddRange(subjects);
        await db.SaveChangesAsync();

        // === TEACHER SUBJECTS ===
        var math = subjects.First(s => s.Id == "MATH");
        var bio = subjects.First(s => s.Id == "BIO");

        var teacherSubjects = new[]
        {
            new TeacherSubject { Id = Guid.NewGuid().ToString(), TeacherId = teacher1.Id, SubjectId = math.Id, ClassId = classes[0].Id },
            new TeacherSubject { Id = Guid.NewGuid().ToString(), TeacherId = teacher2.Id, SubjectId = bio.Id, ClassId = classes[1].Id }
        };
        db.TeacherSubjects.AddRange(teacherSubjects);
        await db.SaveChangesAsync();

        // === GRADES ===
        var grades = new List<Grade>();
        foreach (var student in students)
        {
            grades.Add(new Grade
            {
                StudentId = student.Id,
                TeacherId = teacher1.Id,
                SubjectId = math.Id,
                Value = 5,
                Description = "Test z algebry",
                Weight = 2,
                DateGiven = DateTime.UtcNow
            });
        }
        db.Grades.AddRange(grades);
        await db.SaveChangesAsync();

        // === ATTENDANCE ===
        var attendances = new List<Attendance>();
        foreach (var student in students)
        {
            attendances.Add(new Attendance
            {
                StudentId = student.Id,
                SubjectId = math.Id,
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = "Obecny"
            });
        }
        db.Attendances.AddRange(attendances);
        await db.SaveChangesAsync();
    }
}
