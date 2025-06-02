using System.ComponentModel.DataAnnotations;

namespace GradeBookApp.Shared;

public class GradeDto
{
    public int Id { get; set; }
    
    [Required]
    [Range(0, 6)]
    public int Value { get; set; }
    
    [Required]
    [Range(1, 3)]
    public int Weight { get; set; }
    
    public string Description { get; set; } = "";

    public DateTime DateGiven { get; set; }

    [Required]
    public string StudentId { get; set; } = "";
    
    [Required]
    public string SubjectId { get; set; } = "";
    
    [Required]
    public string TeacherId { get; set; } = "";
    
    public int ClassId { get; set; } 

}