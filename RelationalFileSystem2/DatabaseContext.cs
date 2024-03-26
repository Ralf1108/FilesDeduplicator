using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RelationalFileSystem2.Entities;

namespace RelationalFileSystem2;

public class DatabaseContext : DbContext
{
    public DbSet<FileEntry> Files { get; set; } = null!;
    public DbSet<FolderEntry> Folders { get; set; } = null!;
    //public DbSet<FileAttribute> FileAttributes { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            .LogTo(s => Debug.WriteLine(s), LogLevel.Information)
            .LogTo(Console.WriteLine, LogLevel.Information)
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=FolderTest;Trusted_Connection=True;");

        base.OnConfiguring(optionsBuilder);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<FolderEntry>()
            .HasData(new FolderEntry
            {
                Id = FileSystem.RootFolderId,
                Depth = 0,
                Name = FileSystem.RootFolderName
            });
    }
}