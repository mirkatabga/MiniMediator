using System.Reflection;

namespace MiniMediator;

public sealed class Mediator(Func<Type, object?> serviceFactory) : IMediator
{
    private readonly Func<Type, object?> _serviceFactory = serviceFactory;

    public Task<TResponse> Send<TResponse>(
        IRequest<TResponse> request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestType = request.GetType();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, typeof(TResponse));
        var behaviorCollectionType = typeof(IEnumerable<>).MakeGenericType(behaviorType);

        var handler = _serviceFactory(handlerType)
            ?? throw new InvalidOperationException($"No handler was registered for {requestType.Name}.");

        var behaviors = (_serviceFactory(behaviorCollectionType) as IEnumerable<object>)?.Reverse()
            ?? [];

        RequestHandlerDelegate<TResponse> next = () =>
            InvokeHandler<TResponse>(handler, request, cancellationToken);

        foreach (var behavior in behaviors)
        {
            var current = next;
            next = () => InvokeBehavior<TResponse>(behavior, request, cancellationToken, current);
        }

        return next();
    }

    private static Task<TResponse> InvokeHandler<TResponse>(
        object handler,
        object request,
        CancellationToken cancellationToken)
    {
        const string handleMethodName = nameof(IRequestHandler<IRequest<TResponse>, TResponse>.Handle);

        var method = handler.GetType().GetMethod(
            handleMethodName,
            BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Handler type {handler.GetType().Name} has no Handle method.");

        var result = method.Invoke(handler, [request, cancellationToken]);
        return result as Task<TResponse>
            ?? throw new InvalidOperationException($"Handler {handler.GetType().Name} returned an invalid task.");
    }

    private static Task<TResponse> InvokeBehavior<TResponse>(
        object behavior,
        object request,
        CancellationToken cancellationToken,
        RequestHandlerDelegate<TResponse> next)
    {
        const string handleMethodName = nameof(IPipelineBehavior<IRequest<TResponse>, TResponse>.Handle);

        var method = behavior.GetType().GetMethod(
            handleMethodName,
            BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException($"Behavior type {behavior.GetType().Name} has no Handle method.");

        var result = method.Invoke(behavior, [request, cancellationToken, next]);
        return result as Task<TResponse>
            ?? throw new InvalidOperationException($"Behavior {behavior.GetType().Name} returned an invalid task.");
    }
}
