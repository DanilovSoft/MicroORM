using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using DebugTest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.Extensions.Options;
using Npgsql;

namespace ConsoleTest;

public partial class EfDbContext : DbContext
{
    public DbSet<BlogDb> Blogs => Set<BlogDb>();
    public DbSet<CategoryDb> Categories => Set<CategoryDb>();
    public DbSet<BlogCategoryDb> BlogCategories => Set<BlogCategoryDb>();

    [ModuleInitializer]
    public static void InitModule()
    {
        NpgsqlConnection.GlobalTypeMapper.MapEnum<TestFlaggedEnum>();
    }

    //static LoggerFactory object
    public static readonly ILoggerFactory _loggerFactory = new LoggerFactory(new[] { new DebugLoggerProvider() });

    public EfDbContext()
    {

    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        BlogDb.OnModelCreating(builder);
        BlogCategoryDb.OnModelCreating(builder);
        CategoryDb.OnModelCreating(builder);

        //builder.HasPostgresEnum<TestFlaggedEnum>();

        builder.Entity<GalleryDb>().HasNoKey();
        builder.Entity<Program.GalleryRec>().HasNoKey();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder builder)
    {
        builder.UseNpgsql(Program.PgConnectionString);

        builder.UseLoggerFactory(_loggerFactory).EnableSensitiveDataLogging();
    }
}
