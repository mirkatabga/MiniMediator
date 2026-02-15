using MiniMediator;

var services = new Dictionary<Type, object?>
{
    [typeof(IRequestHandler<Ping, string>)] = new PingHandler(),
    [typeof(IEnumerable<IPipelineBehavior<Ping, string>>)] = new IPipelineBehavior<Ping, string>[]
    {
        new LoggingBehavior<Ping, string>(),
        new TimingBehavior<Ping, string>()
    }
};

var mediator = new Mediator(type => services.TryGetValue(type, out var service) ? service : null);
var response = await mediator.Send(new Ping("mini mediator"));

Console.WriteLine($"Final response: {response}");

public sealed record Ping(string Message) : IRequest<string>;

public sealed class PingHandler : IRequestHandler<Ping, string>
{
    public Task<string> Handle(Ping request, CancellationToken cancellationToken)
        => Task.FromResult($"Handled: {request.Message}");
}

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        CancellationToken cancellationToken,
        RequestHandlerDelegate<TResponse> next)
    {
        Console.WriteLine($"[Log] Before {typeof(TRequest).Name}");
        var response = await next();
        Console.WriteLine($"[Log] After {typeof(TRequest).Name}");
        return response;
    }
}

public sealed class TimingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        CancellationToken cancellationToken,
        RequestHandlerDelegate<TResponse> next)
    {
        var start = DateTimeOffset.UtcNow;
        var response = await next();
        var elapsed = DateTimeOffset.UtcNow - start;
        Console.WriteLine($"[Timing] {typeof(TRequest).Name}: {elapsed.TotalMilliseconds:N0} ms");
        return response;
    }
}
