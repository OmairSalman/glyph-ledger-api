namespace GlyphLedger.Api.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Username { get; set; }
        public required string PasswordHash { get; set; }
        //optional navigation property; to access a user's scans if it's ever necessary through simply user.Scans
        public ICollection<Scan> Scans { get; set; } = new List<Scan>();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}