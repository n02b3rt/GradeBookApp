using System.ComponentModel.DataAnnotations;

namespace GradeBookApp.Data.Entities;

public class Class
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty; // np. "4A", "5B"

    public int Year { get; set; } // np. 2024

    // Relacja do wychowawcy klasy
    public string TeacherId { get; set; } = string.Empty;
    public ApplicationUser Teacher { get; set; } = null!;

    // Lista uczniów przypisanych do klasy
    public ICollection<ApplicationUser> Students { get; set; } = new List<ApplicationUser>();
}