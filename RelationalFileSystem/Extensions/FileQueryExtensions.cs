using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RelationalFileSystem.Entities;
using File = RelationalFileSystem.Entities.File;

namespace RelationalFileSystem.Extensions;

public static class FileQueryExtensions
{
    public static IQueryable<File> IncludeFolderNames(this IQueryable<File> query)
    {
        foreach (var propertyInfo in Folder.Properties)
        {
            var include = CreateIncludeForLevel(propertyInfo.Value.Name);
            query = query.Include(x => x.Folder)
                .ThenInclude(include);
        }

        return query;
    }

    /// <summary>
    /// Creates Include expression: x => x.L0;
    /// </summary>
    private static Expression<Func<Folder?, FolderName>> CreateIncludeForLevel(string propertyName)
    {
        var parameter = Expression.Parameter(typeof(Folder), "x");
        var property = Expression.Property(parameter, propertyName);
        return Expression.Lambda<Func<Folder?, FolderName>>(property, parameter);
    }
}