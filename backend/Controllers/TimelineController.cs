using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using backend.Data;
using backend.Models;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TimelineController : ControllerBase
{
    private readonly AppDbContext _context;

    public TimelineController(AppDbContext context)
    {
        _context = context;
    }

    private int? GetCurrentUserId()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var user = _context.Users.FirstOrDefault(u => u.Email == email);
        return user?.Id;
    }
    [HttpGet]
    public async Task<IActionResult> GetMyEntries([FromQuery] string? search = null)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var query = _context.TimelineEntries
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.EventDate)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e =>
                e.Title.Contains(search) ||
                e.Description.Contains(search) ||
                e.Category.Contains(search));
        }

        var results = await query.ToListAsync();
        return Ok(results);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetEntry(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var entry = await _context.TimelineEntries
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (entry == null) return NotFound();

        return Ok(entry);
    }

    [HttpPost]
    public async Task<IActionResult> CreateEntry([FromBody] TimelineEntry entry)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        entry.UserId = userId.Value;
        entry.CreatedAt = DateTime.UtcNow;
        entry.UpdatedAt = DateTime.UtcNow;

        _context.TimelineEntries.Add(entry);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetEntry), new { id = entry.Id }, entry);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateEntry(int id, [FromBody] TimelineEntry updated)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var entry = await _context.TimelineEntries
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (entry == null) return NotFound();

        entry.Title = updated.Title;
        entry.Description = updated.Description;
        entry.EventDate = updated.EventDate;
        entry.EntryType = updated.EntryType;
        entry.Category = updated.Category;
        entry.ImageUrl = updated.ImageUrl;
        entry.ExternalUrl = updated.ExternalUrl;
        entry.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(entry);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteEntry(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var entry = await _context.TimelineEntries
            .FirstOrDefaultAsync(e => e.Id == id && e.UserId == userId);

        if (entry == null) return NotFound();

        _context.TimelineEntries.Remove(entry);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}
