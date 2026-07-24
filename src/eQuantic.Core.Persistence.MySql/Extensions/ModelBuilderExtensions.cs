using eQuantic.Core.DataModel;
using eQuantic.Core.Domain.Entities;
using eQuantic.Core.Persistence.MySql.Options;
using eQuantic.Core.Persistence.Relational.Extensions;
using Humanizer;
using Microsoft.EntityFrameworkCore;

namespace eQuantic.Core.Persistence.MySql.Extensions;

public static class ModelBuilderExtensions
{
    public static ModelBuilder ApplyMySqlDataModelConventions(
        this ModelBuilder modelBuilder,
        Action<MySqlDataModelConventionOptions>? configureOptions = null)
    {
        var options = new MySqlDataModelConventionOptions();
        configureOptions?.Invoke(options);
        modelBuilder.ApplyRelationalDataModelConventions(opt =>
        {
            options.CopyTo(opt);
            
            // Set default options if not provided
            if(!options.NamingCase.HasValue)
                opt.UseSnakeCase();
            
            if (!options.FullyQualifiedPrimaryKeysEnabled.HasValue)
                opt.UseFullyQualifiedPrimaryKeys();
            
            if (!options.EntityAuditingEnabled.HasValue)
                opt.EnableEntityAuditing();
            
            options.CopyFrom(opt);
        });
        
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            if(!options.EntityAuditingEnabled.HasValue || !options.EntityAuditingEnabled.Value) continue;
            
            if (!typeof(IEntityTimeMark).IsAssignableFrom(clrType)) continue;
            
            var createdAtProperty = entityType.FindProperty(nameof(IEntityTimeMark.CreatedAt));
            createdAtProperty?.SetDefaultValueSql("UTC_TIMESTAMP()");
        }

        return modelBuilder;
    }
}
