using Microsoft.EntityFrameworkCore;
using Tidsregistrering.Models;

namespace Tidsregistrering.Data
{
    public class TidsregistreringContext : DbContext
    {
        public TidsregistreringContext(DbContextOptions<TidsregistreringContext> options)
            : base(options)
        {
        }

        public DbSet<Registrering> Registreringer { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Registrering>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Afdeling).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Brugernavn).IsRequired().HasMaxLength(50);
                entity.Property(e => e.FuldeNavn).HasMaxLength(100);
                entity.Property(e => e.OuAfdeling).HasMaxLength(100);
                entity.Property(e => e.Bemærkninger).HasMaxLength(1000);
            });
        }
    }
}