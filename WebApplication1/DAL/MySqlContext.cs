using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.DAL
{
    public class MySqlContext : DbContext
    {
        public MySqlContext(DbContextOptions<MySqlContext> dbContextOptions) : base(dbContextOptions)
        {
        }
        public DbSet<User> UserDb { get; set; }
        public DbSet<PlayList> PlayListDb { get; set; }
        public DbSet<Track> TrackDb { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
        }
    }
}
