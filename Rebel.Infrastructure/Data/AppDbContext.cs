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
        public DbSet<ReservationActivity> ReservationActivities { get; set; }
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

            builder.Entity<Category>(entity =>
            {
                entity.Property(c => c.DeletedAtUtc)
                    .HasColumnType("timestamp with time zone");

                entity.HasQueryFilter(c => !c.IsDeleted);
            });

            builder.Entity<Event>(entity =>
            {
                entity.Property(e => e.MaxReservations);

                entity.Property(e => e.MaxGuests);

                entity.Property(e => e.DeletedAtUtc)
                    .HasColumnType("timestamp with time zone");

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            builder.Entity<Product>(entity =>
            {
                entity.Property(p => p.DeletedAtUtc)
                    .HasColumnType("timestamp with time zone");

                entity.HasQueryFilter(p => !p.IsDeleted);
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

                entity.Property(r => r.ReservationCode)
                    .IsRequired()
                    .HasMaxLength(16);

                entity.Property(r => r.EmailStatus)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(r => r.CreatedAtUtc)
                    .HasColumnType("timestamp with time zone");

                entity.Property(r => r.RespondedAtUtc)
                    .HasColumnType("timestamp with time zone");

                entity.Property(r => r.LastEmailSentAtUtc)
                    .HasColumnType("timestamp with time zone");

                entity.Property(r => r.LastEmailError)
                    .HasMaxLength(500);

                entity.Property(r => r.DeletedAtUtc)
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

                entity.HasIndex(r => r.ReservationCode)
                    .IsUnique();

                entity.HasIndex(r => new
                {
                    r.ReservationDate,
                    r.ReservationTime,
                    r.TableLabel
                })
                .IsUnique()
                .HasFilter(
                    "\"TableLabel\" IS NOT NULL " +
                    "AND \"Status\" IN ('Approved', 'Arrived')");

                entity.HasQueryFilter(r => !r.IsDeleted);
            });

            builder.Entity<ReservationActivity>(entity =>
            {
                entity.Property(activity => activity.Title)
                    .IsRequired()
                    .HasMaxLength(80);

                entity.Property(activity => activity.Description)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(activity => activity.Actor)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(activity => activity.CreatedAtUtc)
                    .HasColumnType("timestamp with time zone");

                entity.HasIndex(activity => new
                {
                    activity.ReservationId,
                    activity.CreatedAtUtc
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
