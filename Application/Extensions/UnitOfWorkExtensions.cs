namespace Application;

public static class UnitOfWorkExtensions
{
    /// <summary>
    /// Lưu changes và throw InternalServerException nếu không có row nào được affected.
    /// </summary>
    public static async Task EnsureSaveAsync(this IUnitOfWork uow, CancellationToken ct = default)
    {
        var rows = await uow.SaveChangesAsync(ct);
        if (rows < 1)
            throw new DatabaseOperationException(ExceptionMessages.DatabaseOperationFailed);
    }
}
