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
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<PubTable> PubTables { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<PubTable>(entity =>
            {
                entity.Property(table => table.Label)
                    .IsRequired()
                    .HasMaxLength(40);

                entity.Property(table => table.Area)
                    .HasMaxLength(80);

                entity.HasIndex(table => table.Label)
                    .IsUnique();
            });

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

                entity.Property(r => r.TableLabel)
                    .HasMaxLength(40);

                entity.Property(r => r.InternalNote)
                    .HasMaxLength(500);

                entity.HasIndex(r => new
                {
                    r.Status,
                    r.ReservationDate
                });
            });

            builder.Entity<Notification>(entity =>
            {
                entity.Property(n => n.Title)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(n => n.Message)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(n => n.Link)
                    .HasMaxLength(300);

                entity.Property(n => n.CreatedAt)
                    .HasColumnType("timestamp with time zone");

                entity.HasIndex(n => new
                {
                    n.IsRead,
                    n.CreatedAt
                });

                entity.HasOne(n => n.Reservation)
                    .WithMany()
                    .HasForeignKey(n => n.ReservationId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }
    }
}
