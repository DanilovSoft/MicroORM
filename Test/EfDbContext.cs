using Microsoft.EntityFrameworkCore;

namespace ConsoleTest
{
    public partial class EfDbContext : DbContext
    {
        public DbSet<Blog> Blogs => Set<Blog>();
        public DbSet<Category> Categories => Set<Category>();
        public DbSet<BlogCategory> BlogCategories => Set<BlogCategory>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            Blog.OnModelCreating(builder);
            BlogCategory.OnModelCreating(builder);
            Category.OnModelCreating(builder);

            builder.Entity<GalleryDb>().HasNoKey();
            builder.Entity<Program.GalleryRec>().HasNoKey();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
            builder.UseNpgsql(Program.PgConnectionString);
                //.UseSnakeCaseNamingConvention();
        }
    }
}
