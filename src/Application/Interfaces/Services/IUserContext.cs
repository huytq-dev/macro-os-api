namespace Application;

public interface IUserContext
{
    Guid   UserId          { get; }
    string Email           { get; }
    string Role            { get; }
    bool   IsAuthenticated { get; }
}
