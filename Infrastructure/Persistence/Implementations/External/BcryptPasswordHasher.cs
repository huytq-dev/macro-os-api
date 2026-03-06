namespace Infrastructure;

[RegisterService(typeof(IPasswordHasher))]
public class BcryptPasswordHasher : IPasswordHasher
{
    public string Hash(string password)
        => BCrypt.Net.BCrypt.HashPassword(password);

    public bool Verify(string password, string hashed)
        => BCrypt.Net.BCrypt.Verify(password, hashed);
}
