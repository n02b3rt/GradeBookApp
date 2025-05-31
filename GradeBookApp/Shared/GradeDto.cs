namespace GradeBookApp.Shared;

public class GradeDto
{
    public int Id { get; set; }
    public int Value { get; set; }
    public int Weight { get; set; }
    public string Description { get; set; } = "";
    public DateTime DateGiven { get; set; }

    public string StudentId { get; set; } = "";
    public string SubjectId { get; set; } = "";
    public string TeacherId { get; set; } = "";
}