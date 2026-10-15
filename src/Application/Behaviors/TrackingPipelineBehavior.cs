namespace Application;

public class TrackingPipelineBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<TRequest> _logger;

    public TrackingPipelineBehavior(ILogger<TRequest> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        var response = await next(cancellationToken);
        timer.Stop();

        _logger.LogInformation("Request: {Name} ({ElapsedMilliseconds}ms) {@Request}",
            typeof(TRequest).Name, timer.ElapsedMilliseconds, request);

        return response;
    }
}
