namespace GlyphLedger.Api.Models
{
    public class Scan
    {
        public Guid Id { get; set; }
        public required Guid UserId { get; set; }
        //navigation property, so EF turns UserId into an FK
        public User User { get; set; } = null!;
        public required string Content { get; set; }
        public required string Format { get; set; }
        public required string ValueType { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}