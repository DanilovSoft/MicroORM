using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleTest
{
    [Table("Category", Schema = "b")]
    public partial class Category
    {
        public virtual int Id { get; set; }

        public virtual string Name { get; set; }

        public virtual string Slug { get; set; }

        public static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Category>().HasKey(entity => entity.Id);

            modelBuilder.Entity<Category>().Property(category => category.Name).HasMaxLength(200);
        }
    }
}