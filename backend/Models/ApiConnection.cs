namespace backend.Models;

public class ApiConnection
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ApiProvider { get; set; } = default!;
    public string AccessToken { get; set; } = default!;
    public string? RefreshToken { get; set; }
    public DateTime TokenExpiresAt { get; set; }
    public bool IsActive { get; set; }
    public string? Settings { get; set; }
    public User User { get; set; } = default!;
}
