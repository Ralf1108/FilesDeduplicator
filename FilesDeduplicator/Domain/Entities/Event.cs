using RelationalFileSystem.Entities;

namespace FilesDeduplicator.Domain.Entities;

abstract record Event;

abstract record FolderEvent(FolderPath Folder) : Event;

record InputFolderAddedEvent(FolderPath Folder) : FolderEvent(Folder);
record IgnoreFolderAddedEvent(FolderPath Folder) : FolderEvent(Folder);
record OutputFolderAddedEvent(FolderPath Folder) : FolderEvent(Folder);

record InputFolderRemovedEvent(FolderPath Folder) : FolderEvent(Folder);
record IgnoreFolderRemovedEvent(FolderPath Folder) : FolderEvent(Folder);
record OutputFolderRemovedEvent(FolderPath Folder) : FolderEvent(Folder);


abstract record FileEvent(FileInfoHeader File) : Event;
record FileAddedEvent(FileInfoHeader File) : FileEvent(File);
record FileUpdatedEvent(FileInfoHeader File) : FileEvent(File);
record FileRemovedEvent(FileInfoHeader File) : FileEvent(File);