namespace backend.Models;

public class User
{
    public int Id { get; set; }
    public string OAuthProvider { get; set; }
    public string OAuthId { get; set; }
    public string Email { get; set; }
    public string DisplayName { get; set; }
    public string ProfileImageUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;
    public ICollection<TimelineEntry> TimelineEntries { get; set; }
    public ICollection<ApiConnection> ApiConnections { get; set; }
    public string? GitHubUsername { get; set; }

}
