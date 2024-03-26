using System.Collections.Concurrent;
using FilesDeduplicator.Domain.Entities;
using FilesDeduplicator.Domain.Persistence;
using FilesDeduplicator.Domain.Plugins;
using FilesDeduplicator.Extensions;
using Microsoft.EntityFrameworkCore;
using RelationalFileSystem;
using RelationalFileSystem.Entities;

namespace FilesDeduplicator.Domain;

class Engine
{
    public Configuration Configuration { get; } = new();
    public IFileDatabase Database { get; } = new FileDatabase();

    private readonly List<IDuplicatePlugin> _plugins = new();

    private readonly CancellationTokenSource _cts = new();

    private readonly BlockingCollection<FolderPath> _foldersToScan = new();
    private readonly List<string> _scannedFolders = new();
    private Task _scanWorker;

    private readonly BlockingCollection<FileEvent> _fileEvents = new();
    private Task _fileWorker;

    public void AddPlugin(IDuplicatePlugin plugin)
    {
        _plugins.Add(plugin);
    }

    public void AddFolder(FolderPath folder, FolderType folderType)
    {
        var commands = Configuration.AddFolder(folder, folderType);

        foreach (var command in commands)
            Handle(command);
    }

    public async Task Start()
    {
        await using var context = new DatabaseContext();
        //await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        await context.Database.MigrateAsync();

        await RunPlugins();

        _scanWorker = Task.Run(Scan);
        _fileWorker = Task.Run(CheckFile);
    }

    public async Task Stop()
    {
        _cts.Cancel();
        await _scanWorker.WaitAsync(TimeSpan.FromSeconds(3));
        await _fileWorker.WaitAsync(TimeSpan.FromSeconds(3));
    }

    private async Task Scan()
    {
        foreach (var folder in _foldersToScan.GetConsumingEnumerable(_cts.Token))
        {
            await using var context = new DatabaseContext();
            var fileSystem = new FileSystem(context);

            await ScanFolder(fileSystem, folder);
        }
    }

    private async Task ScanFolder(FileSystem fileSystem, FolderPath folder)
    {
        var existingFiles = (await fileSystem.GetFilesInFolder(folder)).ToHashSet();
        var currentFiles = Directory.EnumerateFiles(folder.Value)
            .Take(2000)
            .OrderBy(x => x)
            .Select(x => new FileInfo(x))
            .Select(x => new FileInfoHeader(new FilePath(x.FullName), x.Length, x.LastWriteTimeUtc))
            .ToHashSet();
        
        var deletedFiles = existingFiles.Except(currentFiles).ToList();
        if (deletedFiles.Any())
        {
            var deletedFilePaths = deletedFiles.Select(x => x.Path).ToList();
            await fileSystem.RemoveFiles(deletedFilePaths);

            foreach (var file in deletedFiles) 
                _fileEvents.Add(new FileRemovedEvent(file));
        }

        var addedFiles = currentFiles.Except(existingFiles).ToList();
        if (addedFiles.Any())
        {
            await fileSystem.AddFiles(addedFiles);
            foreach (var file in addedFiles) 
                _fileEvents.Add(new FileAddedEvent(file));
        }

        var subfolders = Directory.EnumerateDirectories(folder.Value)
            .OrderBy(x => x)
            .ToList();
        foreach (var subfolder in subfolders)
        {
            await ScanFolder(fileSystem, new FolderPath(subfolder));
        }
    }
    
    private async Task CheckFile()
    {
        while (!_cts.Token.IsCancellationRequested)
        {
            var batch = _fileEvents.GetConsumingEnumerableBatch(500, TimeSpan.FromSeconds(2), _cts.Token);
            if (!batch.Any())
                continue;

            await RunPlugins();

            foreach (var fileEvent in batch)
            {
                //switch (fileEvent)
                //{
                //    case FileAddedEvent ev:
                //        Database.Add(ev.File);
                //        break;
                //    case FileRemovedEvent ev:
                //        Database.Remove(ev.File);
                //        continue;
                //    case FileUpdatedEvent ev:
                //        foreach (var plugin in _plugins) 
                //            Database.RemoveAttribute(ev.File.Path, plugin.Identifier);
                //        break;
                //    default:
                //        throw new ArgumentOutOfRangeException(nameof(fileEvent));
                //}

                //RunPlugins();
            }
        }
    }

    private async Task RunPlugins()
    {
        foreach (var plugin in _plugins)
        {
            await using var context = new DatabaseContext();
            var query = context.GetFileQuery();
            var candidates = await plugin.GetCalculationCandidates(query);

            var fileSystem = new FileSystem(context);
            foreach (var candidate in candidates)
            {
                var filePath = candidate.GetPath();
                var attribute = plugin.Calculate(filePath);
                await fileSystem.SetAttribute(candidate.Id, attribute.Key, attribute.Value);
            }

            var query2 = context.GetFileAttributeQuery();
            var duplicates = await plugin.FindDuplicates(query2);
        }
    }
    
    private void Handle(Event @event)
    {
        switch (@event)
        {
            case IgnoreFolderAddedEvent cmd:
                break;
            case IgnoreFolderRemovedEvent cmd:
                break;
            case InputFolderAddedEvent cmd:
                _foldersToScan.Add(cmd.Folder);
                break;
            case InputFolderRemovedEvent cmd:
                break;
            case OutputFolderAddedEvent cmd:
                break;
            case OutputFolderRemovedEvent cmd:
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(@event));
        }
    }
}