using Microsoft.EntityFrameworkCore;

namespace eQuantic.Core.Persistence.MySql.Extensions;

public static class DbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UseMySqlDataModelNamingConvention(this DbContextOptionsBuilder builder)
    {
        builder.UseSnakeCaseNamingConvention();
        return builder;
    }
}