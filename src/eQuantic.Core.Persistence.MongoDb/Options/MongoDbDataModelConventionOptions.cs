using eQuantic.Core.Persistence.Options;

namespace eQuantic.Core.Persistence.MongoDb.Options;

public class MongoDbDataModelConventionOptions : DataModelConventionOptions
{
    internal NamingCase? NamingCase { get; private set; }
    internal string? Suffix { get; private set; }

    public MongoDbDataModelConventionOptions()
    {
    }

    public MongoDbDataModelConventionOptions(DataModelConventionOptions options)
    {
        if(options.EntityAuditingEnabled.HasValue)
            EnableEntityAuditing(options.EntityAuditingEnabled.Value);
    }
    
    public MongoDbDataModelConventionOptions UseSnakeCase()
    {
        NamingCase = eQuantic.Core.Persistence.Options.NamingCase.SnakeCase;
        return this;
    }
    
    public MongoDbDataModelConventionOptions UsePascalCase()
    {
        NamingCase = eQuantic.Core.Persistence.Options.NamingCase.PascalCase;
        return this;
    }
    
    public MongoDbDataModelConventionOptions UseCamelCase()
    {
        NamingCase = eQuantic.Core.Persistence.Options.NamingCase.CamelCase;
        return this;
    }
    
    public MongoDbDataModelConventionOptions RemoveEntitySuffixFromCollectionName(string suffix = "Data")
    {
        Suffix = suffix;
        return this;
    }
}