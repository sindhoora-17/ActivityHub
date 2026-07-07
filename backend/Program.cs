using backend.Data;
using backend.Models;
using backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication;
using System.Text.Json;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient<GoogleCalendarService>();
builder.Services.AddScoped<GoogleCalendarService>();

builder.Services.AddHttpClient<GitHubService>();
builder.Services.AddScoped<GitHubService>();

builder.Services.AddHttpClient<DiscordService>();
builder.Services.AddScoped<DiscordService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.HttpOnly = true;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = 401;
        return Task.CompletedTask;
    };
})
.AddGoogle(options =>
{
    options.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
    options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
    options.CallbackPath = "/signin-google";
});


builder.Services.AddAuthorization();

builder.Services.AddHttpClient<SpotifyService>();
builder.Services.AddScoped<SpotifyService>();

builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy
            .SetIsOriginAllowed(origin =>
                origin.StartsWith("http://127.0.0.1"))
            .AllowCredentials()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/auth/google/login", () =>
    Results.Challenge(new AuthenticationProperties
    {
        RedirectUri = "/auth/google/success"
    }, new List<string> { "Google" })
);

app.MapGet("/auth/google/success", async (HttpContext http, AppDbContext db) =>
{
    if (!http.User.Identity!.IsAuthenticated)
        return Results.Unauthorized();

    var email = http.User.FindFirst(ClaimTypes.Email)?.Value
                ?? http.User.Claims.First(c => c.Type.Contains("email")).Value;

    var name = http.User.FindFirst(ClaimTypes.Name)?.Value
               ?? http.User.Claims.First(c => c.Type.Contains("name")).Value;

    var sub = http.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
              ?? http.User.Claims.First(c => c.Type.Contains("sub")).Value;

    var picture = http.User.Claims.FirstOrDefault(c => c.Type.Contains("picture"))?.Value ?? "";

    var user = await db.Users.FirstOrDefaultAsync(u => u.OAuthId == sub);

    if (user == null)
    {
        user = new User
        {
            OAuthProvider = "Google",
            OAuthId = sub,
            Email = email,
            DisplayName = name,
            ProfileImageUrl = picture,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
    }
    else
    {
        user.LastLoginAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.OAuthId),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim("userId", user.Id.ToString())
    };

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

    return Results.Redirect("http://127.0.0.1:5173/");
});

app.MapGet("/api/spotify/login", (HttpContext http, IConfiguration config) =>
{
    var clientId = config["Spotify:ClientId"];
    var redirectUri = config["Spotify:RedirectUri"];
    var encodedRedirect = Uri.EscapeDataString(redirectUri!);

    var url = $"https://accounts.spotify.com/authorize" +
              $"?client_id={clientId}" +
              $"&response_type=code" +
              $"&redirect_uri={encodedRedirect}" +
              $"&scope=user-read-recently-played%20user-top-read";

    return Results.Redirect(url);
});

app.MapGet("/api/spotify/callback", async (HttpContext http, IConfiguration config, AppDbContext db, HttpClient client) =>
{
    var code = http.Request.Query["code"].ToString();
    if (string.IsNullOrEmpty(code))
        return Results.BadRequest("No authorization code received.");

    var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://accounts.spotify.com/api/token");
    tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["grant_type"] = "authorization_code",
        ["code"] = code,
        ["redirect_uri"] = config["Spotify:RedirectUri"]!,
        ["client_id"] = config["Spotify:ClientId"]!,
        ["client_secret"] = config["Spotify:ClientSecret"]!
    });

    var response = await client.SendAsync(tokenRequest);
    var json = await response.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(json);

    var accessToken = doc.RootElement.GetProperty("access_token").GetString()!;
    var refreshToken = doc.RootElement.GetProperty("refresh_token").GetString()!;

    var userId = int.Parse(http.User.FindFirst("userId")!.Value);

    var existing = await db.ApiConnections
        .FirstOrDefaultAsync(x => x.UserId == userId && x.ApiProvider == "Spotify");

    if (existing == null)
    {
        db.ApiConnections.Add(new ApiConnection
        {
            UserId = userId,
            ApiProvider = "Spotify",
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenExpiresAt = DateTime.UtcNow.AddHours(1),
            IsActive = true,
        });
    }
    else
    {
        existing.AccessToken = accessToken;
        existing.RefreshToken = refreshToken;
        existing.TokenExpiresAt = DateTime.UtcNow.AddHours(1);
    }

    await db.SaveChangesAsync();
    return Results.Ok("Spotify connected successfully");
});

