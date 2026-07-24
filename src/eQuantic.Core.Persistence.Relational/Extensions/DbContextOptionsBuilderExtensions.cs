using eQuantic.Core.Persistence.Options;
using Microsoft.EntityFrameworkCore;

namespace eQuantic.Core.Persistence.Relational.Extensions;

public static class DbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder UseDataModelNamingConvention(this DbContextOptionsBuilder builder, NamingCase namingCase)
    {
        switch (namingCase)
        {
            case NamingCase.SnakeCase:
                builder.UseSnakeCaseNamingConvention();
                break;
            case NamingCase.CamelCase:
                builder.UseCamelCaseNamingConvention();
                break;
            case NamingCase.PascalCase:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(namingCase), namingCase, null);
        }

        return builder;
    }
}