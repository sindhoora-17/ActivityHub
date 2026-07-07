using System.Net.Http.Headers;
using System.Text.Json;
using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Services
{
    public class GoogleCalendarService
    {
        private readonly AppDbContext _db;
        private readonly HttpClient _http;
        private readonly IConfiguration _config;

        public GoogleCalendarService(AppDbContext db, HttpClient http, IConfiguration config)
        {
            _db = db;
            _http = http;
            _config = config;
        }

        private async Task<ApiConnection?> GetConn(int userId)
        {
            return await _db.ApiConnections.FirstOrDefaultAsync(c =>
                c.UserId == userId && c.ApiProvider == "GoogleCalendar");
        }

        private async Task<bool> EnsureAccessToken(ApiConnection conn)
        {
            // still valid
            if (conn.TokenExpiresAt > DateTime.UtcNow.AddMinutes(1))
                return true;

            if (string.IsNullOrWhiteSpace(conn.RefreshToken))
                return false;

            var req = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = _config["GoogleCalendar:ClientId"]!,
                    ["client_secret"] = _config["GoogleCalendar:ClientSecret"]!,
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = conn.RefreshToken
                })
            };

            var res = await _http.SendAsync(req);
            var json = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode) return false;

            using var doc = JsonDocument.Parse(json);
            var accessToken = doc.RootElement.GetProperty("access_token").GetString()!;
            var expiresInSec = doc.RootElement.GetProperty("expires_in").GetInt32();

            conn.AccessToken = accessToken;
            conn.TokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresInSec - 60);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<List<TimelineEntry>> SyncAsync(int userId)
        {
            var conn = await GetConn(userId);
            if (conn == null) return new List<TimelineEntry>();

            var ok = await EnsureAccessToken(conn);
            if (!ok) return new List<TimelineEntry>();

            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", conn.AccessToken);

            // Pull upcoming & recent events (tweak as you like)
            var nowIso = DateTime.UtcNow.ToString("o");

            // Only sync the next 30 days (change as you like)
            var timeMaxIso = DateTime.UtcNow.AddYears(1).ToString("o");

            var url =
                "https://www.googleapis.com/calendar/v3/calendars/primary/events" +
                $"?singleEvents=true" +
                $"&timeMin={Uri.EscapeDataString(nowIso)}" +
                $"&timeMax={Uri.EscapeDataString(timeMaxIso)}" +
                $"&orderBy=startTime";


            var raw = await _http.GetStringAsync(url);
            using var doc = JsonDocument.Parse(raw);

            var items = doc.RootElement.TryGetProperty("items", out var arr) ? arr.EnumerateArray().ToList() : new List<JsonElement>();

            var maxAllowed = DateTime.UtcNow.AddYears(1);

            items = items
                .Where(ev =>
                {
                    DateTime eventDate;

                    if (ev.TryGetProperty("start", out var st) && st.TryGetProperty("dateTime", out var dt))
                        eventDate = DateTime.Parse(dt.GetString()!).ToUniversalTime();
                    else if (st.TryGetProperty("date", out var date))
                        eventDate = DateTime.Parse(date.GetString()!).Date;
                    else
                        return false;

                    return eventDate <= maxAllowed;
                })
                .ToList();


            var newEntries = new List<TimelineEntry>();

            foreach (var ev in items)
            {
                var eventId = ev.GetProperty("id").GetString()!;
                var summary = ev.TryGetProperty("summary", out var s) ? s.GetString() ?? "(No title)" : "(No title)";
                var description = ev.TryGetProperty("description", out var d) ? d.GetString() ?? "" : "";
                var location = ev.TryGetProperty("location", out var l) ? l.GetString() ?? "" : "";

                DateTime eventStartUtc;
                if (ev.TryGetProperty("start", out var start) && start.TryGetProperty("dateTime", out var dtProp))
                {
                    eventStartUtc = DateTime.Parse(dtProp.GetString()!).ToUniversalTime();
                }
                else if (start.TryGetProperty("date", out var dateProp))
                {
                    var date = DateTime.Parse(dateProp.GetString()!).Date;
                    eventStartUtc = DateTime.SpecifyKind(date, DateTimeKind.Unspecified);
                }

                else
                {
                    eventStartUtc = DateTime.UtcNow;
                }

                bool exists = await _db.TimelineEntries.AnyAsync(e =>
                    e.UserId == userId &&
                    e.SourceApi == "GoogleCalendar" &&
                    e.ExternalId == eventId);

                if (!exists)
                {
                    newEntries.Add(new TimelineEntry
                    {
                        UserId = userId,
                        Title = $"Calendar: {summary}",
                        Description = string.IsNullOrEmpty(location) ? description : $"{description}\nLocation: {location}",
                        EventDate = eventStartUtc,
                        EntryType = "Event",
                        Category = "GoogleCalendar",
                        SourceApi = "GoogleCalendar",
                        ExternalId = eventId,
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
