using FilesDeduplicator.Domain;
using FilesDeduplicator.Domain.Entities;
using FilesDeduplicator.Domain.Plugins;
using RelationalFileSystem.Entities;

namespace FilesDeduplicator;

internal class Program
{
    static async Task Main(string[] args)
    {
        //var path = new FolderPath(@"D:\Projects\FilesDeduplicator\TestFiles\Images");
        var path = new FolderPath(@"Z:\Android\media\com.whatsapp\WhatsApp\Media\WhatsApp Images");

        var engine = new Engine();
        engine.AddPlugin(new HashDuplicatePlugin());
        await engine.Start();

        engine.AddFolder(path, FolderType.Input);

        Console.ReadKey();
        await engine.Stop();
    }
}