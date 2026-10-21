using System.Text.Json;

namespace Application;

/// <summary>WP4: xoá (soft) recipe của mình; entry đã log giữ snapshot.</summary>
public sealed record DeleteRecipeCommand(Guid Id) : IRequest<Unit>;
