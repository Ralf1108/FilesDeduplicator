using FilesDeduplicator.Domain.Entities;
using RelationalFileSystem.Entities;

namespace FilesDeduplicator.Domain.Persistence;

class FileDatabase : IFileDatabase
{
    private readonly Dictionary<FilePath, FileEntry> _files = new();

    public bool Add(FileInfoHeader fileHeader)
    {
        if (_files.TryGetValue(fileHeader.Path, out var existingFileHeader))
        {
            if (existingFileHeader.Size == fileHeader.Size
                && existingFileHeader.LastChangedAtUtc == fileHeader.UpdatedAtUtc)
                return false; // no change
        }

        var entry = new FileEntry
        {
            Path = fileHeader.Path,
            Size = fileHeader.Size,
            LastChangedAtUtc = fileHeader.UpdatedAtUtc
        };
        _files[fileHeader.Path] = entry;
        return true;
    }

    public List<FileEntry> GetForFolder(string folder)
    {
        return _files.Values
            .Where(x => x.Path.Value.StartsWith(folder))
            .ToList();
    }

    public void Remove(FileInfoHeader file)
    {
        _files.Remove(file.Path);
    }

    public void SetAttribute(FilePath path, FileDuplicateAttribute attribute)
    {
        if (!_files.TryGetValue(path, out var entry))
            throw new InvalidOperationException();

        var attr = entry.Attributes.SingleOrDefault(x => x.Key == attribute.Key);
        if (attr != null)
            entry.Attributes.Remove(attr);

        entry.Attributes.Add(attribute);
    }

    public bool RemoveAttribute(FilePath path, string identifier)
    {
        //if (!_files.TryGetValue(path, out var entry))
        //    throw new InvalidOperationException();

        //var attr = entry.Attributes.SingleOrDefault(x => x.Key == identifier);
        //if (attr != null)
        //    return entry.Attributes.Remove(attr);

        return false;
    }

    public IQueryable<FileEntry> GetQuery()
    {
        return _files.Values.AsQueryable();
    }
}

class FileEntry
{
    public FileId Id { get; set; } = new(Guid.NewGuid());
    public FilePath Path { get; set; }
    public long Size { get; set; }
    public DateTime LastChangedAtUtc { get; set; }

    public DateTime LastScannedAtUtc { get; set; }

    public ISet<FileDuplicateAttribute> Attributes { get; set; } = new HashSet<FileDuplicateAttribute>();
}