using DebugTest;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleTest
{
    [Table("BlogCategory", Schema = "b")]
    public class BlogCategoryDb
    {
        public virtual int Id { get; set; }

        public virtual int BlogId { get; set; }

        public virtual int CategoryId { get; set; }

        public virtual BlogDb Blog { get; set; }

        public virtual CategoryDb Category { get; set; }

        public TestNonFlaggedEnum? TestFlags { get; set; }

        public static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BlogCategoryDb>().HasKey(entity => entity.Id);

            modelBuilder.Entity<BlogCategoryDb>()
                .HasOne(blogCategory => blogCategory.Blog)
                .WithMany(blog => blog.BlogCategories)
                .HasPrincipalKey(blog => blog.Id)
                .HasForeignKey(blogCategory => blogCategory.BlogId);

            modelBuilder.Entity<BlogCategoryDb>()
                .HasOne(blogCategory => blogCategory.Category)
                .WithMany()
                .HasPrincipalKey(category => category.Id)
                .HasForeignKey(blog => blog.CategoryId);
        }
    }
}