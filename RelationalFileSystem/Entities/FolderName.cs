using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace RelationalFileSystem.Entities;

[Index(nameof(Name))]
public class FolderName : Entity
{
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}";
    }
}