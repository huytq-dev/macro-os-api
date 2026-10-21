namespace Application;

public sealed class UpsertVisualBodyFatCommandHandler(ISender _sender) : IRequestHandler<UpsertVisualBodyFatCommand, Guid>
{
    public Task<Guid> Handle(UpsertVisualBodyFatCommand request, CancellationToken ct)
        => _sender.Send(new UpsertBodyFatCommand(request.Date, MeasurementMethod.Visual, request.Percent, null), ct);
}
