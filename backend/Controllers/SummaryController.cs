using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using System.Security.Claims;

namespace backend.Controllers;

[ApiController]
[Route("api/summary")]
[Authorize]
public class SummaryController : ControllerBase
{
    private readonly AppDbContext _db;

    public SummaryController(AppDbContext db)
    {
        _db = db;
    }

    private int? GetUserId()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        return _db.Users.FirstOrDefault(u => u.Email == email)?.Id;
    }
    [HttpGet("spotify")]
    public async Task<IActionResult> SpotifySummary()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var entry = await _db.TimelineEntries
            .Where(e => e.UserId == userId && e.SourceApi == "Spotify")
            .OrderByDescending(e => e.EventDate)
            .FirstOrDefaultAsync();

        if (entry == null)
        {
            return Ok(new {
                lastPlayedTrack = "—",
                artist = "—"
            });
        }
        string track = entry.Title.Replace("Listened to: ", "");
        string artist = entry.Description.Replace("By ", "");

        return Ok(new {
            lastPlayedTrack = track,
            artist = artist
        });
    }
    [HttpGet("github")]
    public async Task<IActionResult> GithubSummary()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var entry = await _db.TimelineEntries
            .Where(e => e.UserId == userId && e.SourceApi == "GitHub")
            .OrderByDescending(e => e.EventDate)
            .FirstOrDefaultAsync();

        if (entry == null)
        {
            return Ok(new {
                lastActivity = "—",
                repo = "—"
            });
        }
        string repo = entry.Title.Split(" ").Last();

        return Ok(new {
            lastActivity = entry.Title,
            repo = repo
        });
    }

    [HttpGet("gcal")]
    public async Task<IActionResult> GcalSummary()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var nextEvent = await _db.TimelineEntries
            .Where(e => e.UserId == userId && e.SourceApi == "GoogleCalendar")
            .OrderBy(e => e.EventDate)
            .FirstOrDefaultAsync();

        if (nextEvent == null)
        {
            return Ok(new {
                nextEvent = "—",
                date = "—"
            });
        }

        return Ok(new {
            nextEvent = nextEvent.Title,
            date = nextEvent.EventDate.ToString("MMM dd • h:mm tt 'UTC'")
        });
    }

    [HttpGet("discord")]
    public async Task<IActionResult> DiscordSummary()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var entries = await _db.TimelineEntries
            .Where(e => e.UserId == userId && e.SourceApi == "Discord")
            .OrderByDescending(e => e.EventDate)
            .ToListAsync();

        if (!entries.Any())
        {
            return Ok(new {
                servers = 0,
                lastServer = "—"
            });
        }

        var latest = entries.First();
        string serverName = latest.Title.Replace("Joined Discord Server: ", "");

        return Ok(new {
            servers = entries.Count,
            lastServer = serverName
        });
    }
}
