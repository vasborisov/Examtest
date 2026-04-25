using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using proekt_za_6ca.Data.Entities;

namespace proekt_za_6ca.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Конфигурация за Reservation
            // ========================
            builder.Entity<Reservation>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.Property(r => r.ReservationTime)
                    .IsRequired();

                entity.Property(r => r.PeopleCount)
                    .IsRequired();

                entity.Property(r => r.Comment)
                    .HasMaxLength(500);

                entity.Property(r => r.Status)
                    .IsRequired()
                    .HasConversion<string>();

                entity.Property(r => r.OwnerId)
                    .IsRequired();

                entity.Property(r => r.CreatedOn)
                    .IsRequired();

                // 🔗 Връзка с Restaurant
                entity.HasOne(r => r.Restaurants)
                    .WithMany(rest => rest.Reservations)
                    .HasForeignKey(r => r.RestaurantId)
                    .OnDelete(DeleteBehavior.Restrict);

                // 🔗 Връзка с User
                entity.HasOne(r => r.Owner)
                    .WithMany(u => u.Reservations)
                    .HasForeignKey(r => r.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ========================
            // Конфигурация за Restaurants
            // ========================
            builder.Entity<Restaurants>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.Property(r => r.Title)
                    .IsRequired()
                    .HasMaxLength(64);

                entity.Property(r => r.Description)
                    .HasMaxLength(255);

                entity.Property(r => r.Address)
                    .IsRequired();

                entity.Property(r => r.ImageUrl)
                    .IsRequired();

                entity.Property(r => r.Latitude)
                    .IsRequired();

                entity.Property(r => r.Longitude)
                    .IsRequired();

                entity.Property(r => r.OwnerId)
                    .IsRequired();

                entity.Property(r => r.CreatedOn)
                    .IsRequired();

                entity.HasOne(r => r.Owner)
                    .WithMany(u => u.Restaurants)
                    .HasForeignKey(r => r.OwnerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // ========================
            // Конфигурация за User
            // ========================
            builder.Entity<User>(entity =>
            {
                entity.Property(u => u.FirstName)
                    .HasMaxLength(255);

                entity.Property(u => u.LastName)
                    .HasMaxLength(255);
            });

        }
        public DbSet<Restaurants> Restaurants { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
    }
}
