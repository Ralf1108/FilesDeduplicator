using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RelationalFileSystem.Entities;
using File = RelationalFileSystem.Entities.File;

namespace RelationalFileSystem;

public class DatabaseContext : DbContext
{
    public DbSet<Folder> Folders { get; set; } = null!;
    public DbSet<FolderName> FolderNames { get; set; } = null!;
    public DbSet<File> Files { get; set; } = null!;
    public DbSet<FileAttribute> FileAttributes { get; set; } = null!;

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<FileId>()
            .HaveConversion<FileId.EfCoreValueConverter>();
        configurationBuilder.Properties<FileAttributeId>()
            .HaveConversion<FileAttributeId.EfCoreValueConverter>();
        configurationBuilder.Properties<FileAttributeKey>()
            .HaveConversion<FileAttributeKey.EfCoreValueConverter>();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            .LogTo(s => Debug.WriteLine(s), LogLevel.Information)
            //.LogTo(Console.WriteLine, LogLevel.Information)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=FolderTest;Trusted_Connection=True;");

        base.OnConfiguring(optionsBuilder);
    }

    public IQueryable<File> GetFileQuery()
    {
        return Files;
    }

    public IQueryable<FileAttribute> GetFileAttributeQuery()
    {
        return FileAttributes;
    }
}