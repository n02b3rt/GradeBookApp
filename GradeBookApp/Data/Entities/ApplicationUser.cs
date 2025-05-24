using Microsoft.AspNetCore.Identity;

namespace GradeBookApp.Data.Entities;

public class ApplicationUser : IdentityUser
{
    // Dodatkowe informacje o użytkowniku
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public DateOnly? BirthDate { get; set; }
    public string? Gender { get; set; }
    public string? Address { get; set; }
    public string? PESEL { get; set; }

    // Tylko dla studentów
    public DateOnly? EnrollmentDate { get; set; }

    // Tylko dla nauczycieli
    public DateOnly? HireDate { get; set; }

    // Klasa przypisana (dla ucznia)
    public int? ClassId { get; set; }
    public Class? Class { get; set; }

    // Nawigacje
    public ICollection<Grade> Grades { get; set; } = new List<Grade>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
}