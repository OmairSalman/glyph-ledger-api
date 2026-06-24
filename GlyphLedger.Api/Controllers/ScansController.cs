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
    public class ScansController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ScansController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("my-scans")]
        [Authorize]
        public async Task<IActionResult> MyScans()
        {
            var sub = User.FindFirst("sub")?.Value!;
            var userId = Guid.Parse(sub);
            var userScans = await _context.Scans
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new { s.Id, s.Content, s.Format, s.ValueType, s.CreatedAt })
                .ToListAsync();
            return Ok(new { scans = userScans });
        }

        [HttpPost("save-scan")]
        [Authorize]
        public async Task<IActionResult> SaveScan(SaveScanRequest request)
        {
            var sub = User.FindFirst("sub")?.Value!;
            var userId = Guid.Parse(sub);
            Scan scan = new()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Content = request.Content,
                Format = request.Format,
                ValueType = request.ValueType,
                CreatedAt = DateTime.UtcNow
            };
            _context.Scans.Add(scan);
            await _context.SaveChangesAsync();
            return Ok(new { scan });
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteScan(Guid id)
        {
            var sub = User.FindFirst("sub")?.Value!;
            var userId = Guid.Parse(sub);
            var scan = await _context.Scans.FirstOrDefaultAsync(s => s.Id == id && s.UserId == userId);
            if (scan == null) return NotFound("Scan not found");
            _context.Scans.Remove(scan);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}