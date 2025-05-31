namespace GradeBookApp.Shared;

public class TeacherSubjectDto
{
    public string Id { get; set; } = "";
    public string TeacherId { get; set; } = "";
    public string SubjectId { get; set; } = "";
    public int ClassId { get; set; }
}