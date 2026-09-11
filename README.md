# ActivityHub — Personal Activity Timeline

ActivityHub is a full-stack application that combines activity from Spotify, GitHub, Google Calendar, and Discord into one unified, filterable timeline. Users sign in with Google, connect external services, sync recent activity, and can also add timeline entries manually.

## Highlights

- **Multi-provider integration** — pulls activity from Spotify, Google Calendar, Discord, and GitHub into one shared timeline model.
- **Automatic token refresh** — Spotify, Google Calendar, and Discord access tokens are refreshed when needed using stored refresh tokens.
- **Normalized activity model** — provider-specific API responses are converted into a common `TimelineEntry` representation.
- **Deduplicated syncs** — provider event identifiers prevent repeated syncs from inserting duplicate timeline entries.
- **Authenticated full-stack flow** — Google login with an HttpOnly cookie-backed session protects dashboard and API routes.
- **Persistent local data** — Entity Framework Core with SQLite stores users, API connections, and timeline entries.

## Tech Stack

**Frontend:** React 19, React Router, Tailwind CSS, Vite  
**Backend:** .NET 9 Web API, Entity Framework Core, Swagger  
**Authentication:** Google OAuth login, provider authorization flows for Spotify / Google Calendar / Discord, cookie sessions  
**Database:** SQLite with EF Core migrations

## Architecture

```text
                 ┌────────────────────┐
                 │      React UI      │
                 │ dashboard/timeline │
                 └─────────┬──────────┘
                           │ credentials: include
                           ▼
                 ┌────────────────────┐
                 │    .NET 9 API      │
                 │ auth + sync routes │
                 └──────┬─────┬───────┘
                        │     │
            ┌───────────┘     └────────────┐
            ▼                              ▼
  ┌──────────────────┐          ┌──────────────────┐
  │ Provider APIs    │          │ EF Core + SQLite │
  │ Spotify / GCal   │          │ users, tokens,   │
  │ Discord / GitHub │          │ timeline entries │
  └──────────────────┘          └──────────────────┘
```

Each provider service fetches external activity, maps it into `TimelineEntry`, checks whether the provider event has already been persisted, and saves only new entries.

## Authentication and provider connections

Application login uses Google OAuth through ASP.NET Core authentication. After login, the backend creates an HttpOnly cookie session and associates the Google identity with a local user record.

Spotify, Google Calendar, and Discord use authorization-code flows. Their access tokens, refresh tokens, and expiration times are stored per user so the backend can refresh expired access tokens before sync.

GitHub currently uses a user-supplied personal access token plus a GitHub username rather than a GitHub OAuth flow.

## Features

### Dashboard
Shows the latest activity from connected services with provider-specific sync controls.

### Unified timeline
Displays activity cards from multiple providers with source filtering and entry deletion.

### Manual entries
Users can create their own timeline events with a title, description, date, and source.

### Provider sync
- **Google Calendar** — imports upcoming calendar events, including all-day events.
- **Spotify** — imports recently played tracks and artist information.
- **Discord** — imports joined-server activity.
- **GitHub** — imports supported public activity events such as pushes, repository creation, stars, pull requests, and issues.

## Project structure

```text
backend/
├── Controllers/       API controllers
├── Services/          provider integration and sync logic
├── Models/            User, TimelineEntry, ApiConnection
├── Data/              EF Core DbContext
├── Migrations/        database schema history
├── Program.cs         authentication, provider callbacks, sync endpoints
└── appsettings.json   placeholder configuration only

frontend/
├── src/components/    timeline/dashboard UI
├── src/pages/         Login, Dashboard, Timeline, Landing
├── src/auth/          auth context and protected routes
├── src/App.jsx
└── src/main.jsx
```

## Configuration

No real OAuth client secrets should be committed. `backend/appsettings.json` contains placeholders only. Supply local credentials through `appsettings.Development.json` (gitignored), environment variables, or .NET user secrets.

Example configuration keys:

```json
"Spotify": {
  "ClientId": "...",
  "ClientSecret": "...",
  "RedirectUri": "http://127.0.0.1:5184/api/spotify/callback"
},
"Authentication": {
  "Google": {
    "ClientId": "...",
    "ClientSecret": "..."
  }
},
"GoogleCalendar": {
  "ClientId": "...",
  "ClientSecret": "...",
  "RedirectUri": "http://127.0.0.1:5184/api/gcal/callback"
},
"Discord": {
  "ClientId": "...",
  "ClientSecret": "...",
  "RedirectUri": "http://127.0.0.1:5184/api/discord/callback"
}
```

## Running locally

Requires the **.NET 9 SDK** and a current Node.js LTS release.

### Backend

```bash
cd backend
dotnet tool install --global dotnet-ef   # first time only
dotnet ef database update
dotnet run
```

Backend: `http://127.0.0.1:5184`  
Swagger: `http://127.0.0.1:5184/swagger`

### Frontend

```bash
cd frontend
npm install
npm run dev -- --host 127.0.0.1
```

Frontend: `http://127.0.0.1:5173`

## Key API endpoints

| Method | Endpoint | Description |
| --- | --- | --- |
| GET | `/api/timeline` | Fetch timeline entries |
| POST | `/api/timeline` | Create a timeline entry |
| GET | `/api/timeline/{id}` | Fetch one entry |
| DELETE | `/api/timeline/{id}` | Delete an entry |
| POST | `/api/spotify/sync` | Sync Spotify activity |
| POST | `/api/gcal/sync` | Sync Google Calendar activity |
| POST | `/api/discord/sync` | Sync Discord activity |
| POST | `/api/github/sync` | Sync GitHub activity |

## Security notes

This repository is a portfolio/demo application, not a production identity platform.

- OAuth client secrets are expected to live outside source control.
- Local SQLite database files are ignored and should never be committed because they can contain user data and provider tokens.
- Provider access and refresh tokens are currently stored in the local SQLite database; a production deployment should encrypt sensitive tokens at rest or use a dedicated secrets/token store.
- The custom Spotify, Google Calendar, and Discord authorization flows should add OAuth `state` validation before being exposed beyond local/demo use.

## CI

GitHub Actions verifies that the .NET backend builds and the React frontend installs and builds successfully on every push and pull request.
