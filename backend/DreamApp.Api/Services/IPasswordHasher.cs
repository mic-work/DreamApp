namespace DreamApp.Api.Services;

public interface IPasswordHasher
{
    /// <summary>Hashes a plain-text password into a self-contained, storable string.</summary>
    string Hash(string password);

    /// <summary>Verifies a plain-text password against a previously stored hash.</summary>
    bool Verify(string password, string storedHash);
}
