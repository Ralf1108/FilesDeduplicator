using LinqKit;
using Microsoft.EntityFrameworkCore;
using RelationalFileSystem2.Entities;

namespace RelationalFileSystem2;

public class FileSystem
{
    public static readonly string RootFolderName = "<root>";
    public static readonly Guid RootFolderId = new("00000000-0000-0000-0000-000000000001");

    private readonly DatabaseContext _context;

    public FileSystem(DatabaseContext context)
    {
        _context = context;
    }

    public async Task<FolderEntry?> GetFolder(string path)
    {
        var parts = GetFolderParts(path);
        var folders = await GetFolders(parts, false);
        return folders.FirstOrDefault();
    }

    public async Task<List<FolderEntry>> GetFolders(string path)
    {
        var parts = GetFolderParts(path);
        return await GetFolders(parts, true);
    }

    public async Task<Guid> CreateFolder(string path)
    {
        var parts = GetFolderParts(path);
        var existingFolders = await GetFolders(parts, true);

        var lastOrDefault = existingFolders.LastOrDefault();
        var parentId = lastOrDefault?.ParentId ?? RootFolderId;
        var deepestExistingFolderDepth = lastOrDefault == null ? 1 : existingFolders.Count;
        foreach (var part in parts.Skip(deepestExistingFolderDepth))
        {
            parentId = await AddFolder(part, parentId);
        }

        return parentId;
    }

    /// <summary>
    /// // TODO: check if possible with recursive CTE by supplying part names + depths
    /// </summary>
    private async Task<List<FolderEntry>> GetFolders(string[] pathParts, bool returnAllFolders)
    {
        // taken from: https://www.albahari.com/nutshell/predicatebuilder.aspx
        // TODO: doesn't work correctly because there could be more than one folder on same depth with same name!!!!!!!!!!
        var predicate = PredicateBuilder.New<FolderEntry>();
        var depth = -1;
        foreach (var part in pathParts)
        {
            depth++;

            var localDepth = depth;
            predicate = predicate.Or(p => p.Name == part && p.Depth == localDepth);
        }

        var folders = await _context.Folders
            .Where(predicate)
            //.OrderByDescending(x => x.Depth)
            .ToListAsync();

        // only root found
        if (folders.Count == 1)
            return new List<FolderEntry>();

        // try to find the one path from deepest to root because on each level there could be multiple folder parts with same name
        var map = folders.ToLookup(x => x.Depth);
        var resultFolderParts = new List<FolderEntry>(pathParts.Length);

        var maxDepth = pathParts.Length - 1;
        var startFolderEntries = map[maxDepth];
        foreach (var startPart in startFolderEntries)
        {
            resultFolderParts.Add(startPart);
            if (!FindNextPart(map, startPart, maxDepth - 1, resultFolderParts))
                resultFolderParts.RemoveAt(0);
        }

        return resultFolderParts;
    }

    private static bool FindNextPart(ILookup<int, FolderEntry> folderParts,
        FolderEntry currentEntry,
        int depth,
        IList<FolderEntry> resultFolderParts)
    {
        if (depth == -1)
            return true; // root reached

        foreach (var entry in folderParts[depth])
        {
            if (entry.Id != currentEntry.ParentId)
                continue;

            resultFolderParts.Add(entry);
            if (!FindNextPart(folderParts, entry, depth - 1, resultFolderParts))
                resultFolderParts.RemoveAt(resultFolderParts.Count - 1);

            return true;
        }

        return false;
    }

    private static string[] GetFolderParts(string path)
    {
        if (path[0] != Path.DirectorySeparatorChar)
            throw new InvalidOperationException($"Only absolute path names are supported and have to start with '{Path.DirectorySeparatorChar}'");

        var parts = path.Split(Path.DirectorySeparatorChar);
        parts[0] = RootFolderName;
        return parts;
    }

    public async Task<Guid> AddFile(string name, Guid parentFolderId)
    {
        var parentFolder = _context.Folders.AsTracking().Single(x => x.Id == parentFolderId);

        var newFile = new FileEntry
        {
            Id = Guid.NewGuid(),
            Name = name,
            FolderEntry = parentFolder
        };

        _context.Files.Add(newFile);
        await _context.SaveChangesAsync();
        return newFile.Id;
    }

    public async Task<Guid> AddFolder(string name, Guid parentFolderId)
    {
        var parentFolder = _context.Folders.AsTracking().Single(x => x.Id == parentFolderId);

        var newFolder = new FolderEntry
        {
            Id = Guid.NewGuid(),
            Name = name,
            Depth = parentFolder.Depth + 1,
            Parent = parentFolder
        };

        _context.Folders.Add(newFolder);
        await _context.SaveChangesAsync();
        return newFolder.Id;
    }
}