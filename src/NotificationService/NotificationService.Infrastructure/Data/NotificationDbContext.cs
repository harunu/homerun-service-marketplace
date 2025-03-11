using Microsoft.EntityFrameworkCore;
using NotificationService.Core.Entities;

namespace NotificationService.Infrastructure.Data
{
    /// <summary>
    /// Represents the database context for notifications.
    /// </summary>
    public class NotificationDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationDbContext"/> class.
        /// </summary>
        /// <param name="options">Database context options.</param>
        public NotificationDbContext(DbContextOptions<NotificationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the Notifications table.
        /// </summary>
        public DbSet<Notification> Notifications { get; set; } = null!;

        /// <summary>
        /// Configures the entity model for the database.
        /// </summary>
        /// <param name="modelBuilder">The model builder used to configure entities.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Notification>()
                .HasKey(n => n.Id);

            modelBuilder.Entity<Notification>()
                .Property(n => n.ServiceProviderId)
                .IsRequired();

            modelBuilder.Entity<Notification>()
                .Property(n => n.Message)
                .IsRequired();

            modelBuilder.Entity<Notification>()
                .Property(n => n.CreatedAt)
                .IsRequired();

            modelBuilder.Entity<Notification>()
                .Property(n => n.IsRead)
                .IsRequired();
        }
    }
}
