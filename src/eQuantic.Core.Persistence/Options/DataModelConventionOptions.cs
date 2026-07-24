namespace eQuantic.Core.Persistence.Options;

public enum NamingCase
{
    PascalCase,
    CamelCase,
    SnakeCase
}

public class DataModelConventionOptions
{
    internal bool? EntityAuditingEnabled { get; private set; }
    
    public DataModelConventionOptions EnableEntityAuditing(bool enabled = true)
    {
        EntityAuditingEnabled = enabled;
        return this;
    }
    
    internal void CopyTo(DataModelConventionOptions opt)
    {
        if (EntityAuditingEnabled.HasValue)
            opt.EnableEntityAuditing(EntityAuditingEnabled.Value);
    }
    
    internal void CopyFrom(DataModelConventionOptions opt)
    {
        if (opt.EntityAuditingEnabled.HasValue)
            EnableEntityAuditing(opt.EntityAuditingEnabled.Value);
    }
}