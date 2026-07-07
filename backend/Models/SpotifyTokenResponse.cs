namespace backend.Models
{
    public class SpotifyTokenResponse
    {
        public string AccessToken { get; set; } = default!;
        public string TokenType { get; set; } = default!;
        public int ExpiresIn { get; set; }
        public string RefreshToken { get; set; } = default!;
    }
}
