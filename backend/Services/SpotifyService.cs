using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace backend.Services
{
    public class SpotifyService
    {
        private readonly HttpClient _http;
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        public SpotifyService(HttpClient http, AppDbContext db, IConfiguration config)
        {
            _http = http;
            _db = db;
            _config = config;
        }
        public async Task<string?> RefreshSpotifyTokenAsync(int userId)
        {
            var conn = await _db.ApiConnections
                .FirstOrDefaultAsync(x => x.UserId == userId && x.ApiProvider == "Spotify");

            if (conn == null || string.IsNullOrEmpty(conn.RefreshToken))
                return null;

            var req = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
            req.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = conn.RefreshToken,
                ["client_id"] = _config["Spotify:ClientId"]!,
                ["client_secret"] = _config["Spotify:ClientSecret"]!,
            });

            var res = await _http.SendAsync(req);
            var json = JsonDocument.Parse(await res.Content.ReadAsStringAsync());

            if (!res.IsSuccessStatusCode)
                return null;

            var newToken = json.RootElement.GetProperty("access_token").GetString()!;
            conn.AccessToken = newToken;
            conn.TokenExpiresAt = DateTime.UtcNow.AddHours(1);

            await _db.SaveChangesAsync();
            return newToken;
        }
        public async Task<List<TimelineEntry>> SyncRecentlyPlayedAsync(int userId)
        {
            var conn = await _db.ApiConnections
                .FirstOrDefaultAsync(x => x.UserId == userId && x.ApiProvider == "Spotify");

            if (conn == null)
                throw new Exception("Spotify is not connected.");

            if (conn.TokenExpiresAt < DateTime.UtcNow)
            {
                var newToken = await RefreshSpotifyTokenAsync(userId);
                if (newToken == null)
                    throw new Exception("Failed to refresh Spotify token.");

                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", newToken);
            }
            else
            {
                _http.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", conn.AccessToken);
            }
            var response = await _http.GetAsync(
                "https://api.spotify.com/v1/me/player/recently-played?limit=10"
            );

            if (!response.IsSuccessStatusCode)
                throw new Exception("Spotify API call failed: " + response.StatusCode);

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var items = doc.RootElement.GetProperty("items");
            var newEntries = new List<TimelineEntry>();

            foreach (var item in items.EnumerateArray())
            {
                var track = item.GetProperty("track");
                var name = track.GetProperty("name").GetString();
                var artist = track.GetProperty("artists")[0].GetProperty("name").GetString();
                var playedAt = item.GetProperty("played_at").GetDateTime();

                var entry = new TimelineEntry
                {
                    UserId = userId,
                    Title = $"Listened to: {name}",
                    Description = $"By {artist}",
                    EventDate = playedAt,
                    EntryType = "Activity",
                    Category = "Music",
                    SourceApi = "Spotify",
                    ExternalId = track.GetProperty("id").GetString()!,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                bool exists = await _db.TimelineEntries.AnyAsync(x =>
                    x.UserId == userId &&
                    x.SourceApi == "Spotify" &&
                    x.ExternalId == entry.ExternalId &&
                    x.EventDate == entry.EventDate
                );

                if (!exists)
                {
                    _db.TimelineEntries.Add(entry);
                    newEntries.Add(entry);
                }
            }

            await _db.SaveChangesAsync();
            return newEntries;
        }
    }
}
