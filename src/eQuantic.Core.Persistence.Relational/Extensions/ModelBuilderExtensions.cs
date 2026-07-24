using eQuantic.Core.DataModel;
using eQuantic.Core.Persistence.Extensions;
using eQuantic.Core.Persistence.Options;
using eQuantic.Core.Persistence.Relational.Options;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace eQuantic.Core.Persistence.Relational.Extensions;

public static class ModelBuilderExtensions
{
    public static ModelBuilder ApplyRelationalDataModelConventions(
        this ModelBuilder modelBuilder,
        Action<RelationalDataModelConventionOptions>? configureOptions = null)
    {
        var options = new RelationalDataModelConventionOptions();
        configureOptions?.Invoke(options);
        modelBuilder.ApplyDataModelConventions(opt =>
        {
            options.CopyTo(opt);
            
            // Set default options if not provided
            if(!options.EntityAuditingEnabled.HasValue)
                opt.EnableEntityAuditing();
            
            options.CopyFrom(opt);
        });

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            var entityName = clrType.Name;
            if (!string.IsNullOrEmpty(options.Suffix) && clrType.Name.EndsWith(options.Suffix))
            {
                entityName = entityName[..^options.Suffix.Length];
            }

            var tableName = GetTableName(entityName, options.NamingCase);
            entityType.SetTableName(tableName);

            if (options.FullyQualifiedPrimaryKeysEnabled.HasValue && options.FullyQualifiedPrimaryKeysEnabled.Value)
            {
                var idProperty = entityType.FindProperty(nameof(EntityDataBase.Id));
                if (idProperty != null && idProperty.IsPrimaryKey())
                {
                    var pkName = GetPrimaryKeyName(entityName, options.NamingCase);
                    idProperty.SetColumnName(pkName);
                }
            }

            ConfigureUserRelationships(entityType);
        }

        return modelBuilder;
    }

    private static string GetTableName(string input, NamingCase? namingCase)
    {
        return namingCase switch
        {
            NamingCase.PascalCase => input.Pluralize(),
            NamingCase.CamelCase => input.Pluralize().Camelize(),
            NamingCase.SnakeCase => input.Pluralize().Underscore(),
            _ => input.Pluralize()
        };
    }

    private static string GetPrimaryKeyName(string entityName, NamingCase? namingCase)
    {
        return namingCase switch
        {
            NamingCase.PascalCase => entityName + nameof(EntityDataBase.Id),
            NamingCase.CamelCase => entityName.Camelize() + nameof(EntityDataBase.Id),
            NamingCase.SnakeCase => entityName.Underscore() + "_id",
            _ => entityName + nameof(EntityDataBase.Id)
        };
    }

    private static void ConfigureUserRelationships(IMutableEntityType entityType)
    {
        var clrType = entityType.ClrType;
        var interfaces = clrType.GetInterfaces();

        // IEntityOwned<TUser, TUserKey> - CreatedBy relationship
        var ownedInterface = interfaces.FirstOrDefault(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IEntityOwned<,>));

        if (ownedInterface != null)
        {
            var userType = ownedInterface.GetGenericArguments()[0];
            ConfigureUserRelationship(entityType, "CreatedBy", nameof(IEntityOwned.CreatedById), userType,
                isRequired: true);
        }

        // IEntityTrack<TUser, TUserKey> - UpdatedBy relationship
        var trackInterface = interfaces.FirstOrDefault(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IEntityTrack<,>));

        if (trackInterface != null)
        {
            var userType = trackInterface.GetGenericArguments()[0];
            ConfigureUserRelationship(entityType, "UpdatedBy", nameof(IEntityTrack.UpdatedById), userType,
                isRequired: false);
        }

        // IEntityHistory<TUser, TUserKey> - DeletedBy relationship
        var historyInterface = interfaces.FirstOrDefault(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IEntityHistory<,>));

        if (historyInterface != null)
        {
            var userType = historyInterface.GetGenericArguments()[0];
            ConfigureUserRelationship(entityType, "DeletedBy", nameof(IEntityHistory.DeletedById), userType,
                isRequired: false);
        }
    }

    private static void ConfigureUserRelationship(
        IMutableEntityType entityType,
        string navigationProperty,
        string foreignKeyProperty,
        Type userType,
        bool isRequired)
    {
        try
        {
            var navigationProp = entityType.ClrType.GetProperty(navigationProperty);
            var foreignKeyProp = entityType.FindProperty(foreignKeyProperty);
            var principalEntityType = entityType.Model.FindEntityType(userType);

            if (navigationProp == null || foreignKeyProp == null || principalEntityType == null) return;

            var foreignKeyProperties = new[] { foreignKeyProp };
            var principalKey = principalEntityType.FindPrimaryKey();

            if (principalKey == null) return;

            var foreignKey = entityType.AddForeignKey(
                foreignKeyProperties,
                principalKey,
                principalEntityType);

            foreignKey.IsRequired = isRequired;
            foreignKey.DeleteBehavior = DeleteBehavior.NoAction;

            foreignKey.SetDependentToPrincipal(navigationProp);
        }
        catch (Exception)
        {
            // Nothing to do
        }
    }
}