using System.ComponentModel.DataAnnotations;

namespace RelationalFileSystem2.Entities;

public class FileAttributeKey
{
    public Guid Id { get; set; }

    // navigation
    public FileEntry FileEntry { get; set; } = null!;
    
    [Required]
    [MaxLength(255)]
    public string Name { get; set; }

    public string Value { get; set; }
}