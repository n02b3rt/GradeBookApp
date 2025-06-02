// GradeBookApp.Data.Seed/DbSeeder.cs

using GradeBookApp.Data.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GradeBookApp.Data.Seed
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            // 1. Utwórz role (Admin, Teacher, Student), jeśli nie istnieją
            var roles = new[] { "Admin", "Teacher", "Student" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // 2. Jeśli baza już zawiera jakichkolwiek użytkowników, zakończ seeda
            if (await db.Users.AnyAsync())
                return;

            // === 3. ADMIN ===
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
                await userManager.AddToRoleAsync(admin, "Admin");
            else
                foreach (var err in adminResult.Errors)
                    Console.WriteLine($"❌ Błąd tworzenia Admina: {err.Description}");

            // === 4. NAUCZYCIELE PRZEDMIOTÓW ===
            // Tworzymy nauczycieli dla każdego z głównych przedmiotów
            var subjectTeachers = new List<(string login, string email, string first, string last, string subjectId)>() 
            {
                ("pol@school.local", "pol@school.local", "Paweł",       "Polonistyczny", "POL"),
                ("math@school.local", "math@school.local", "Maria",     "Matematyczna",  "MATH"),
                ("eng@school.local", "eng@school.local", "Ewa",         "Angielska",     "ENG"),
                ("info@school.local", "info@school.local", "Igor",      "Informatyczny", "INFO"),
                ("tech@school.local", "tech@school.local", "Tomasz",    "Techniczny",    "TECH"),
                ("geog@school.local", "geog@school.local", "Grażyna",   "Geograficzna",  "GEOG"),
                ("bio@school.local", "bio@school.local", "Bartek",      "Biologiczny",   "BIO"),
                ("chem@school.local", "chem@school.local", "Celina",    "Chemiczna",     "CHEM"),
                ("phys@school.local", "phys@school.local", "Piotr",     "Fizyczny",      "PHYS"),
                ("hist@school.local", "hist@school.local", "Helena",    "Historyczna",   "HIST"),
                ("wos@school.local", "wos@school.local", "Wojtek",      "Społeczny",     "WOS"),
                ("mus@school.local", "mus@school.local", "Monika",      "Muzyczna",      "MUS"),
                ("art@school.local", "art@school.local", "Andrzej",     "Artystyczny",   "ART"),
                ("pe@school.local", "pe@school.local", "Patryk",       "Sportowy",      "PE"),
                ("edb@school.local", "edb@school.local", "Edyta",      "Bezpieczna",    "EDB")
            };

            var teacherUsers = new List<ApplicationUser>();
            foreach (var (login, email, first, last, subjId) in subjectTeachers)
            {
                var t = new ApplicationUser
                {
                    UserName = login,
                    Email = email,
                    EmailConfirmed = true,
                    FirstName = first,
                    LastName = last,
                    HireDate = new DateOnly(2012, 9, 1) // przykładowa data zatrudnienia
                };
                var res = await userManager.CreateAsync(t, $"{subjId}haslo123!");
                if (res.Succeeded)
                {
                    await userManager.AddToRoleAsync(t, "Teacher");
                    teacherUsers.Add(t);
                }
                else
                {
                    foreach (var err in res.Errors)
                        Console.WriteLine($"❌ Błąd tworzenia nauczyciela {login}: {err.Description}");
                }
            }

            // Po utworzeniu nauczycieli zapisujemy zmiany, aby mieli wygenerowane Id
            await db.SaveChangesAsync();

            // === 5. KLASY ===
            // Użyjemy niektórych nauczycieli przedmiotów jako wychowawców klas:
            //  - mathTeacher jako wychowawca 4A
            //  - bioTeacher jako wychowawca 5B
            //  - histTeacher jako wychowawca 6A
            //  - polTeacher jako wychowawca 8B

            var mathTeacher = teacherUsers.First(u => u.UserName == "math@school.local");
            var bioTeacher  = teacherUsers.First(u => u.UserName == "bio@school.local");
            var histTeacher = teacherUsers.First(u => u.UserName == "hist@school.local");
            var polTeacher  = teacherUsers.First(u => u.UserName == "pol@school.local");

            var classes = new[]
            {
                new Class { Name = "4A", Year = 2024, TeacherId = mathTeacher.Id },
                new Class { Name = "5B", Year = 2024, TeacherId = bioTeacher.Id },
                new Class { Name = "6A", Year = 2024, TeacherId = histTeacher.Id },
                new Class { Name = "8B", Year = 2024, TeacherId = polTeacher.Id }
            };
            db.Classes.AddRange(classes);
            await db.SaveChangesAsync();

            // === 6. Uczniowie ===
            // Tworzymy dużo uczniów: 20 uczniów na każdą klasę (łącznie 80 uczniów)
            var students = new List<ApplicationUser>();
            int studentCounter = 1;
            foreach (var c in classes)
            {
                for (int i = 0; i < 20; i++)
                {
                    var student = new ApplicationUser
                    {
                        UserName = $"student{studentCounter}@school.local",
                        Email = $"student{studentCounter}@school.local",
                        EmailConfirmed = true,
                        FirstName = $"Uczeń{studentCounter}",
                        LastName = $"Klasowy{c.Name}",
                        ClassId = c.Id,
                        EnrollmentDate = new DateOnly(2022, 9, 1),
                        // Jeśli klasa ma Year=2024, to uczniowie mają około lat:
                        BirthDate = new DateOnly(2014, 1, 1).AddYears(-(c.Year - 2024))
                    };
                    var studRes = await userManager.CreateAsync(student, "Student123!");
                    if (studRes.Succeeded)
                    {
                        await userManager.AddToRoleAsync(student, "Student");
                        students.Add(student);
                    }
                    else
                    {
                        foreach (var err in studRes.Errors)
                            Console.WriteLine($"❌ Błąd tworzenia Ucznia {student.UserName}: {err.Description}");
                    }
                    studentCounter++;
                }
            }
            await db.SaveChangesAsync();

            // === 7. PRZEDMIOTY ===
            var subjects = new[]
            {
                new Subject { Id = "POL",  Name = "Język polski",     ShortName = "POL"  },
                new Subject { Id = "MATH", Name = "Matematyka",       ShortName = "MATH" },
                new Subject { Id = "ENG",  Name = "Język angielski",  ShortName = "ENG"  },
                new Subject { Id = "INFO", Name = "Informatyka",      ShortName = "INFO" },
                new Subject { Id = "TECH", Name = "Technika",         ShortName = "TECH" },
                new Subject { Id = "GEOG", Name = "Geografia",        ShortName = "GEOG" },
                new Subject { Id = "BIO",  Name = "Biologia",         ShortName = "BIO"  },
                new Subject { Id = "CHEM", Name = "Chemia",           ShortName = "CHEM" },
                new Subject { Id = "PHYS", Name = "Fizyka",           ShortName = "PHYS" },
                new Subject { Id = "HIST", Name = "Historia",         ShortName = "HIST" },
                new Subject { Id = "WOS",  Name = "Wiedza o społeczeństwie", ShortName = "WOS"  },
                new Subject { Id = "MUS",  Name = "Muzyka",           ShortName = "MUS"  },
                new Subject { Id = "ART",  Name = "Plastyka",         ShortName = "ART"  },
                new Subject { Id = "PE",   Name = "Wychowanie fizyczne",    ShortName = "PE"   },
                new Subject { Id = "EDB",  Name = "Edukacja dla bezpieczeństwa", ShortName = "EDB"  }
            };
            db.Subjects.AddRange(subjects);
            await db.SaveChangesAsync();

            // === 8. NAUCZYCIEL-PRZEDMIOT-KLASA (TeacherSubject) ===
            // Każdy nauczyciel przedmiotu prowadzi swój przedmiot we wszystkich klasach
            var teacherSubjects = new List<TeacherSubject>();
            foreach (var subj in subjects)
            {
                // Znajdź odpowiedniego nauczyciela z listy teacherUsers
                var teacherForThisSubject = teacherUsers.First(u =>
                    u.UserName.StartsWith(subj.ShortName.ToLower())); 
                // np. u.UserName == "math@school.local" dla subj.ShortName=="MATH"

                foreach (var c in classes)
                {
                    teacherSubjects.Add(new TeacherSubject
                    {
                        Id = Guid.NewGuid().ToString(),
                        TeacherId = teacherForThisSubject.Id,
                        SubjectId = subj.Id,
                        ClassId = c.Id
                    });
                }
            }
            db.TeacherSubjects.AddRange(teacherSubjects);
            await db.SaveChangesAsync();

            // === 9. OCENY (Grades) ===
            // Dodajemy każdemu uczniowi po ocenie z każdego przedmiotu (80 uczniów × 15 przedmiotów = 1200 ocen)
            var grades = new List<Grade>();
            var rng = new Random();
            foreach (var student in students)
            {
                foreach (var subj in subjects)
                {
                    // Znajdź nauczyciela prowadzącego ten przedmiot w klasie ucznia
                    var teacherSubjectLink = teacherSubjects
                        .First(ts => ts.SubjectId == subj.Id && ts.ClassId == student.ClassId);
                    grades.Add(new Grade
                    {
                        StudentId = student.Id,
                        TeacherId = teacherSubjectLink.TeacherId,
                        SubjectId = subj.Id,
                        Value = rng.Next(2, 7), // losowo od 2 do 6
                        Description = $"Ocena z {subj.Name}",
                        Weight = rng.Next(1, 4), // losowo waga 1–3
                        DateGiven = DateTime.UtcNow.AddDays(-rng.Next(0, 30)) // losowa data w ciągu ostatnich 30 dni
                    });
                }
            }
            db.Grades.AddRange(grades);
            await db.SaveChangesAsync();

            // === 10. OBECNOŚCI (Attendance) ===
            // Dodajemy dla każdego ucznia po jednym wpisie obecności na każdy przedmiot na dzisiejszą datę
            var attendances = new List<Attendance>();
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            foreach (var student in students)
            {
                foreach (var subj in subjects)
                {
                    attendances.Add(new Attendance
                    {
                        StudentId = student.Id,
                        SubjectId = subj.Id,
                        Date = today,
                        Status = "Obecny"
                    });
                }
            }
            db.Attendances.AddRange(attendances);
            await db.SaveChangesAsync();
        }
    }
}
