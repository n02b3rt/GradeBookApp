namespace GradeBookApp.Data.Entities;

public class Attendance
{
    public int Id { get; set; }

    // Relacja do ucznia
    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = null!;

    // Relacja do przedmiotu
    public string SubjectId { get; set; } = string.Empty;
    public Subject Subject { get; set; } = null!;

    // Data obecności
    public DateOnly Date { get; set; }

    // Status obecności
    public string Status { get; set; } = "Obecny"; // Obecny / Nieobecny / Spóźniony

    // Notatka opcjonalna
    public string? Note { get; set; }
}