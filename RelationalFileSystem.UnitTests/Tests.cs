using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RelationalFileSystem.Entities;
using RelationalFileSystem.Extensions;
using File = RelationalFileSystem.Entities.File;

namespace RelationalFileSystem.UnitTests
{
    public class Tests
    {
        private DatabaseContext _context;

        [SetUp]
        public void Setup()
        {
            _context = new DatabaseContext();

            _context.Database.EnsureDeleted();
            _context.Database.EnsureCreated();
        }

        [TearDown]
        public void TearDown()
        {
            _context.Dispose();
        }

        [Test]
        public void CanAddFileManually()
        {
            // Arrange
            var folderName = new FolderName { Name = "root" };
            var folder = new Folder { Depth = 1, L0 = folderName};
            var file = new File { Name = "test.pdf", Folder = folder};

            // Act
            _context.FolderNames.Add(folderName);
            _context.Folders.Add(folder);
            _context.Files.Add(file);
            _context.SaveChanges();

            // Assert
        }

        [Test]
        public async Task CanAddFileViaHelper()
        {
            // Arrange
            var filePath1 = new FilePath(@"c:\root\test.pdf");
            var filePath2 = new FilePath(@"c:\root\test2.pdf");
            var fileSystem = new FileSystem(_context);

            // Act
            var file1 = await fileSystem.AddFile(new FileInfoHeader(filePath1, 0, DateTime.UtcNow));
            var file2 = await fileSystem.AddFile(new FileInfoHeader(filePath2, 0, DateTime.UtcNow));

            // Assert
            _context.ChangeTracker.Clear();
            var files = _context.Files.ToList();
            files.Should().HaveCount(2);
        }

        [Test]
        public async Task CanAdd2000FilesViaHelper()
        {
            // Arrange
            var testFolder = new FolderPath(Path.Combine(Environment.CurrentDirectory, "test"));
            var fileCount = 1000;
            var folderDepth = 2;
            var testPaths = CreateTestPaths(testFolder, folderDepth, fileCount);
            
            // Act
            var fileSystem = new FileSystem(_context);
            await fileSystem.AddFiles(testPaths);

            // Assert
            _context.ChangeTracker.Clear();
            var files = await _context.Files.IncludeFolderNames().ToListAsync();
            files.Should().HaveCount(testPaths.Count);
        }

        [Test]
        public async Task CanLoadFilesFromFolder()
        {
            // Arrange
            var testFolder = new FolderPath(Path.Combine(Environment.CurrentDirectory, "test"));
            var fileCount = 10;
            var folderDepth = 2;
            var testPaths = CreateTestPaths(testFolder, folderDepth, fileCount);
            
            // Act
            var fileSystem = new FileSystem(_context);
            await fileSystem.AddFiles(testPaths);
            
            // Assert
            _context.ChangeTracker.Clear();
            var checkFolder = new FolderPath(Path.GetDirectoryName(testPaths.First().Path.Value));
            var loadedFiles = await fileSystem.GetFilesInFolder(checkFolder);
            loadedFiles.Should().HaveCount(fileCount);
            var firstTestPaths = testPaths.Take(fileCount).ToList();
            loadedFiles.Should().BeEquivalentTo(firstTestPaths);
        }

        [Test]
        public async Task ThrowsAtTooManySubfolders()
        {
            // Arrange
            var testFolder = new FolderPath(string.Empty);
            var fileCount = 1;
            var folderDepth = Folder.Properties.Count + 1;
            var testPath = CreateTestPaths(testFolder, folderDepth, fileCount).Last();
            
            // Act
            var fileSystem = new FileSystem(_context);
            Func<Task> call = async () => await fileSystem.AddFile(testPath);

            // Assert
            await call.Should().ThrowAsync<InvalidOperationException>();
        }

        private static List<FileInfoHeader> CreateTestPaths(FolderPath testFolder, int folderDepth, int fileCount)
        {
            var testPaths = new List<FileInfoHeader>();
            for (var depth = 0; depth < folderDepth; depth++)
            {
                var subNames = Enumerable.Range(0, depth + 1).Select(x => $"F{x}");
                var subFolders = string.Join(Path.DirectorySeparatorChar, subNames);
                var path = Path.Combine(testFolder.Value, subFolders);
                for (var count = 0; count < fileCount; count++)
                {
                    var fileName = $"file_{count}.txt";
                    var filePath = new FilePath(Path.Combine(path, fileName));
                    var header = new FileInfoHeader(filePath, 0, DateTime.UtcNow);
                    testPaths.Add(header);
                }
            }

            return testPaths;
        }
    }
}