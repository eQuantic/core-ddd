using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using eQuantic.Core.Data.Modeling;
using eQuantic.Core.Data.Repository;

namespace eQuantic.Core.Persistence.Repositories;

/// <summary>
/// The value of every stored member of a DataModel at one moment, copied so that nothing done to the DataModel or to
/// the aggregate afterwards changes it. Two snapshots of one aggregate tell which members a save has to write.
/// </summary>
/// <typeparam name="TData">The DataModel.</typeparam>
internal sealed class DataModelSnapshot<TData>
    where TData : class
{
    /// <summary>
    /// The members compared: public instance properties with a getter and a setter, except those marked
    /// <see cref="UnmappedAttribute" /> and navigations (an <see cref="IEntity" />, or a sequence of them), which no
    /// provider stores as a member of this row.
    /// </summary>
    private static readonly PropertyInfo[] Members = typeof(TData)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(property => property is { CanRead: true, CanWrite: true }
                           && property.GetIndexParameters().Length == 0
                           && property.GetCustomAttribute<UnmappedAttribute>(inherit: true) is null
                           && !IsNavigation(property.PropertyType))
        .ToArray();

    private readonly object?[] _values;

    private DataModelSnapshot(object?[] values) => _values = values;

    /// <summary>Copies the stored members of <paramref name="data" />.</summary>
    public static DataModelSnapshot<TData> Take(TData data)
    {
        var values = new object?[Members.Length];
        for (var index = 0; index < Members.Length; index++)
        {
            values[index] = Capture(Members[index].GetValue(data));
        }

        return new DataModelSnapshot<TData>(values);
    }

    /// <summary>The members whose value differs from <paramref name="baseline" />, in declaration order.</summary>
    public IReadOnlyList<PropertyInfo> ChangedSince(DataModelSnapshot<TData> baseline)
    {
        var changed = new List<PropertyInfo>();
        for (var index = 0; index < Members.Length; index++)
        {
            if (!Same(baseline._values[index], _values[index]))
            {
                changed.Add(Members[index]);
            }
        }

        return changed;
    }

    /// <summary>
    /// The selector a member-scoped modify takes for <paramref name="members" />: <c>x =&gt; (object)x.A</c> for one,
    /// <c>x =&gt; new TData { A = x.A, B = x.B }</c> for several. It is only read, never compiled, so any constructor
    /// of <typeparamref name="TData" /> serves, with default arguments.
    /// </summary>
    public static Expression<Func<TData, object?>> Selector(IReadOnlyList<PropertyInfo> members)
    {
        var parameter = Expression.Parameter(typeof(TData), "x");
        Expression body = members.Count == 1
            ? Expression.Property(parameter, members[0])
            : Expression.MemberInit(
                Construct(),
                members.Select(member => Expression.Bind(member, Expression.Property(parameter, member))));
        return Expression.Lambda<Func<TData, object?>>(Expression.Convert(body, typeof(object)), parameter);
    }

    private static NewExpression Construct()
    {
        var constructor = typeof(TData).GetConstructor(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, Type.EmptyTypes)
                          ?? typeof(TData).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                              .OrderBy(candidate => candidate.GetParameters().Length)
                              .First();
        return Expression.New(constructor, constructor.GetParameters().Select(parameter => Expression.Default(parameter.ParameterType)));
    }

    private static bool IsNavigation(Type type)
    {
        if (typeof(IEntity).IsAssignableFrom(type))
        {
            return true;
        }

        if (type == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(type))
        {
            return false;
        }

        var element = type.IsArray
            ? type.GetElementType()
            : type.GetInterfaces().Append(type)
                .FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                ?.GetGenericArguments()[0];
        return element is not null && typeof(IEntity).IsAssignableFrom(element);
    }

    /// <summary>
    /// A copy of <paramref name="value" /> that nothing can change afterwards: scalars and strings as they are, byte
    /// arrays copied, JSON nodes deep-cloned, anything else as its JSON serialization.
    /// </summary>
    private static object? Capture(object? value)
    {
        switch (value)
        {
            case null:
                return null;
            case string or bool or char or byte or sbyte or short or ushort or int or uint or long or ulong
                or float or double or decimal or Guid or DateTime or DateTimeOffset or TimeSpan or DateOnly or TimeOnly or Enum:
                return value;
            case byte[] bytes:
                return bytes.ToArray();
            case JsonNode node:
                return node.DeepClone();
            case JsonElement element:
                return new Serialized(element.GetRawText());
            case JsonDocument document:
                return new Serialized(document.RootElement.GetRawText());
        }

        try
        {
            return new Serialized(JsonSerializer.Serialize(value, value.GetType()));
        }
        catch (Exception exception) when (exception is NotSupportedException or JsonException or InvalidOperationException
                                              or ArgumentException)
        {
            // What cannot be copied cannot be compared: it counts as changed, so the doubt costs a write, never a change.
            return Uncomparable.Instance;
        }
    }

    private static bool Same(object? before, object? after) => (before, after) switch
    {
        (null, null) => true,
        (null, _) or (_, null) => false,
        (Uncomparable, _) or (_, Uncomparable) => false,
        // DateTime.Equals ignores the kind and DateTimeOffset.Equals the offset; a store may keep either.
        (DateTime first, DateTime second) => first.Ticks == second.Ticks && first.Kind == second.Kind,
        (DateTimeOffset first, DateTimeOffset second) => first.EqualsExact(second),
        (byte[] first, byte[] second) => first.AsSpan().SequenceEqual(second),
        (JsonNode first, JsonNode second) => JsonNode.DeepEquals(first, second),
        _ => before.GetType() == after.GetType() && before.Equals(after),
    };

    private sealed record Serialized(string Json);

    private sealed class Uncomparable
    {
        public static readonly Uncomparable Instance = new();
    }
}
