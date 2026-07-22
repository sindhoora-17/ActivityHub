# ActivityHub — Personal Activity Timeline

## Overview
**ActivityHub** is a full-stack app that automatically builds a timeline of your
digital life by integrating with multiple third-party APIs, and lets you add
personal events by hand. Connect Spotify, GitHub, Google Calendar, and Discord,
and ActivityHub pulls your recent activity into one unified, filterable timeline.

This project demonstrates:

- Full-stack development with **React + .NET 8**
- **OAuth 2.0** authorization-code flows across four providers
- **Automatic per-provider access-token refresh** using stored refresh tokens
- Persistent storage via **Entity Framework Core + SQLite**
- Protected API routes and cookie-based sessions
- A modern UI with filters, sync buttons, and timeline cards

---

## Tech Stack

**Frontend:** React (JavaScript), React Router, Tailwind CSS, Vite
**Backend:** .NET 8 Web API, Entity Framework Core (SQLite), Swagger
**Auth:** OAuth 2.0 (Google login + Spotify / Google Calendar / Discord / GitHub connections), cookie-based sessions
**Database:** SQLite with EF Core code-first migrations

---

## Authentication

Login is handled through **Google OAuth**. Individual services (Spotify, Google
Calendar, Discord, GitHub) are then connected per user via their own OAuth
authorization-code flows.

**Login flow**
1. User clicks "Login with Google"
2. Redirect to the Google consent screen
3. Backend validates the returned Google identity
4. User info is stored in SQLite
5. A secure HttpOnly cookie manages the session
6. Only authenticated users can reach protected routes

**Token refresh.** For each connected provider, the backend stores the access
token, its expiry, and a refresh token. Before every sync it checks whether the
access token has expired and, if so, transparently exchanges the refresh token
for a new one — so syncing keeps working without asking the user to log in again.

Protected routes include `/dashboard`, `/timeline`, `/api/timeline/*`, and
`/api/summary/*`.

---

## Features

### 1. Dashboard summary cards
Shows the most recent activity from each connected service. Each tile has a Sync
button that queries the backend for updates.

| Service         | Summary shown                     |
|-----------------|-----------------------------------|
| Google Calendar | Next upcoming event               |
| Spotify         | Last played track + artist        |
| Discord         | Servers joined + last active server |
| GitHub          | Most recent repo activity         |

### 2. Timeline page
A card-based timeline with:
- Icons per source (Google, Spotify, Discord, GitHub)
- A filter bar (All, GoogleCalendar, Spotify, Discord, GitHub)
- Delete on each entry
- Responsive layout and animations

### 3. Add-entry modal
Manually add an entry (title, description, date, source), saved straight to SQLite.

### 4. Third-party integrations
- **Google Calendar** — fetches upcoming events, handles all-day events, saves new entries on sync
- **Spotify** — fetches most recently played track, parses track + artist
- **Discord** — fetches joined servers and recent activity
- **GitHub** — fetches push events, stars, and repo creation, saved as summary entries

Each provider's distinct API response is normalized into one shared
`TimelineEntry` model, and entries already saved are skipped on re-sync so
syncing never creates duplicates.

---

## Project structure

```
backend/
├── Controllers/
│   ├── AuthController.cs
│   ├── SpotifyController.cs
│   ├── TimelineController.cs
│   └── SummaryController.cs
├── Services/
│   ├── GoogleCalendarService.cs
│   ├── SpotifyService.cs
│   ├── DiscordService.cs
│   └── GitHubServices.cs
├── Models/
│   ├── User.cs
│   ├── TimelineEntry.cs
│   └── ApiConnection.cs
├── Data/
│   └── AppDbContext.cs
├── Migrations/
├── Program.cs           # Google login + per-provider OAuth callbacks & sync routes
└── appsettings.json     # config template (placeholders only — no real secrets)

frontend/
├── src/
│   ├── components/       # ApiTile, ProfileSidebar, TimelineFeed, TopBar, ...
│   ├── pages/            # LoginPage, Dashboard, TimelinePage, LandingPage
│   ├── auth/             # AuthContext / AuthProvider / ProtectedRoute
│   ├── App.jsx
│   └── main.jsx
```

---

## Configuration

OAuth client IDs and secrets are read from configuration and are **not** committed
to the repo — `appsettings.json` contains placeholders only. Supply your real
values locally via `appsettings.Development.json` (gitignored) or .NET user
secrets. Each provider needs a client ID, client secret, and redirect URI:

```json
"Spotify":          { "ClientId": "...", "ClientSecret": "...", "RedirectUri": "http://127.0.0.1:5184/api/spotify/callback" },
"Authentication":   { "Google": { "ClientId": "...", "ClientSecret": "..." } },
"GoogleCalendar":   { "ClientId": "...", "ClientSecret": "...", "RedirectUri": "http://127.0.0.1:5184/api/gcal/callback" },
"Discord":          { "ClientId": "...", "ClientSecret": "...", "RedirectUri": "http://127.0.0.1:5184/api/discord/callback" },
"GitHub":           { "AccessToken": "..." }
```

---

## Running locally

> Requires the .NET 8 SDK and Node.js (LTS).

**Backend**
```bash
cd backend
dotnet tool install --global dotnet-ef   # first time only
dotnet ef database update                # apply migrations
dotnet run                               # http://localhost:5184
```

**Frontend**
```bash
cd frontend
npm install
npm run dev -- --host 127.0.0.1          # http://127.0.0.1:5173
```

Swagger API docs: http://127.0.0.1:5184/swagger

---

## Key API endpoints

| Method | Endpoint             | Description             |
|--------|----------------------|-------------------------|
| GET    | `/api/timeline`      | Fetch all entries       |
| POST   | `/api/timeline`      | Create a timeline entry |
| GET    | `/api/timeline/{id}` | Get a specific entry    |
| DELETE | `/api/timeline/{id}` | Delete an entry         |

Each provider exposes a sync endpoint following the same pattern:
`/api/spotify/sync`, `/api/gcal/sync`, `/api/discord/sync`, `/api/github/sync`.