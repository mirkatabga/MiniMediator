using MiniMediator;

namespace MiniMediator.Tests;

public sealed class MediatorTests
{
    [Fact]
    public async Task Send_WithoutBehaviors_InvokesHandlerAndReturnsResponse()
    {
        var handler = new EchoHandler();
        var mediator = CreateMediator(
            (typeof(IRequestHandler<EchoRequest, string>), handler));

        var result = await mediator.Send(new EchoRequest("hello"));

        Assert.Equal("echo:hello", result);
        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task Send_WithBehaviors_ExecutesInRegistrationOrderAroundHandler()
    {
        var trace = new List<string>();
        var handler = new TraceHandler(trace);
        var behavior1 = new TraceBehavior("b1", trace);
        var behavior2 = new TraceBehavior("b2", trace);

        var mediator = CreateMediator(
            (typeof(IRequestHandler<EchoRequest, string>), handler),
            (typeof(IEnumerable<IPipelineBehavior<EchoRequest, string>>),
                new IPipelineBehavior<EchoRequest, string>[] { behavior1, behavior2 }));

        var result = await mediator.Send(new EchoRequest("x"));

        Assert.Equal("ok:x", result);
        Assert.Equal(
            ["b1:before", "b2:before", "handler", "b2:after", "b1:after"],
            trace);
    }

    [Fact]
    public async Task Send_WhenHandlerMissing_ThrowsInvalidOperationException()
    {
        var mediator = new Mediator(_ => null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mediator.Send(new EchoRequest("missing")));

        Assert.Contains("No handler was registered", ex.Message);
    }

    private static Mediator CreateMediator(params (Type serviceType, object service)[] services)
    {
        var map = services.ToDictionary(x => x.serviceType, x => (object?)x.service);
        return new Mediator(type => map.TryGetValue(type, out var service) ? service : null);
    }

    private sealed record EchoRequest(string Text) : IRequest<string>;

    private sealed class EchoHandler : IRequestHandler<EchoRequest, string>
    {
        public int CallCount { get; private set; }

        public Task<string> Handle(EchoRequest request, CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult($"echo:{request.Text}");
        }
    }

    private sealed class TraceHandler(List<string> trace) : IRequestHandler<EchoRequest, string>
    {
        public Task<string> Handle(EchoRequest request, CancellationToken cancellationToken)
        {
            trace.Add("handler");
            return Task.FromResult($"ok:{request.Text}");
        }
    }

    private sealed class TraceBehavior(string name, List<string> trace)
        : IPipelineBehavior<EchoRequest, string>
    {
        public async Task<string> Handle(
            EchoRequest request,
            CancellationToken cancellationToken,
            RequestHandlerDelegate<string> next)
        {
            trace.Add($"{name}:before");
            var result = await next();
            trace.Add($"{name}:after");
            return result;
        }
    }
}
