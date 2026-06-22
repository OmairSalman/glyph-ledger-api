using GlyphLedger.Api.Data;
using GlyphLedger.Api.DTOs;
using GlyphLedger.Api.Models;
using GlyphLedger.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GlyphLedger.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ITokenService _tokenService;
        private readonly IConfiguration _config;

        public AuthController(AppDbContext context, ITokenService tokenService, IConfiguration config)
        {
            _context = context;
            _tokenService = tokenService;
            _config = config;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            bool exists = await _context.Users.AnyAsync(u => u.Username == request.Username);
            if (exists) return Conflict("Username is already taken");
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
            User user = new()
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Username = request.Username,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };
            user.UpdatedAt = user.CreatedAt;
            _context.Users.Add(user);
            var (accessToken, refreshToken) = IssueTokensFor(user.Id);
            await _context.SaveChangesAsync();
            return Ok(new
            {
                user = new { user.Id, user.Username, user.Name },
                accessToken,
                refreshToken
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);
            if (user == null) return Unauthorized("Invalid username or password");
            var passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!passwordValid) return Unauthorized("Invalid username or password");
            var (accessToken, refreshToken) = IssueTokensFor(user.Id);
            await _context.SaveChangesAsync();
            return Ok(new
            {
                user = new { user.Id, user.Username, user.Name },
                accessToken,
                refreshToken
            });
        }

        private (string accessToken, string refreshToken) IssueTokensFor(Guid userId)
        {
            var accessToken = _tokenService.CreateAccessToken(userId);
            var refreshToken = _tokenService.CreateRefreshToken();
            RefreshToken rt = new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(_config.GetValue<int>("JwtSettings:RefreshTokenDays"))
            };
            _context.RefreshTokens.Add(rt);
            return (accessToken, refreshToken);
        }
    }
}