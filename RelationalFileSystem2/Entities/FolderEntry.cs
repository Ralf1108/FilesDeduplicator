using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace RelationalFileSystem2.Entities;

/// <summary>
/// find max directory depth on Linux (NAS):
/// find / -type d | awk -F"/" 'NF > max {max = NF} END {print max}'
/// -> 22
/// </summary>
[Index(nameof(Name), nameof(Depth), nameof(ParentId), IsUnique = true)]
public class FolderEntry
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }

    public FolderEntry? Parent { get; set; }
    public int Depth { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; }

    public List<FileEntry> Files { get; set; } = null!;

    public override string ToString() => $"({Depth}) {Name}";
}