app.MapPost("/api/spotify/sync", async (HttpContext http, IConfiguration config, SpotifyService spotify, AppDbContext db) =>
{
    if (!http.User.Identity!.IsAuthenticated)
        return Results.Unauthorized();

    var userIdClaim = http.User.FindFirst("userId");
    if (userIdClaim == null)
        return Results.Json(new { error = "User session missing." }, statusCode: 401);

    var userId = int.Parse(userIdClaim.Value);
    var conn = await db.ApiConnections
        .FirstOrDefaultAsync(x => x.UserId == userId && x.ApiProvider == "Spotify");

    if (conn == null)
    {
        return Results.Json(new
        {
            redirect = $"{config["FrontendBase"]}/api/spotify/login"
        });
    }
    if (conn.TokenExpiresAt <= DateTime.UtcNow)
    {
        var newToken = await spotify.RefreshSpotifyTokenAsync(userId);

        if (newToken == null)
        {
            return Results.Json(new
            {
                redirect = $"{config["FrontendBase"]}/api/spotify/login"
            });
        }
    }
    var newEntries = await spotify.SyncRecentlyPlayedAsync(userId);
    return Results.Ok(new { added = newEntries.Count, entries = newEntries });
});

app.MapGet("/auth/me", async (HttpContext http, AppDbContext db) =>
{
    if (!http.User.Identity!.IsAuthenticated)
        return Results.Unauthorized();

    var userIdClaim = http.User.FindFirst("userId");
    if (userIdClaim == null)
        return Results.Json(new { error = "No userId claim found. Re-login at /auth/google/login" }, statusCode: 401);

    var userId = int.Parse(userIdClaim.Value);
    var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);

    if (user == null)
        return Results.Json(new { error = "User not found in database." }, statusCode: 404);

    return Results.Json(new
    {
        user.Id,
        user.Email,
        user.DisplayName,
        user.ProfileImageUrl,
        user.CreatedAt,
        user.LastLoginAt
    });
});

app.MapPost("/api/github/set-username", async (HttpContext http, AppDbContext db, string username) =>
{
    if (!http.User.Identity!.IsAuthenticated)
        return Results.Unauthorized();

    var userId = int.Parse(http.User.FindFirst("userId")!.Value);
    var user = await db.Users.FindAsync(userId);

    if (user == null)
        return Results.NotFound("User not found");

    user.GitHubUsername = username;
    await db.SaveChangesAsync();

    return Results.Ok("GitHub username saved!");
});

app.MapPost("/api/github/connect", async (HttpContext http, AppDbContext db, IConfiguration config, string token) =>
{
    if (!http.User.Identity!.IsAuthenticated)
        return Results.Unauthorized();

    var userId = int.Parse(http.User.FindFirst("userId")!.Value);

    var existing = await db.ApiConnections
        .FirstOrDefaultAsync(x => x.UserId == userId && x.ApiProvider == "GitHub");

    if (existing == null)
    {
        db.ApiConnections.Add(new ApiConnection
        {
            UserId = userId,
            ApiProvider = "GitHub",
            AccessToken = token,
            RefreshToken = "",
            TokenExpiresAt = DateTime.UtcNow.AddYears(10),
            IsActive = true
        });
    }
    else
    {
        existing.AccessToken = token;
    }

    await db.SaveChangesAsync();
    return Results.Ok("GitHub token saved");
});

app.MapPost("/api/github/sync", async (HttpContext http, GitHubService github) =>
{
    if (!http.User.Identity!.IsAuthenticated)
        return Results.Unauthorized();

    var userId = int.Parse(http.User.FindFirst("userId")!.Value);
    var results = await github.SyncGitHubEventsAsync(userId);

    return Results.Ok(new { added = results.Count, entries = results });
});

