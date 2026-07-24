using eQuantic.Core.Persistence.Options;

namespace eQuantic.Core.Persistence.Relational.Options;

public class RelationalDataModelConventionOptions : DataModelConventionOptions
{
    internal NamingCase? NamingCase { get; private set; }
    internal bool? FullyQualifiedPrimaryKeysEnabled { get; private set; }
    internal string? Suffix { get; private set; }

    internal void CopyTo(RelationalDataModelConventionOptions opt)
    {
        if (EntityAuditingEnabled.HasValue)
            opt.EnableEntityAuditing(EntityAuditingEnabled.Value);
            
        if (NamingCase.HasValue)
        {
            opt.UseNamingCase(NamingCase.Value);
        }
        
        if (FullyQualifiedPrimaryKeysEnabled.HasValue)
            opt.UseFullyQualifiedPrimaryKeys(FullyQualifiedPrimaryKeysEnabled.Value);
            
        if (!string.IsNullOrEmpty(Suffix))
            opt.RemoveEntitySuffixFromTableName(Suffix);
    }
    
    internal void CopyFrom(RelationalDataModelConventionOptions opt)
    {
        if (opt.EntityAuditingEnabled.HasValue)
            EnableEntityAuditing(opt.EntityAuditingEnabled.Value);
            
        if (opt.NamingCase.HasValue)
        {
            UseNamingCase(opt.NamingCase.Value);
        }
        
        if (opt.FullyQualifiedPrimaryKeysEnabled.HasValue)
            UseFullyQualifiedPrimaryKeys(opt.FullyQualifiedPrimaryKeysEnabled.Value);
            
        if (!string.IsNullOrEmpty(opt.Suffix))
            RemoveEntitySuffixFromTableName(opt.Suffix);
    }
    
    public RelationalDataModelConventionOptions UseFullyQualifiedPrimaryKeys(bool enabled = true)
    {
        FullyQualifiedPrimaryKeysEnabled = enabled;
        return this;
    }
    
    public RelationalDataModelConventionOptions UseNamingCase(eQuantic.Core.Persistence.Options.NamingCase namingCase)
    {
        NamingCase = namingCase;
        return this;
    }
    
    public RelationalDataModelConventionOptions UseSnakeCase()
    {
        NamingCase = eQuantic.Core.Persistence.Options.NamingCase.SnakeCase;
        return this;
    }
    
    public RelationalDataModelConventionOptions UsePascalCase()
    {
        NamingCase = eQuantic.Core.Persistence.Options.NamingCase.PascalCase;
        return this;
    }
    
    public RelationalDataModelConventionOptions UseCamelCase()
    {
        NamingCase = eQuantic.Core.Persistence.Options.NamingCase.CamelCase;
        return this;
    }
    
    public RelationalDataModelConventionOptions RemoveEntitySuffixFromTableName(string suffix = "Data")
    {
        Suffix = suffix;
        return this;
    }
}