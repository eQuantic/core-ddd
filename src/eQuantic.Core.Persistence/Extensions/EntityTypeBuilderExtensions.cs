using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eQuantic.Core.Persistence.Extensions;

public static class EntityTypeBuilderExtensions
{
    /// <summary>
    /// Has JSON data
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="resourceName"></param>
    /// <param name="assembly"></param>
    /// <typeparam name="T"></typeparam>
    /// <returns></returns>
    public static EntityTypeBuilder<T> HasJsonData<T>(this EntityTypeBuilder<T> builder, string resourceName,
        Assembly assembly)
        where T : class
    {
        var entities = ReadJsonResources<T>(resourceName, assembly);
        if (entities != null)
            builder.HasData(entities);
        return builder;
    }

    private static T[]? ReadJsonResources<T>(string resourceName, Assembly assembly)
    {
        var prefix = assembly.GetName().Name;
        using var stream = assembly.GetManifestResourceStream($"{prefix}.{resourceName}");
        if (stream == null) return null;
        using var reader = new StreamReader(stream);
        var jsonString = reader.ReadToEnd();
        if (string.IsNullOrEmpty(jsonString)) return null;
        return JsonSerializer.Deserialize<T[]>(jsonString, new JsonSerializerOptions
        {
            Converters = { new JsonStringEnumConverter() },
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }
}