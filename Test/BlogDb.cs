using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleTest
{
    [Table("Blog", Schema = "b")]
    public class BlogDb
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Article { get; set; }

        public DateTime? Publish { get; set; }

        public string Slug { get; set; }

        public ICollection<BlogCategoryDb> BlogCategories { get; set; }

        public static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<BlogDb>().HasKey(entity => entity.Id);
        }
    }
}
