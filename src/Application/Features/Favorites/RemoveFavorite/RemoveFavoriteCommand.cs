namespace Application;

public sealed record RemoveFavoriteCommand(Guid Id) : IRequest<Unit>;
