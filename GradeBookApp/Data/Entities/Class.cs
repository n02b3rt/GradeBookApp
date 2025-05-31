using System.ComponentModel.DataAnnotations;

namespace GradeBookApp.Data.Entities;

public class Class
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = "";

    [Range(1900, 2100)]
    public int Year { get; set; }

    [Required]
    public string TeacherId { get; set; } = "";
    public ApplicationUser Teacher { get; set; } = null!;
}