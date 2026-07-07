using backend.Data;
using backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;

namespace backend.Services
{
    public class GitHubService
    {
        private readonly AppDbContext _db;
        private readonly HttpClient _http;

        public GitHubService(AppDbContext db, HttpClient http)
        {
            _db = db;
            _http = http;
        }
        private async Task<string> PrepareGitHubClient(int userId)
        {
            var conn = await _db.ApiConnections
                .FirstOrDefaultAsync(x => x.UserId == userId && x.ApiProvider == "GitHub");

            if (conn == null || string.IsNullOrWhiteSpace(conn.AccessToken))
                throw new Exception("GitHub token is missing. Please connect GitHub first.");

            _http.DefaultRequestHeaders.Clear();
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("PersonalTimelineApp");
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", conn.AccessToken);

            return conn.AccessToken;
        }
        public async Task<List<TimelineEntry>> SyncGitHubEventsAsync(int userId)
        {
            await PrepareGitHubClient(userId);

            var user = await _db.Users.FindAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.GitHubUsername))
                throw new Exception("GitHub username not set. Please save GitHub username first.");
            string url = $"https://api.github.com/users/{user.GitHubUsername}/events";

            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"GitHub API error: {error}");
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            var events = doc.RootElement;
            var newEntries = new List<TimelineEntry>();

            foreach (var ev in events.EnumerateArray())
            {
                string eventId = ev.GetProperty("id").GetString()!;
                string type = ev.GetProperty("type").GetString()!;
                string repoName = ev.GetProperty("repo").GetProperty("name").GetString()!;
                DateTime createdAt = ev.GetProperty("created_at").GetDateTime();

                bool exists = await _db.TimelineEntries.AnyAsync(x =>
                    x.UserId == userId &&
                    x.SourceApi == "GitHub" &&
                    x.ExternalId == eventId
                );

                if (exists)
                    continue;

                string? title = type switch
                {
                    "PushEvent"    => $"Pushed commits to {repoName}",
                    "CreateEvent"  => $"Created repo {repoName}",
                    "ForkEvent"    => $"Forked {repoName}",
                    "WatchEvent"   => $"Starred {repoName}",
                    "PullRequestEvent" => $"Opened pull request in {repoName}",
                    "IssuesEvent"  => $"Worked on an issue in {repoName}",
                    _ => null
                };

                if (title == null)
                    continue;

                var entry = new TimelineEntry
                {
                    UserId = userId,
                    Title = title,
                    Description = $"GitHub activity: {type}",
                    EventDate = createdAt,
                    EntryType = "Coding",
                    Category = "GitHub",
                    SourceApi = "GitHub",
                    ExternalId = eventId,
                    ExternalUrl = $"https://github.com/{repoName}",
                    Metadata = "{}",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _db.TimelineEntries.Add(entry);
                newEntries.Add(entry);
            }

            await _db.SaveChangesAsync();
            return newEntries;
        }
    }
}
