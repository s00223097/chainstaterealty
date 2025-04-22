namespace Shared.Model
{
    /// <summary>
    /// Interface for user information that can be shared across projects
    /// </summary>
    public interface IUser
    {
        string Id { get; }
        string UserName { get; }
        string Email { get; }
    }
} 