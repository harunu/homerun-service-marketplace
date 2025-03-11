using Microsoft.EntityFrameworkCore;
using RatingService.Core.Entities;

namespace RatingService.Infrastructure.Data
{
    /// <summary>
    /// Database context for handling Rating entities using Entity Framework Core.
    /// </summary>
    public class RatingDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="RatingDbContext"/>.
        /// </summary>
        /// <param name="options">Database options for configuring the context.</param>
        public RatingDbContext(DbContextOptions<RatingDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Represents the Ratings table in the database.
        /// </summary>
        public DbSet<Rating> Ratings { get; set; } = null!;

        /// <summary>
        /// Configures the entity model properties and constraints.
        /// </summary>
        /// <param name="modelBuilder">Model builder for defining entity configurations.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Rating>()
                .HasKey(r => r.Id); // Primary key

            modelBuilder.Entity<Rating>()
                .Property(r => r.Score)
                .IsRequired();

            modelBuilder.Entity<Rating>()
                .Property(r => r.ServiceProviderId)
                .IsRequired();

            modelBuilder.Entity<Rating>()
                .Property(r => r.CustomerId)
                .IsRequired();

            modelBuilder.Entity<Rating>()
                .Property(r => r.CreatedAt)
                .IsRequired();
        }
    }
}
