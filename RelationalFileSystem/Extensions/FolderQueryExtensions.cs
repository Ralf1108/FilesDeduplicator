using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using RelationalFileSystem.Entities;

namespace RelationalFileSystem.Extensions;

public static class FolderQueryExtensions
{
    public static IQueryable<Folder> WhereFolderLevels(this IQueryable<Folder> query, string[] folderParts)
    {
        var index = 0;
        foreach (var folderPart in folderParts)
        {
            var where = CreateWhereForLevel(index, folderPart);
            query = query.Where(where);
            index++;
        }

        return query;
    }

    /// <summary>
    /// Creates WHERE expression: Expression&lt;Func&lt;Folder, bool&gt;&gt; expression = x =&gt; x.L1.Name == folderName;
    /// </summary>
    private static Expression<Func<Folder, bool>> CreateWhereForLevel(int index, string folderName)
    {
        var parameter = Expression.Parameter(typeof(Folder), "x");
        var property = Expression.Property(parameter, Folder.PropertyPrefix + index);
        var propertyName = Expression.Property(property, nameof(FolderName.Name));

        var constant = Expression.Constant(folderName);
        var eq = Expression.Equal(propertyName, constant);
        // => Expression<Func<Folder, bool>> expression = x => x.L1.Name == folderName;
        return Expression.Lambda<Func<Folder, bool>>(eq, parameter);
    }

    public static IQueryable<Folder> IncludeFolderNames(this IQueryable<Folder> query)
    {
        foreach (var propertyInfo in Folder.Properties)
        {
            var include = CreateIncludeForLevel(propertyInfo.Value.Name);
            query = query.Include(include);    
        }
        
        return query;
    }

    /// <summary>
    /// Creates Include expression: x => x.L1;
    /// </summary>
    private static Expression<Func<Folder, FolderName>> CreateIncludeForLevel(string propertyName)
    {
        var parameter = Expression.Parameter(typeof(Folder), "x");
        var property = Expression.Property(parameter, propertyName);
        return Expression.Lambda<Func<Folder, FolderName>>(property, parameter);
    }
}