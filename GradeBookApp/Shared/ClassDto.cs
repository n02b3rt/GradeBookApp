namespace GradeBookApp.Shared
{
    public class ClassDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;   // zabezpieczenie przed null, jeśli nullable enabled
        public int Year { get; set; }
        public string? TeacherId { get; set; }     // jeśli TeacherId może być null
        public List<string> StudentIds { get; set; } = new();
        public UserDto? Teacher { get; set; }
    }
}