using eQuantic.Core.Persistence.Extensions;
using eQuantic.Core.Persistence.MongoDb.Options;
using eQuantic.Core.Persistence.Options;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using MongoDB.EntityFrameworkCore.Extensions;

namespace eQuantic.Core.Persistence.MongoDb.Extensions;

public static class ModelBuilderExtensions
{
    public static ModelBuilder ApplyMongoDbDataModelConventions(
        this ModelBuilder modelBuilder,
        Action<MongoDbDataModelConventionOptions>? configureOptions = null)
    {
        var options = new MongoDbDataModelConventionOptions();
        
        modelBuilder.ApplyDataModelConventions(opt =>
        {
            opt.EnableEntityAuditing();
            
            options = new MongoDbDataModelConventionOptions(opt);
            configureOptions?.Invoke(options);
        });
        
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            
            var entityName = clrType.Name;
            if (!string.IsNullOrEmpty(options.Suffix) && clrType.Name.EndsWith(options.Suffix))
            {
                entityName = entityName[..^options.Suffix.Length];
                var tableName = GetCollectionName(entityName, options.NamingCase);
                entityType.SetCollectionName(tableName);
            }
            
            foreach(var property in entityType.GetProperties())
            {
                var elementName = GetElementName(property.Name, options.NamingCase);
                property.SetElementName(elementName);
            }
        }
        
        return modelBuilder;
    }
    
    private static string GetCollectionName(string input, NamingCase? namingCase)
    {
        return namingCase switch
        {
            NamingCase.PascalCase => input.Pluralize(),
            NamingCase.CamelCase => input.Pluralize().Camelize(),
            NamingCase.SnakeCase => input.Pluralize().Underscore(),
            _ => input.Pluralize()
        };
    }
    
    private static string GetElementName(string input, NamingCase? namingCase)
    {
        return namingCase switch
        {
            NamingCase.PascalCase => input,
            NamingCase.CamelCase => input.Camelize(),
            NamingCase.SnakeCase => input.Underscore(),
            _ => input
        };
    }
}