using System.Reflection;

namespace eQuantic.Core.Domain.Entities.Requests;

public class BasicRequest
{
    internal bool IsReferencedRequest()
    {
        return IsReferencedRequest(this);
    }

    internal object? GetReferenceValue()
    {
        return GetReferenceMethod()?.Invoke(this, null);
    }

    internal Type? GetReferenceType()
    {
        return GetReferenceMethod()?.ReturnType;
    }

    private MethodInfo? GetReferenceMethod()
    {
        return IsReferencedRequest() ? GetType().GetMethod("GetReferenceId") : null;
    }
    
    internal static bool IsReferencedRequest(BasicRequest request)
    {
        return request.GetType().GetInterfaces().Any(o => o.IsGenericType && o.GetGenericTypeDefinition() == typeof(IReferencedRequest<>));
    }

    internal static object? GetReferenceValue(BasicRequest request)
    {
        return request.GetReferenceMethod()?.Invoke(request, null);
    }

    internal static Type? GetReferenceType(BasicRequest request)
    {
        return request.GetReferenceMethod()?.ReturnType;
    }
}