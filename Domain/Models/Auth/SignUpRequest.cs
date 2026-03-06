namespace Contract;

public sealed class SignUpRequest
{
    public required string Name { get; set; }
    public required string Username { get; set; }
    public required string Email { get; set; }
    public required string Password { get; set; }
    public required string StudentId { get; set; }
    public required string SchoolName { get; set; }
    public required string Role {  get; set; }
}