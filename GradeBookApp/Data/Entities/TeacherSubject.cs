using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GradeBookApp.Data.Entities;

public class TeacherSubject
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString(); // lub usuń i użyj klucza złożonego

    [Required]
    public string TeacherId { get; set; }
    public ApplicationUser Teacher { get; set; } = null!;

    [Required]
    public string SubjectId { get; set; }
    public Subject Subject { get; set; } = null!;

    [Required]
    public int ClassId { get; set; }  // POPRAWIONE: ClassId jako int
    public Class Class { get; set; } = null!;
}