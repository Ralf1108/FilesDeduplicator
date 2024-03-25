using BitFaster.Caching.Lru;
using Microsoft.EntityFrameworkCore;
using RelationalFileSystem.Entities;
using RelationalFileSystem.Extensions;
using File = RelationalFileSystem.Entities.File;

namespace RelationalFileSystem;

public class FileSystem
{
    private static readonly SemaphoreSlim CreateFolderNamesLock = new(1);

    private readonly DatabaseContext _context;

    /// <summary>
    ///  taken from https://github.com/bitfaster/BitFaster.Caching
    /// </summary>
    private readonly ConcurrentLru<FolderPath, Folder> _folderCache = new(1000);
    private readonly ConcurrentLru<FilePath, File> _fileCache = new(50000);
    
    public FileSystem(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<FileId> AddFile(FileInfoHeader fileInfoHeader)
    {
        var fileId = await AddFileInternal(fileInfoHeader);
        await _context.SaveChangesAsync();
        return fileId;
    }

    public async Task<List<FileId>> AddFiles(List<FileInfoHeader> fileInfoHeaders)
    {
        var fileIds = new List<FileId>();
        foreach (var fileInfoHeader in fileInfoHeaders)
        {
            var file = await AddFileInternal(fileInfoHeader, true);
            fileIds.Add(file);
        }

        await _context.SaveChangesAsync();
        return fileIds;
    }

    public async Task RemoveFiles(List<FilePath> filePaths)
    {
        var toDelete = new List<File>();
        foreach (var filePath in filePaths)
        {
            var file = await GetFile(filePath, true);
            if (file == null)
                continue;

            toDelete.Add(file);
        }

        foreach (var file in toDelete)
        {
            await _context.FileAttributes
                .Where(x => x.File.Id == file.Id)
                .ExecuteDeleteAsync();
        }

        _context.AttachRange(toDelete);
        _context.RemoveRange(toDelete);
        await _context.SaveChangesAsync();
    }

    public async Task SetAttribute(FileId fileId, FileAttributeKey key, string value)
    {
        var attribute = await _context.FileAttributes
            .Where(x => x.File.Id == fileId)
            .Where(x => x.Key == key)
            .AsTracking()
            .SingleOrDefaultAsync();
        if (attribute == null)
        {
            attribute = new FileAttribute { FileId = fileId, Key = key, Value = value };
            await _context.FileAttributes.AddAsync(attribute);
        }
        else
            attribute.Value = value;

        await _context.SaveChangesAsync();
    }

    private async Task<FileId> AddFileInternal(FileInfoHeader fileInfoHeader, bool useCache = false)
    {
        var folderPath = GetFolderPath(fileInfoHeader.Path);
        var folder =  await GetFolder(folderPath, useCache) ?? await CreateFolder(folderPath, useCache);

        var fileName = GetFileName(fileInfoHeader.Path);
        _context.Attach(folder); // ensure tracked
        var file = new File
        {
            Name = fileName,
            Folder = folder,
            Size = fileInfoHeader.Size,
            UpdatedAtUtc = fileInfoHeader.UpdatedAtUtc
        };
        _context.Files.Add(file);
        return file.Id;
    }

    public async Task<List<FileInfoHeader>> GetFilesInFolder(FolderPath path)
    {
        var folder = await GetFolder(path, false);
        if (folder == null)
            return new List<FileInfoHeader>();

        var result = await _context.Files
            .Where(x => x.Folder == folder)
            .IncludeFolderNames()
            .ToListAsync();

        foreach (var file in result) 
            _fileCache.AddOrUpdate(file.GetPath(), file);

        return result
            .Select(x => new FileInfoHeader(x.GetPath(), x.Size, x.UpdatedAtUtc))
            .ToList();
    }

    private async Task<File?> GetFile(FilePath path, bool useCache)
    {
        if (useCache && _fileCache.TryGet(path, out var f))
            return f;
        
        var folderPath = GetFolderPath(path);
        var folder = await GetFolder(folderPath, useCache);
        if (folder == null) 
            return null;

        var fileName = GetFileName(path);
        var file = await _context.Files
            .Where(x => x.Folder == folder)
            .Where(x => x.Name == fileName)
            .SingleOrDefaultAsync();
        if (useCache && file != null)
            _fileCache.AddOrUpdate(path, file);

        return file;
    }

    private async Task<Folder?> GetFolder(FolderPath path, bool useCache)
    {
        if (useCache && _folderCache.TryGet(path, out var f))
            return f;

        var folderPathInfo = GetFolderParts(path);
        var folderParts = folderPathInfo.Parts;
        var folder = await _context.Folders
            .Where(x => x.Depth == folderParts.Length)
            .WhereFolderLevels(folderParts)
            .SingleOrDefaultAsync();
        if(useCache && folder != null)
            _folderCache.AddOrUpdate(folderPathInfo.Path, folder);

        return folder;
    }

    private async Task<Folder> CreateFolder(FolderPath path, bool useCache)
    {
        var folderPathInfo = GetFolderParts(path);
        var folderParts = folderPathInfo.Parts;

        var folderNames = await EnsureFolderNamesExist(folderParts);
        var orderedFolderNames = folderParts
            .Select(x => folderNames[x])
            .ToList();
        var folder = new Folder { Depth = folderParts.Length };
        folder.SetFolderParts(orderedFolderNames);

        _context.Folders.Add(folder);
        await _context.SaveChangesAsync();
        if (useCache)
            _folderCache.AddOrUpdate(folderPathInfo.Path, folder);
        return folder;
    }

    private async Task<Dictionary<string, FolderName>> EnsureFolderNamesExist(string[] folderParts)
    {
        await CreateFolderNamesLock.WaitAsync();
        try
        {
            var folderNames = (await GetFolderNames(folderParts)).ToDictionary(x => x.Name);
            var missingFolderNames = folderParts.Except(folderNames.Keys).ToList();
            if (missingFolderNames.Any())
            {
                foreach (var missingName in missingFolderNames)
                {
                    var folderName = new FolderName { Name = missingName };
                    _context.FolderNames.Add(folderName);

                    folderNames.Add(folderName.Name, folderName);
                }

                // ensure folder names are written and can be queried -> avoid duplicates
                await _context.SaveChangesAsync();
            }

            return folderNames;
        }
        finally
        {
            CreateFolderNamesLock.Release(1);
        }
    }

    private async Task<List<FolderName>> GetFolderNames(string[] folderParts)
    {
        return await _context.FolderNames
            .Where(x => folderParts.Contains(x.Name))
            .AsTracking()
            .ToListAsync();

    }

    private static string GetFileName(FilePath path)
    {
        return Path.GetFileName(path.Value);
    }

    private static FolderPath GetFolderPath(FilePath path)
    {
        var directoryName = Path.GetDirectoryName(path.Value);
        if (directoryName == null)
            throw new InvalidOperationException();

        return new FolderPath(directoryName);
    }

    private static FolderPathInfo GetFolderParts(FolderPath path)
    {
        var directoryName = path.Value;
        var parts = directoryName.Split(Path.DirectorySeparatorChar);
        if (parts.Length > Folder.Properties.Count)
        {
            var message = $"Max supported subfolders count is {Folder.Properties.Count} but got {parts.Length} - Path was: '{path}'";
            throw new InvalidOperationException(message);
        }

        return new FolderPathInfo(new FolderPath(directoryName), parts);
    }

    private record FolderPathInfo(FolderPath Path, string[] Parts);
}