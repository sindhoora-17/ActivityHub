using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using backend.Data;
using backend.Models;

namespace backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var email = User.FindFirstValue(ClaimTypes.Email);
        var name = User.FindFirstValue(ClaimTypes.Name);
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var picture = User.FindFirstValue("urn:google:picture") ?? "";

        if (string.IsNullOrEmpty(email))
            return Unauthorized("No user info found.");

        var user = _context.Users.FirstOrDefault(u => u.Email == email);
        if (user == null)
        {
            user = new User
            {
                OAuthProvider = "Google",
                OAuthId = id ?? Guid.NewGuid().ToString(),
                Email = email,
                DisplayName = name ?? "",
                ProfileImageUrl = picture
            };
            _context.Users.Add(user);
        }
        else
        {
            user.DisplayName = name ?? user.DisplayName;
            user.ProfileImageUrl = picture;
            user.LastLoginAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(new
        {
            user.Id,
            user.DisplayName,
            user.Email,
            user.ProfileImageUrl
        });
    }
}
