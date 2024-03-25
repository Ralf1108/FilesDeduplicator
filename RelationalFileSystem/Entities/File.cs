using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using StronglyTypedIds;

[assembly: StronglyTypedIdDefaults(converters:StronglyTypedIdConverter.SystemTextJson
                                              | StronglyTypedIdConverter.TypeConverter
                                              | StronglyTypedIdConverter.EfCoreValueConverter)]

namespace RelationalFileSystem.Entities;

[StronglyTypedId(StronglyTypedIdBackingType.Guid)]
public partial struct FileId { }

[StronglyTypedId(StronglyTypedIdBackingType.Guid)]
public partial struct FileAttributeId { }

[StronglyTypedId(StronglyTypedIdBackingType.Guid)]
public partial struct FileAttributeKey { }

[StronglyTypedId(StronglyTypedIdBackingType.String)]
public partial struct FilePath { }

[StronglyTypedId(StronglyTypedIdBackingType.String)]
public partial struct FolderPath { }

public record FileInfoHeader(FilePath Path, long Size, DateTime UpdatedAtUtc);

[Index(nameof(Name))]
public class File
{
    public FileId Id { get; set; } = new(Guid.NewGuid());
    public Folder? Folder { get; set; }

    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public long Size { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public List<FileAttribute> Attributes { get; set; } = new();

    public FilePath GetPath()
    {
        var path = Folder == null
            ? Name
            : Path.Combine(Folder.GetPath().Value, Name);
        return new FilePath(path);
    }

    public override string ToString() => GetPath().Value;
}