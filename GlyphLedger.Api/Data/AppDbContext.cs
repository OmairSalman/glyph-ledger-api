using GlyphLedger.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace GlyphLedger.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Scan> Scans { get; set; } = null!;
        public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

        //forcing usernames and refresh tokens to be unique through a DB index, by overriding the OnModelCreating method from DbContext
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().HasIndex(user => user.Username).IsUnique();
            modelBuilder.Entity<RefreshToken>().HasIndex(rt => rt.Token).IsUnique();
        }
    }
}