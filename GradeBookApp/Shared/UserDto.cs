using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace GradeBookApp.Shared
{
    public class UserDto : IValidatableObject
    {
        public string Id { get; set; } = null!;

        [Required(ErrorMessage = "Nazwa użytkownika jest wymagana.")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Nazwa użytkownika musi mieć od 3 do 50 znaków.")]
        public string UserName { get; set; } = null!;

        [Required(ErrorMessage = "Email jest wymagany.")]
        [EmailAddress(ErrorMessage = "Niepoprawny format email.")]
        [StringLength(100, ErrorMessage = "Email może mieć maksymalnie 100 znaków.")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Imię jest wymagane.")]
        [StringLength(30, MinimumLength = 2, ErrorMessage = "Imię musi mieć od 2 do 30 znaków.")]
        [RegularExpression(@"^[A-Za-zĄąĆćĘęŁłŃńÓóŚśŹźŻż\s'-]+$", ErrorMessage = "Imię może zawierać tylko litery, spacje, apostrofy i myślniki.")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Nazwisko jest wymagane.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Nazwisko musi mieć od 2 do 50 znaków.")]
        [RegularExpression(@"^[A-Za-zĄąĆćĘęŁłŃńÓóŚśŹźŻż\s'-]+$", ErrorMessage = "Nazwisko może zawierać tylko litery, spacje, apostrofy i myślniki.")]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "Data urodzenia jest wymagana.")]
        [DataType(DataType.Date)]
        public DateOnly? BirthDate { get; set; }

        [Required(ErrorMessage = "Płeć jest wymagana.")]
        [RegularExpression(@"^(Male|Female|Other)$", ErrorMessage = "Płeć musi być 'Male', 'Female' lub 'Other'.")]
        public string? Gender { get; set; }

        [Required(ErrorMessage = "Adres jest wymagany.")]
        [StringLength(200, ErrorMessage = "Adres może mieć maksymalnie 200 znaków.")]
        public string? Address { get; set; }

        [Required(ErrorMessage = "PESEL jest wymagany.")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "PESEL musi składać się z 11 cyfr.")]
        public string? PESEL { get; set; }

        [DataType(DataType.Date)]
        public DateOnly? EnrollmentDate { get; set; }

        [DataType(DataType.Date)]
        public DateOnly? HireDate { get; set; }

        public int? ClassId { get; set; }

        public List<string> Roles { get; set; } = new();

        public string RolesDisplay
        {
            get
            {
                if (Roles == null || !Roles.Any())
                    return "(brak)";
                return string.Join(", ", Roles);
            }
        }

        public string FullName => $"{FirstName} {LastName}";

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var results = new List<ValidationResult>();

            // 1) BirthDate musi być w przeszłości (co najmniej 3 lata temu)
            if (BirthDate.HasValue)
            {
                var today = DateOnly.FromDateTime(DateTime.Now);
                if (BirthDate.Value >= today)
                {
                    results.Add(new ValidationResult(
                        "Data urodzenia musi być datą w przeszłości.",
                        new[] { nameof(BirthDate) }));
                }
                else if ((today.Year - BirthDate.Value.Year) < 3)
                {
                    results.Add(new ValidationResult(
                        "Użytkownik musi mieć co najmniej 3 lata.",
                        new[] { nameof(BirthDate) }));
                }
            }

            // 2) Jeśli jest EnrollmentDate, to nie wcześniej niż po urodzeniu
            if (EnrollmentDate.HasValue && BirthDate.HasValue)
            {
                if (EnrollmentDate.Value <= BirthDate.Value)
                {
                    results.Add(new ValidationResult(
                        "Data zapisu nie może być wcześniejsza niż data urodzenia.",
                        new[] { nameof(EnrollmentDate) }));
                }
            }

            // 3) Jeśli jest HireDate, to nie wcześniej niż po urodzeniu i nie później niż dziś
            if (HireDate.HasValue && BirthDate.HasValue)
            {
                if (HireDate.Value <= BirthDate.Value)
                {
                    results.Add(new ValidationResult(
                        "Data zatrudnienia nie może być wcześniejsza niż data urodzenia.",
                        new[] { nameof(HireDate) }));
                }
                else if (HireDate.Value > DateOnly.FromDateTime(DateTime.Now))
                {
                    results.Add(new ValidationResult(
                        "Data zatrudnienia nie może być w przyszłości.",
                        new[] { nameof(HireDate) }));
                }
            }

            // 4) Nie pozwalamy na jednoczesne ustawienie EnrollmentDate i HireDate, jeżeli rola nie zawiera 'Student' lub 'Teacher'
            if (EnrollmentDate.HasValue && !Roles.Contains("Student"))
            {
                results.Add(new ValidationResult(
                    "Data zapisu może być ustawiona tylko dla użytkowników z rolą 'Student'.",
                    new[] { nameof(EnrollmentDate) }));
            }
            if (HireDate.HasValue && !Roles.Contains("Teacher"))
            {
                results.Add(new ValidationResult(
                    "Data zatrudnienia może być ustawiona tylko dla użytkowników z rolą 'Teacher'.",
                    new[] { nameof(HireDate) }));
            }

            // 5) ClassId może być ustawione tylko, jeśli rola to 'Student'
            if (ClassId.HasValue && !Roles.Contains("Student"))
            {
                results.Add(new ValidationResult(
                    "Klasa może być przypisana tylko użytkownikom z rolą 'Student'.",
                    new[] { nameof(ClassId) }));
            }

            return results;
        }
    }
}
