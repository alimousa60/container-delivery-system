using BCrypt.Net;
using ContainerDelivery.Core.Interfaces;

namespace ContainerDelivery.Infrastructure.Auth;

public class PasswordService : IPasswordService
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
    }

    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }

    public bool NeedsRehash(string hash)
    {
        return BCrypt.Net.BCrypt.PasswordNeedsRehash(hash, 12);
    }
}

public interface IPasswordService
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string hash);
    bool NeedsRehash(string hash);
}