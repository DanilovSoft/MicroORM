using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleTest
{
    [Table("Category", Schema = "b")]
    public partial class CategoryDb
    {
        public virtual int Id { get; set; }

        public virtual string Name { get; set; }

        public virtual string Slug { get; set; }

        public static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CategoryDb>().HasKey(entity => entity.Id);

            modelBuilder.Entity<CategoryDb>().Property(category => category.Name).HasMaxLength(200);
        }
    }
}