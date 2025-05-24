using System.ComponentModel.DataAnnotations;

namespace GradeBookApp.Data.Entities;

public class Grade
{
    public int Id { get; set; }

    [Range(1, 6)]
    public int Value { get; set; }

    public string Description { get; set; } = string.Empty;

    [Range(1, 5)]
    public int Weight { get; set; }

    public DateTime DateGiven { get; set; } = DateTime.UtcNow;

    [Required]
    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser Student { get; set; } = null!;

    [Required]
    public string SubjectId { get; set; } = string.Empty;
    public Subject Subject { get; set; } = null!;

    [Required]
    public string TeacherId { get; set; } = string.Empty;
    public ApplicationUser Teacher { get; set; } = null!;
}