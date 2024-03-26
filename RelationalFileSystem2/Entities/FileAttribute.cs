using Microsoft.EntityFrameworkCore;

namespace RelationalFileSystem2.Entities;

[Index(nameof(Value))]
public class FileAttribute
{
    public Guid Id { get; set; }

    // navigation
    public FileEntry FileEntry { get; set; } = null!;
    
    public string Key { get; set; }
    public string Value { get; set; } = string.Empty;
}