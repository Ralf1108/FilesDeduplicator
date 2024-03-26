using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace RelationalFileSystem2.Entities;

[Index(nameof(Name))]
public class FileEntry
{
    public Guid Id { get; set; }
    public Guid FolderEntryId { get; set; }
    public FolderEntry FolderEntry { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public long Size { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    
    public override string ToString() => Name;
}