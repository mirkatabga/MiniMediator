# MiniMediator

MiniMediator is a tiny MediatR-style implementation for teams that use:

`request/handler + a few behaviors`

## Supported Features

- `IRequest<TResponse>`
- `IRequestHandler<TRequest, TResponse>`
- `IPipelineBehavior<TRequest, TResponse>`
- `IMediator.Send(...)`

## Run Sample

```powershell
dotnet run --project MiniMediator.Sample/MiniMediator.Sample.csproj
```

The sample shows:

- a request (`Ping`)
- one handler (`PingHandler`)
- two behaviors (`LoggingBehavior`, `TimingBehavior`)
