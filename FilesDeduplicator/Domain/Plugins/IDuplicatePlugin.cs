using FilesDeduplicator.Domain.Entities;
using RelationalFileSystem.Entities;
using File = RelationalFileSystem.Entities.File;

namespace FilesDeduplicator.Domain.Plugins;

internal interface IDuplicatePlugin
{
    string Identifier { get; }
    FileAttributeKey Key { get; }

    FileDuplicateAttribute Calculate(FilePath filePath);

    Task<List<File>> GetCalculationCandidates(IQueryable<File> query);
    Task<List<List<FileId>>> FindDuplicates(IQueryable<FileAttribute> query);
}