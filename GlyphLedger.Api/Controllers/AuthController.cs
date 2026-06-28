using GlyphLedger.Api.Data;
using GlyphLedger.Api.DTOs;
using GlyphLedger.Api.Models;
using GlyphLedger.Api.Services;
using Microsoft.AspNetCore.Authorization;
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
            if (request.Password.Length < 6) return BadRequest("Password must be at least 6 characters long");
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

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshRequest request)
        {
            var rt = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == request.RefreshToken);
            if (rt == null) return Unauthorized("Invalid refresh token");
            if (rt.ExpiresAt <= DateTime.UtcNow)
            {
                _context.RefreshTokens.Remove(rt);
                await _context.SaveChangesAsync();
                return Unauthorized("Invalid refresh token");
            }
            var userId = rt.UserId;
            var (accessToken, refreshToken) = IssueTokensFor(userId);
            _context.RefreshTokens.Remove(rt);
            await _context.SaveChangesAsync();
            return Ok(new
            {
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

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            var sub = User.FindFirst("sub")?.Value!;
            var userId = Guid.Parse(sub);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");
            return Ok(new
            {
                user.Id,
                user.Name,
                user.Username
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout(LogoutRequest request)
        {
            var sub = User.FindFirst("sub")?.Value!;
            var userId = Guid.Parse(sub);
            var rt = await _context.RefreshTokens.FirstOrDefaultAsync(r => r.Token == request.RefreshToken && r.UserId == userId);
            if (rt != null)
            {
                _context.RefreshTokens.Remove(rt);
                await _context.SaveChangesAsync();
            }
            return NoContent();
        }

        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
        {
            var sub = User.FindFirst("sub")?.Value!;
            var userId = Guid.Parse(sub);
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return NotFound("User not found");
            var passwordValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash);
            if (!passwordValid) return Unauthorized("Invalid current password");
            if (request.NewPassword == request.CurrentPassword) return BadRequest("New password is the same as the old password");
            if (request.NewPassword.Length < 6) return BadRequest("Password must be at least 6 characters long");
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;
            var userTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == userId)
                .ToListAsync();
            _context.RefreshTokens.RemoveRange(userTokens);
            var (accessToken, refreshToken) = IssueTokensFor(user.Id);
            await _context.SaveChangesAsync();
            return Ok(new
            {
                accessToken,
                refreshToken
            });
        }
    }
}