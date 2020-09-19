using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleTest
{
    [Table("BlogCategory", Schema = "b")]
    public partial class BlogCategory
    {
        public virtual int Id { get; set; }

        public virtual int BlogId { get; set; }

        public virtual int CategoryId { get; set; }

        public virtual Blog Blog { get; set; }

        public virtual Category Category { get; set; }

        public static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BlogCategory>().HasKey(entity => entity.Id);

            modelBuilder.Entity<BlogCategory>()
                .HasOne(blogCategory => blogCategory.Blog)
                .WithMany(blog => blog.BlogCategories)
                .HasPrincipalKey(blog => blog.Id)
                .HasForeignKey(blogCategory => blogCategory.BlogId);

            modelBuilder.Entity<BlogCategory>()
                .HasOne(blogCategory => blogCategory.Category)
                .WithMany()
                .HasPrincipalKey(category => category.Id)
                .HasForeignKey(blog => blog.CategoryId);
        }
    }
}