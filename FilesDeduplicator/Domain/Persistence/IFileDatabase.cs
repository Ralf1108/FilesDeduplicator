using FilesDeduplicator.Domain.Entities;
using RelationalFileSystem.Entities;

namespace FilesDeduplicator.Domain.Persistence;

internal interface IFileDatabase
{
    bool Add(FileInfoHeader fileHeader);
    void SetAttribute(FilePath path, FileDuplicateAttribute attribute);
    List<FileEntry> GetForFolder(string folder);
    void Remove(FileInfoHeader file);
    IQueryable<FileEntry> GetQuery();
}