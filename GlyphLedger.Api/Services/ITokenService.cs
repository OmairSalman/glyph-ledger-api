using GlyphLedger.Api.Models;

namespace GlyphLedger.Api.Services
{
    public interface ITokenService
    {
        string CreateAccessToken(Guid userId);
        string CreateRefreshToken();
    }
}