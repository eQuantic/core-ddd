using Microsoft.EntityFrameworkCore;

namespace eQuantic.Core.Persistence.PostgreSql.Extensions;

public static class DbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UsePostgreSqlDataModelNamingConvention(this DbContextOptionsBuilder builder)
    {
        builder.UseSnakeCaseNamingConvention();
        return builder;
    }
}