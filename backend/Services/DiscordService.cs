using backend.Data;
using backend.Models;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class DiscordService
    {
        private readonly AppDbContext _db;
        private readonly HttpClient _http;

        public DiscordService(AppDbContext db, HttpClient http)
        {
            _db = db;
            _http = http;
        }

        private async Task<string?> GetToken(int userId)
        {
            var conn = await _db.ApiConnections
                .FirstOrDefaultAsync(c => c.UserId == userId && c.ApiProvider == "Discord");

            return conn?.AccessToken;
        }

        public async Task<List<TimelineEntry>> SyncAsync(int userId)
        {
            var token = await GetToken(userId);
            if (token == null) return new List<TimelineEntry>();

            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var newEntries = new List<TimelineEntry>();

            var guildJson = await _http.GetStringAsync("https://discord.com/api/users/@me/guilds");
            var guildArray = JsonDocument.Parse(guildJson).RootElement.EnumerateArray();

            foreach (var g in guildArray)
            {
                string serverName = g.GetProperty("name").GetString()!;
                string serverId = g.GetProperty("id").GetString()!;

                DateTime eventDate = DateTime.UtcNow;
                string description = "Discord Server";

                try
                {
                    var memberJson = await _http.GetStringAsync($"https://discord.com/api/users/@me/guilds/{serverId}/member");
                    var member = JsonDocument.Parse(memberJson).RootElement;

                    if (member.TryGetProperty("joined_at", out var joinedProp))
                    {
                        eventDate = DateTime.Parse(joinedProp.GetString()!).ToUniversalTime();
                    }
                    else
                    {
                        description = "Discord Server (join date unavailable)";
                    }
                }
                catch
                {
                    description = "Discord Server (join date unavailable)";
                }

                bool exists = await _db.TimelineEntries.AnyAsync(e =>
                    e.UserId == userId &&
                    e.SourceApi == "Discord" &&
                    e.ExternalId == serverId);

                if (!exists)
                {
                    newEntries.Add(new TimelineEntry
                    {
                        UserId = userId,
                        Title = $"Joined Discord Server: {serverName}",
                        Description = description,
                        EventDate = eventDate,
                        EntryType = "Milestone",
                        Category = "Discord",
                        SourceApi = "Discord",
                        ExternalId = serverId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }

            if (newEntries.Any())
            {
                _db.TimelineEntries.AddRange(newEntries);
                await _db.SaveChangesAsync();
            }

            return newEntries;
        }
    }
}
