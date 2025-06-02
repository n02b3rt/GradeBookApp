// ﻿GradeBookApp.Shared/UserDto.cs

using System;
using System.Collections.Generic;
using System.Linq;

namespace GradeBookApp.Shared
{
    public class UserDto
    {
        public string Id { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public DateOnly? BirthDate { get; set; }
        public string? Gender { get; set; }
        public string? Address { get; set; }
        public string? PESEL { get; set; }
        public DateOnly? EnrollmentDate { get; set; }
        public DateOnly? HireDate { get; set; }
        public int? ClassId { get; set; }

        // Lista ról przypisanych do użytkownika
        public List<string> Roles { get; set; } = new();

        // Pomocnicza właściwość łącząca wszystkie role w jeden ciąg (lub zwracająca "(brak)")
        public string RolesDisplay
        {
            get
            {
                if (Roles == null || !Roles.Any())
                    return "(brak)";
                return string.Join(", ", Roles);
            }
        }

        // (opcjonalnie) wygodna właściwość łącząca imię i nazwisko
        public string FullName => $"{FirstName} {LastName}";
    }
}