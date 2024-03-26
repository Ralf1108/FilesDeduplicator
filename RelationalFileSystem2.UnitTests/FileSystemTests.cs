using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace RelationalFileSystem2.UnitTests
{
    public class FileSystemTests : IDisposable, IAsyncDisposable
    {
        private DatabaseContext _context;

        private void Setup()
        {
            _context = new DatabaseContext();
            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
        }

        [Fact]
        public async Task ExistsRootFolder()
        {
            // Arrange
            Setup();

            // Assert
            var loadedFolder = await _context.Folders.SingleAsync();
            loadedFolder.Id.Should().Be(FileSystem.RootFolderId);
            loadedFolder.Name.Should().Be(FileSystem.RootFolderName);
            loadedFolder.ParentId.Should().BeNull();
        }

        [Fact]
        public async Task CanAddFile()
        {
            // Arrange
            Setup();

            var fileName = "Test1";

            // Act
            var fileSystem = CreateFileSystem();
            var fileId = await fileSystem.AddFile(fileName, FileSystem.RootFolderId);

            // Assert
            ClearContext();
            var loadedFile = await _context.Files.SingleAsync();
            loadedFile.Id.Should().Be(fileId);
            loadedFile.Name.Should().Be(fileName);
            loadedFile.FolderEntryId.Should().Be(FileSystem.RootFolderId);
        }

        [Fact]
        public async Task CanAddFolder()
        {
            // Arrange
            Setup();

            var folderName = "Test1";

            // Act
            var fileSystem = CreateFileSystem();
            var folderId = await fileSystem.AddFolder(folderName, FileSystem.RootFolderId);

            // Assert
            ClearContext();
            var loadedFolder = await _context.Folders.SingleAsync(x => x.Name == folderName);
            loadedFolder.Id.Should().Be(folderId);
            loadedFolder.Name.Should().Be(folderName);
            loadedFolder.ParentId.Should().Be(FileSystem.RootFolderId);
        }

        [Fact]
        public async Task CanAddCascadedFolders()
        {
            // Arrange
            Setup();

            var maxFolderDepth = 10;
            var folderNamePrefix = "Test";

            // Act
            var fileSystem = CreateFileSystem();
            var folderIds = new List<Guid> { FileSystem.RootFolderId };
            for (var i = 1; i <= maxFolderDepth; i++)
            {
                var parentFolderId = folderIds.Last();
                var folderId = await fileSystem.AddFolder(folderNamePrefix + i, parentFolderId);
                folderIds.Add(folderId);
            }

            // Assert
            ClearContext();
            var loadedFolders = await _context.Folders
                .OrderBy(x => x.Depth)
                .ToListAsync();

            for (var i = 0; i < loadedFolders.Count; i++)
            {
                var loadedFolder = loadedFolders[i];
                loadedFolder.Id.Should().Be(folderIds[i]);
                loadedFolder.Depth.Should().Be(i);

                if (i == 0)
                    loadedFolder.Name.Should().Be(FileSystem.RootFolderName);
                else
                    loadedFolder.Name.Should().Be(folderNamePrefix + i);
            }
        }

        [Fact]
        public async Task CanCreateFolderByPath()
        {
            // Arrange
            Setup();

            var maxDepth = 10;
            var path = Path.DirectorySeparatorChar + string.Join(Path.DirectorySeparatorChar, Enumerable.Range(0, maxDepth).Select(x => x.ToString()));

            // Act
            var fileSystem = CreateFileSystem();
            var folderId = await fileSystem.CreateFolder(path);

            // Assert
            ClearContext();
            var loadedFolder = await _context.Folders.SingleOrDefaultAsync(x => x.Id == folderId);
            loadedFolder.Should().NotBeNull();

            var otherFolders = await _context.Folders
                .OrderBy(x => x.Depth)
                .ToListAsync();
            otherFolders.Should().HaveCount(maxDepth + 1);
            for (var i = 0; i < maxDepth; i++)
            {
                otherFolders[i + 1].Name.Should().Be(i.ToString());
            }
        }

        [Fact]
        public async Task CanCreateFolderByPathWithSameSubfolderNames()
        {
            // Arrange
            Setup();

            var path1 = Path.DirectorySeparatorChar + string.Join(Path.DirectorySeparatorChar, "1", "2", "3", "4");
            var path2 = Path.DirectorySeparatorChar + string.Join(Path.DirectorySeparatorChar, "5", "2", "3", "6");

            // Act
            var fileSystem = CreateFileSystem();
            var folderId1 = await fileSystem.CreateFolder(path1);
            var folderId2 = await fileSystem.CreateFolder(path2);

            // Assert
            ClearContext();
            var fileSystem2 = CreateFileSystem();

            var folders1 = await fileSystem2.GetFolders(path1);
            folders1.Should().HaveCount(5);
            folders1[0].Id.Should().Be(folderId1);
            folders1[0].Name.Should().Be("4");
            folders1[0].Name.Should().Be("3");
            folders1[1].Name.Should().Be("2");
            folders1[2].Name.Should().Be("1");
            folders1[3].Name.Should().Be(FileSystem.RootFolderName);

            var folders2 = await fileSystem2.GetFolders(path2);
            folders2.Should().HaveCount(5);
            folders2[0].Id.Should().Be(folderId2);
            folders2[0].Name.Should().Be("6");
            folders2[0].Name.Should().Be("3");
            folders2[1].Name.Should().Be("2");
            folders2[2].Name.Should().Be("5");
            folders2[3].Name.Should().Be(FileSystem.RootFolderName);
        }

        private FileSystem CreateFileSystem()
        {
            ClearContext();
            return new FileSystem(_context);
        }

        private void ClearContext()
        {
            _context.ChangeTracker.Clear();
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public async ValueTask DisposeAsync()
        {
            await _context.DisposeAsync();
        }
    }
}