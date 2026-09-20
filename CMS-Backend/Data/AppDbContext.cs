using CMS_Backend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CMS_Backend.Data
{
    public class AppDbContext : IdentityDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public  DbSet<ApiActivityLog> ApiActivityLog { get; set; }
        public  DbSet<ApiDataChangeLog> ApiDataChangeLog { get; set; }
        public  DbSet<RefreshToken> RefreshTokens { get; set; }


        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //}
    }
}
