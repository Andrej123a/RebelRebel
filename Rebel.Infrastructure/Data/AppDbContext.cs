using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Rebel.Domain.Entities;

namespace Rebel.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<ContactInfo> ContactInfos { get; set; }
        public DbSet<Reservation> Reservations { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Reservation>(entity =>
            {
                /*
                 * ReservationDate претставува само календарски датум,
                 * па во PostgreSQL го чуваме како date, а не timestamptz.
                 */
                entity.Property(r => r.ReservationDate)
                    .HasColumnType("date");

                entity.Property(r => r.ReservationTime)
                    .HasColumnType("time without time zone");

                entity.Property(r => r.CreatedAtUtc)
                    .HasColumnType("timestamp with time zone");

                entity.Property(r => r.RespondedAtUtc)
                    .HasColumnType("timestamp with time zone");

                entity.Property(r => r.Status)
                    .HasConversion<string>()
                    .HasMaxLength(20);

                entity.HasIndex(r => new
                {
                    r.Status,
                    r.ReservationDate
                });
            });
        }
    }
}