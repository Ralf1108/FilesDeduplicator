using Microsoft.EntityFrameworkCore;

namespace RelationalFileSystem.Entities;

[Index(nameof(Value))]
public class FileAttribute
{
    public FileAttributeId Id { get; set; } = new(Guid.NewGuid());
    public FileId FileId { get; set; }

    // navigation
    public File File { get; set; } = null!;
    
    public FileAttributeKey Key { get; set; }
    public string Value { get; set; } = string.Empty;
}