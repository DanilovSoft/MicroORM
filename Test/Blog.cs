using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConsoleTest
{
    [Table("Blog", Schema = "b")]
    public class Blog
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Article { get; set; }

        public DateTime? Publish { get; set; }

        public SlugType Slug { get; set; }

        public ICollection<BlogCategory> BlogCategories { get; set; }

        public static void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Blog>().HasKey(entity => entity.Id);
        }
    }

    public class SlugType
    {
        private readonly string _slug;

        public SlugType(string slug)
        {
            _slug = slug;
        }

        public static implicit operator SlugType(string slug) => new(slug);
    }
}
