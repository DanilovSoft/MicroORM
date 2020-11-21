using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Test;

namespace ConsoleTest
{
    public partial class EfDbContext : DbContext
    {
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
