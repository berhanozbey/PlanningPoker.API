using Microsoft.EntityFrameworkCore;
using PlanningPoker.API.Models;

namespace PlanningPoker.API.Data
{
    // DbContext'ten miras alıyoruz ki EF Core özelliklerini kullanalım
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // Veri tabanında oluşacak tablolarımız:
        public DbSet<Room> Rooms { get; set; }
        public DbSet<User> Users { get; set; }
    }
}