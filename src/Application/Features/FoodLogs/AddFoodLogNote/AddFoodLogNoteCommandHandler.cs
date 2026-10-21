namespace Application;

public sealed class AddFoodLogNoteCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<AddFoodLogNoteCommand, Guid>
{
    public async Task<Guid> Handle(AddFoodLogNoteCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var now = await FoodLogSupport.LocalNowAsync(_uow, userId, _timeProvider, ct);
        var date = request.LogDate ?? now.Date;

        // ponytail: đếm rồi thêm, hai POST đồng thời có thể vượt 5 một ghi chú; thêm ràng buộc khi cần chặt tuyệt đối
        var count = await _uow.Repository<FoodLogNote>().Query().CountAsync(n => n.UserId == userId && n.LogDate == date, ct);
        if (count >= FoodLogNoteLimits.MaxPerDay) throw new ConflictException(ExceptionMessages.NoteLimitReached(FoodLogNoteLimits.MaxPerDay));

        var (notedAt, _) = FoodLogSupport.ResolveTime(date, request.NotedAt, null, now);
        var note = new FoodLogNote
        {
            Id = Guid.NewGuid(), UserId = userId, LogDate = date, NotedAt = notedAt,
            Text = request.Text.Trim(), Name = FoodLogNoteRules.CleanName(request.Name), Color = request.Color ?? NoteColor.Gray,
        };
        await _uow.Repository<FoodLogNote>().AddAsync(note, ct);
        await _uow.SaveChangesAsync(ct);
        return note.Id;
    }
}
