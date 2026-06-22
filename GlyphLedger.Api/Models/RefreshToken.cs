namespace GlyphLedger.Api.Models
{
    public class RefreshToken
    {
        public Guid Id { get; set; }
        public required Guid UserId { get; set; }
        //navigation property, so EF turns UserId into an FK
        public User User { get; set; } = null!;
        public required string Token { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}