app.MapGet("/api/discord/login", (IConfiguration config) =>
{
    var clientId = config["Discord:ClientId"]!;
    var redirectUri = config["Discord:RedirectUri"]!;
    var scopes = "identify guilds guilds.members.read";
    var encodedScopes = Uri.EscapeDataString(scopes);

    var url =
        $"https://discord.com/api/oauth2/authorize" +
        $"?client_id={clientId}" +
        $"&response_type=code" +
        $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
        $"&scope={Uri.EscapeDataString(scopes)}" +
        $"&prompt=consent";

    Console.WriteLine("DEBUG LOGIN URL: " + url);

    return Results.Redirect(url);
});

app.MapGet("/api/discord/callback", async (HttpContext http, IConfiguration config, AppDbContext db, HttpClient client) =>
{
    var code = http.Request.Query["code"];
    if (string.IsNullOrEmpty(code))
        return Results.BadRequest("No code received");

    var tokenRequest = new HttpRequestMessage(HttpMethod.Post, "https://discord.com/api/oauth2/token");
    tokenRequest.Content = new FormUrlEncodedContent(new Dictionary<string, string>
    {
        ["client_id"] = config["Discord:ClientId"]!,
        ["client_secret"] = config["Discord:ClientSecret"]!,
        ["grant_type"] = "authorization_code",
        ["code"] = code!,
        ["redirect_uri"] = config["Discord:RedirectUri"]!
    });

    var tokenResponse = await client.SendAsync(tokenRequest);
    var json = JsonDocument.Parse(await tokenResponse.Content.ReadAsStringAsync());

    var accessToken = json.RootElement.GetProperty("access_token").GetString()!;
    var refreshToken = json.RootElement.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;

    var userId = int.Parse(http.User.FindFirst("userId")!.Value);

    var existing = await db.ApiConnections
        .FirstOrDefaultAsync(x => x.UserId == userId && x.ApiProvider == "Discord");

    if (existing == null)
    {
        db.ApiConnections.Add(new ApiConnection
        {
            UserId = userId,
            ApiProvider = "Discord",
            AccessToken = accessToken,
            RefreshToken = refreshToken ?? "",
            TokenExpiresAt = DateTime.UtcNow.AddHours(1),
            IsActive = true
        });
    }
    else
    {
        existing.AccessToken = accessToken;
        existing.RefreshToken = refreshToken ?? existing.RefreshToken;
        existing.TokenExpiresAt = DateTime.UtcNow.AddHours(1);
    }

    await db.SaveChangesAsync();

    return Results.Ok("Discord Connected Successfully!");
});

app.MapPost("/api/discord/sync", async (DiscordService discord, HttpContext http, AppDbContext db) =>
{
    if (!http.User.Identity!.IsAuthenticated)
        return Results.Unauthorized();

    var userId = int.Parse(http.User.FindFirst("userId")!.Value);
    var conn = await db.ApiConnections
        .FirstOrDefaultAsync(x => x.UserId == userId && x.ApiProvider == "Discord");

    if (conn == null)
    {
        return Results.Json(new { redirect = "/api/discord/login" });
    }

    var newEntries = await discord.SyncAsync(userId);
    return Results.Ok(new { added = newEntries.Count, entries = newEntries });
});

app.MapGet("/api/gcal/login", (IConfiguration config) =>
{
    var clientId = config["GoogleCalendar:ClientId"]!;
    var redirectUri = Uri.EscapeDataString(config["GoogleCalendar:RedirectUri"]!);

    var scope = Uri.EscapeDataString("https://www.googleapis.com/auth/calendar.events.readonly");

    var url =
        "https://accounts.google.com/o/oauth2/v2/auth" +
        $"?client_id={clientId}" +
        "&response_type=code" +
        $"&redirect_uri={redirectUri}" +
        $"&scope={scope}" +
        "&access_type=offline" +
        "&include_granted_scopes=true" +
        "&prompt=consent";

    return Results.Redirect(url);
});

