using System.Reflection;
using System.Text.RegularExpressions;

namespace RelationalFileSystem.Entities;

/// <summary>
/// find max directory depth on Linux (NAS):
/// find / -type d | awk -F"/" 'NF > max {max = NF} END {print max}'
/// -> 22
/// </summary>
public class Folder : Entity
{
    public const string PropertyPrefix = "L";
    private static readonly Regex PropertyRegex = new($@"{PropertyPrefix}(?<index>\d+)");

    public static readonly IReadOnlyDictionary<int, PropertyInfo> Properties =
        new SortedDictionary<int, PropertyInfo>(typeof(Folder)
            .GetProperties()
            .Select(x => new
            {
                PropertyInfo = x,
                Match = PropertyRegex.Match(x.Name)
            })
            .Where(x => x.Match.Success)
            .ToDictionary(x => int.Parse(x.Match.Groups["index"].Value), x => x.PropertyInfo));

    public int Depth { get; set; }

    public FolderName? L0 { get; set; }
    public FolderName? L1 { get; set; }
    public FolderName? L2 { get; set; }
    public FolderName? L3 { get; set; }
    public FolderName? L4 { get; set; }
    public FolderName? L5 { get; set; }
    public FolderName? L6 { get; set; }
    public FolderName? L7 { get; set; }
    public FolderName? L8 { get; set; }
    public FolderName? L9 { get; set; }
    public FolderName? L10 { get; set; }
    public FolderName? L11 { get; set; }
    public FolderName? L12 { get; set; }
    public FolderName? L13 { get; set; }
    public FolderName? L14 { get; set; }
    public FolderName? L15 { get; set; }
    public FolderName? L16 { get; set; }
    public FolderName? L17 { get; set; }
    public FolderName? L18 { get; set; }
    public FolderName? L19 { get; set; }
    public FolderName? L20 { get; set; }
    public FolderName? L21 { get; set; }
    public FolderName? L22 { get; set; }
    public FolderName? L23 { get; set; }
    public FolderName? L24 { get; set; }
    public FolderName? L25 { get; set; }
    public FolderName? L26 { get; set; }
    public FolderName? L27 { get; set; }
    public FolderName? L28 { get; set; }
    public FolderName? L29 { get; set; }

    public void SetFolderParts(IEnumerable<FolderName> folderNames)
    {
        var index = 0;
        foreach (var folderName in folderNames)
        {
            var property = Properties[index];
            property.SetValue(this, folderName);
            index++;
        }
    }

    public FolderPath GetPath()
    {
        var names = Properties.Values
            .OrderBy(x => x.Name)
            .Select(x => (FolderName?)x.GetValue(this))
            .Where(x => x != null)
            .Select(x => x!.Name)
            .ToArray();

        return new FolderPath(Path.Combine(names));
    }

    public override string ToString() => $"({Depth}) {GetPath()}";
}