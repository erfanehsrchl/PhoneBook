using System.Reflection;
using PhoneBook.Domain.Shared;

namespace PhoneBook.Application.Behaviors;

internal static class ValidationResultFactory
{
    public static TResponse Create<TResponse>(Error error)
    {
        if (typeof(TResponse) == typeof(Result))
        {
            return (TResponse)(object)Result.Failure(error);
        }

        Type responseType = typeof(TResponse);

        if (!responseType.IsGenericType
            || responseType.GetGenericTypeDefinition() != typeof(Result<>))
        {
            throw new InvalidOperationException(
                $"Validation behavior supports only Result responses, not '{responseType.Name}'.");
        }

        MethodInfo failureMethod = responseType.GetMethod(
            nameof(Result.Failure),
            BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
            binder: null,
            types: [typeof(Error)],
            modifiers: null)
            ?? throw new InvalidOperationException(
                $"Response type '{responseType.Name}' does not expose a compatible failure factory.");

        return (TResponse)failureMethod.Invoke(null, [error])!;
    }
}