app.MapGet("/api/gcal/callback", async (
    HttpContext http,
    IConfiguration config,
    AppDbContext db,
    HttpClient client) =>
{
    var code = http.Request.Query["code"].ToString();
    if (string.IsNullOrEmpty(code))
        return Results.BadRequest("No authorization code received.");

    var tokenReq = new HttpRequestMessage(HttpMethod.Post, "https://oauth2.googleapis.com/token")
    {
        Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["client_id"] = config["GoogleCalendar:ClientId"]!,
            ["client_secret"] = config["GoogleCalendar:ClientSecret"]!,
            ["redirect_uri"] = config["GoogleCalendar:RedirectUri"]!,
            ["grant_type"] = "authorization_code"
        })
    };

    var tokenRes = await client.SendAsync(tokenReq);
    var tokenJson = await tokenRes.Content.ReadAsStringAsync();
    using var doc = JsonDocument.Parse(tokenJson);

    if (!tokenRes.IsSuccessStatusCode)
        return Results.BadRequest($"Token exchange failed: {tokenJson}");

    var accessToken = doc.RootElement.GetProperty("access_token").GetString()!;
    var expiresInSec = doc.RootElement.GetProperty("expires_in").GetInt32();
    var refreshToken =
        doc.RootElement.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;

    if (!http.User.Identity!.IsAuthenticated)
        return Results.Unauthorized();

    var userId = int.Parse(http.User.FindFirst("userId")!.Value);

    var existing = await db.ApiConnections
        .FirstOrDefaultAsync(x => x.UserId == userId && x.ApiProvider == "GoogleCalendar");

    if (existing == null)
    {
        db.ApiConnections.Add(new ApiConnection
        {
            UserId = userId,
            ApiProvider = "GoogleCalendar",
            AccessToken = accessToken,
            RefreshToken = refreshToken ?? "",
            TokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresInSec - 60),
            IsActive = true
        });
    }
    else
    {
        existing.AccessToken = accessToken;
        if (!string.IsNullOrEmpty(refreshToken))
            existing.RefreshToken = refreshToken;
        existing.TokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresInSec - 60);
    }

    await db.SaveChangesAsync();
    return Results.Ok("Google Calendar connected successfully!");
});

app.MapPost("/api/gcal/sync", async (GoogleCalendarService gcal, HttpContext http, AppDbContext db) =>
{
    if (!http.User.Identity!.IsAuthenticated)
        return Results.Unauthorized();

    var userId = int.Parse(http.User.FindFirst("userId")!.Value);

    var conn = await db.ApiConnections
        .FirstOrDefaultAsync(x => x.UserId == userId && x.ApiProvider == "GoogleCalendar");

    if (conn == null)
    {
        return Results.Json(new { redirect = "/api/gcal/login" });
    }

    var newEntries = await gcal.SyncAsync(userId);
    return Results.Ok(new { added = newEntries.Count, entries = newEntries });
});

app.MapPost("/auth/logout", async (HttpContext http) =>
{
    await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Ok(new { message = "Logged out" });
});

app.MapGet("/api/status", async (HttpContext http, AppDbContext db) =>
{
    if (!http.User.Identity!.IsAuthenticated)
        return Results.Unauthorized();

    var userId = int.Parse(http.User.FindFirst("userId")!.Value);

    var connections = await db.ApiConnections
        .Where(c => c.UserId == userId)
        .ToListAsync();

    return Results.Json(new
    {
        spotify = connections.Any(c => c.ApiProvider == "Spotify" && c.IsActive),
        github = connections.Any(c => c.ApiProvider == "GitHub" && c.IsActive),
        discord = connections.Any(c => c.ApiProvider == "Discord" && c.IsActive),
        gcal = connections.Any(c => c.ApiProvider == "GoogleCalendar" && c.IsActive)
    });
});

app.MapGet("/api/connections", async (HttpContext http, AppDbContext db) =>
{
    if (!http.User.Identity!.IsAuthenticated)
        return Results.Ok(new
        {
            spotify = false,
            github = false,
            discord = false,
            gcal = false
        });

    var userId = int.Parse(http.User.FindFirst("userId")!.Value);

    var conns = await db.ApiConnections
        .Where(c => c.UserId == userId)
        .ToListAsync();

    bool spotify = conns.Any(c => c.ApiProvider == "Spotify" && c.IsActive);
    bool github = conns.Any(c => c.ApiProvider == "GitHub" && c.IsActive);
    bool discord = conns.Any(c => c.ApiProvider == "Discord" && c.IsActive);
    bool gcal = conns.Any(c => c.ApiProvider == "GoogleCalendar" && c.IsActive);

    return Results.Ok(new
    {
        spotify,
        github,
        discord,
        gcal
    });
});

app.Run();
