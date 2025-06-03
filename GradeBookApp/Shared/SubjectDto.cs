using System.ComponentModel.DataAnnotations;

namespace GradeBookApp.Shared;

public class SubjectDto
{
    
    public string Id { get; set; } = "";
    
    [Required]
    public string Name { get; set; } = "";
    
    [Required]
    public string ShortName { get; set; } = "";
}