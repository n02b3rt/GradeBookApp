using System.ComponentModel.DataAnnotations;

namespace GradeBookApp.Shared
{
    public class ClassDto
    {
        public int Id { get; set; }
        
        [Required(ErrorMessage = "Nazwa klasy jest wymagana.")]
        [RegularExpression("^[4-8][A-F]$", 
            ErrorMessage = "Nazwa klasy musi składać się z cyfry 4–8 i litery A–F (np. 4A, 6C).")]
        public string Name { get; set; } = null!; 
        
        public int Year { get; set; }
        
        [Required(ErrorMessage = "Wybór wychowawcy jest obowiązkowy.")]
        public string TeacherId { get; set; } = null!; 
        
        public List<string> StudentIds { get; set; } = new();
        public UserDto? Teacher { get; set; }
        
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            int currentYear = DateTime.Now.Year;
            int minYear = currentYear - 4;  // np. jeśli teraz 2025, to minYear = 2021
            int maxYear = currentYear;      // 2025

            if (Year < minYear || Year > maxYear)
            {
                yield return new ValidationResult(
                    $"Rok musi być pomiędzy {minYear} a {maxYear}.", 
                    new[] { nameof(Year) }
                );
            }
        }
    }
}