namespace GradeBookApp.Shared;

public class AttendanceDto
{
    public int Id { get; set; }

    public string StudentId { get; set; } = "";
    public string SubjectId { get; set; } = "";
    public DateOnly Date { get; set; }
    public string Status { get; set; } = "Obecny";
    public string? Note { get; set; }
}