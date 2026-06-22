namespace GlyphLedger.Api.DTOs
{
    public class RegisterRequest
    {
        public required string Name { get; set; }
        public required string Username { get; set; }
        public required string Password { get; set; }
    }
}