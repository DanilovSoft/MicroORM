using Microsoft.EntityFrameworkCore;

namespace ConsoleTest
{
    public partial class EfDbContext : DbContext
    {
        public DbSet<BlogDb> Blogs => Set<BlogDb>();
        public DbSet<CategoryDb> Categories => Set<CategoryDb>();
        public DbSet<BlogCategoryDb> BlogCategories => Set<BlogCategoryDb>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            BlogDb.OnModelCreating(builder);
            BlogCategoryDb.OnModelCreating(builder);
            CategoryDb.OnModelCreating(builder);

            builder.Entity<GalleryDb>().HasNoKey();
            builder.Entity<Program.GalleryRec>().HasNoKey();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
            builder.UseNpgsql(Program.PgConnectionString);
        }
    }
}
