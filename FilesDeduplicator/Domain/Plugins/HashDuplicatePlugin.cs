using System.Security.Cryptography;
using FilesDeduplicator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using RelationalFileSystem.Entities;
using RelationalFileSystem.Extensions;

namespace FilesDeduplicator.Domain.Plugins;

class HashDuplicatePlugin : IDuplicatePlugin
{
    private readonly SHA1 _hash = SHA1.Create();

    public string Identifier => "SHA1";
    public FileAttributeKey Key => new(Guid.Parse("B9D529AC-F41B-4247-91FF-0389456101DD"));

    public async Task<List<RelationalFileSystem.Entities.File>> GetCalculationCandidates(
        IQueryable<RelationalFileSystem.Entities.File> query)
    {
        var result = await query
            .IncludeFolderNames()
            .Where(x => x.Attributes.All(a => a.Key != Key))
            .GroupBy(x => x.Size)
            //.Where(x => x.Count() > 1) // workaround
            .Select(x => new { Count = x.Count(), Items = x.ToList() })
            .Where(x => x.Count > 1)
            .Select(x => x.Items)
            .ToListAsync();

        return result
            .SelectMany(x => x)
            .ToList();
    }

    public async Task<List<List<FileId>>> FindDuplicates(IQueryable<RelationalFileSystem.Entities.FileAttribute> query)
    {
        var result = await query
            .Where(x => x.Key == Key)
            .GroupBy(x => x.Value)
            //.Where(x => x.Count() > 1) // workaround
            .Select(x => new { Count = x.Count(), Items = x.ToList() })
            .Where(x => x.Count > 1)
            .ToListAsync();


        return result
            .Select(x => x.Items.Select(i => i.FileId).ToList())
            .ToList();
        //var result = await query
        //    .Where(x=>x.Attributes.Any(a=>a.Key == Key))
        //    .Select(x => new
        //    {
        //        x.Id,
        //        Hash = x.Attributes.Single(a => a.Key == Key).Value
        //    })
        //    .GroupBy(x => x.Hash)
        //    //.Where(x => x.Count() > 1) // workaround
        //    .Select(x => new { Count = x.Count(), Items = x.ToList() })
        //    .Where(x => x.Count > 1)
        //    .ToListAsync();

        //return result
        //    .Select(x => x.Items.Select(i => i.Id).ToList())
        //    .ToList();
    }

    public FileDuplicateAttribute Calculate(FilePath filePath)
    {
        using var fs = System.IO.File.OpenRead(filePath.Value);
        var hash = _hash.ComputeHash(fs);
        var hex = Convert.ToHexString(hash);
        return new FileDuplicateAttribute(Key, hex);
    }
}