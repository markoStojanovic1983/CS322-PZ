using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RecipeSharingPlatform.Models;

namespace RecipeSharingPlatform.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Category> Categories { get; set; }
        public DbSet<Recipe> Recipes { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<RecipeStep> RecipeSteps { get; set; }
        public DbSet<UserFavorite> UserFavorites { get; set; }
        public DbSet<Rating> Ratings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>(entity =>
            {
                entity.Property(e => e.Id).HasMaxLength(127);
                entity.Property(e => e.NormalizedUserName).HasMaxLength(127);
                entity.Property(e => e.NormalizedEmail).HasMaxLength(127);
            });

            builder.Entity<UserFavorite>()
                .HasKey(uf => new { uf.UserID, uf.RecipeID });

            builder.Entity<Recipe>()
                .HasOne(r => r.Category)
                .WithMany(c => c.Recipes)
                .HasForeignKey(r => r.CategoryID);

            builder.Entity<Ingredient>()
                .HasOne(i => i.Recipe)
                .WithMany(r => r.Ingredients)
                .HasForeignKey(i => i.RecipeID);

            builder.Entity<RecipeStep>()
                .HasOne(rs => rs.Recipe)
                .WithMany(r => r.RecipeSteps)
                .HasForeignKey(rs => rs.RecipeID);

            builder.Entity<Rating>()
                .HasIndex(r => new { r.UserID, r.RecipeID })
                .IsUnique();
        }
    }
}