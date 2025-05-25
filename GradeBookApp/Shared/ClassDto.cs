namespace GradeBookApp.Shared
{
    public class ClassDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;   // zabezpieczenie przed null, jeśli nullable enabled
        public int Year { get; set; }
        public string? TeacherId { get; set; }     // jeśli TeacherId może być null
        public List<string> StudentIds { get; set; } = new();

        // Jeśli chcesz, możesz też dodać listę studentów jako obiektów, np:
        // public List<UserDto> Students { get; set; } = new();
    }
}