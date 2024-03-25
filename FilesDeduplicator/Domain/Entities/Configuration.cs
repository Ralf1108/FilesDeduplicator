using RelationalFileSystem.Entities;

namespace FilesDeduplicator.Domain.Entities;



class Configuration
{
    public ISet<FolderPath> InputFolders { get; set; } = new HashSet<FolderPath>();
    public ISet<FolderPath> IgnoredFolders { get; set; } = new HashSet<FolderPath>();
    public ISet<FolderPath> OutputFolders { get; set; } = new HashSet<FolderPath>();

    public IEnumerable<FolderEvent> AddFolder(FolderPath folder, FolderType folderType)
    {
        switch (folderType)
        {
            case FolderType.Input:
                if (IgnoredFolders.Remove(folder))
                    yield return new IgnoreFolderRemovedEvent(folder);
                if (OutputFolders.Remove(folder))
                    yield return new OutputFolderRemovedEvent(folder);

                if (InputFolders.Add(folder))
                    yield return new InputFolderAddedEvent(folder);
                break;
            case FolderType.Ignore:
                if (InputFolders.Remove(folder))
                    yield return new InputFolderRemovedEvent(folder);
                if (OutputFolders.Remove(folder))
                    yield return new OutputFolderRemovedEvent(folder);

                if (IgnoredFolders.Add(folder))
                    yield return new IgnoreFolderAddedEvent(folder);
                break;
            case FolderType.Output:
                if (InputFolders.Remove(folder))
                    yield return new InputFolderRemovedEvent(folder);
                if (IgnoredFolders.Remove(folder))
                    yield return new IgnoreFolderRemovedEvent(folder);

                if (OutputFolders.Add(folder))
                    yield return new OutputFolderAddedEvent(folder);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(folderType), folderType, null);
        }
    }

    public IEnumerable<FolderEvent> RemoveFolder(FolderPath folder, FolderType folderType)
    {
        switch (folderType)
        {
            case FolderType.Input:
                if (InputFolders.Remove(folder))
                    yield return new InputFolderRemovedEvent(folder);
                break;
            case FolderType.Ignore:
                if (IgnoredFolders.Remove(folder))
                    yield return new IgnoreFolderRemovedEvent(folder);
                break;
            case FolderType.Output:
                if (OutputFolders.Remove(folder))
                    yield return new OutputFolderRemovedEvent(folder);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(folderType), folderType, null);
        }
    }

    private ISet<FolderPath> GetFolderList(FolderType folderType)
    {
        var list = folderType switch
        {
            FolderType.Input => InputFolders,
            FolderType.Ignore => IgnoredFolders,
            FolderType.Output => OutputFolders,
            _ => throw new ArgumentOutOfRangeException(nameof(folderType), folderType, null)
        };
        return list;
    }
}

internal enum FolderType
{
    None,
    Input,
    Ignore,
    Output
}

record FileDuplicateAttribute(FileAttributeKey Key, string Value)
{
    private sealed class NameEqualityComparer : IEqualityComparer<FileDuplicateAttribute>
    {
        public bool Equals(FileDuplicateAttribute x, FileDuplicateAttribute y)
        {
            if (ReferenceEquals(x, y)) return true;
            if (ReferenceEquals(x, null)) return false;
            if (ReferenceEquals(y, null)) return false;
            if (x.GetType() != y.GetType()) return false;
            return x.Key == y.Key;
        }

        public int GetHashCode(FileDuplicateAttribute obj)
        {
            return obj.Key.GetHashCode();
        }
    }

    public static IEqualityComparer<FileDuplicateAttribute> NameComparer { get; } = new NameEqualityComparer();
}