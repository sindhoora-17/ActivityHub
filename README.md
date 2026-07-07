# Personal Timeline Application

## Overview
The **Personal Timeline App** is a full-stack journaling application that automatically builds a timeline of a user’s digital life by integrating with multiple third-party APIs. Users can also manually add personal events.

This project demonstrates:

- Full-stack development with **React + .NET 8**
- OAuth authentication **(Google)**
- Integration with **Google Calendar, Spotify, Discord, GitHub**
- Persistent storage via **Entity Framework Core + SQLite**
- Protected API routes & secure sessions
- A modern UI with filters, sync buttons, and timeline cards

---

## Tech Stack

### Frontend
- React - JavaScript
- React Router
- Tailwind CSS

### Backend
- .NET 8 Web API
- Entity Framework Core (SQLite)
- Google OAuth
- HttpClient for API calls
- Swagger documentation

### Database
- SQLite
- EF Core Code-First Migrations

---

## Authentication

The app uses **Google OAuth** for secure login.

### Authentication Flow
1. User clicks "Login with Google"
2. Redirect to Google consent screen
3. After login, backend validates the Google token
4. User info is stored in the SQLite database
5. A secure cookie manages session-based authentication
6. Only authenticated users can access protected routes

Protected routes include:
- `/dashboard`
- `/timeline`
- `/api/timeline/*`
- `/api/summary/*`

---

## Features

### 1. Dashboard Summary Cards
Displays the most recent activity from synced APIs:

| API | Summary |
|-----|---------|
| Google Calendar | Next event shown in UTC |
| Spotify | Last played track + artist |
| Discord | Total servers joined + last active server |
| GitHub | Last repo activity |

Each tile includes a Sync button that queries the backend for updates.

---

### 2. Timeline Page
A card-based timeline UI featuring:

- Icons representing Google, Spotify, Discord, GitHub  
- Filter bar (All, GoogleCalendar, Spotify, Discord, GitHub)  
- Delete button for each entry  
- Responsive layout and animations  

---

### 3. Add Entry Modal
Users can manually add:

- Title  
- Description  
- Date  
- Source API   

Saved instantly into the SQLite database.

---

### 4. Third-Party API Integrations

This project integrates four APIs:

#### Google Calendar
- Fetches upcoming events  
- Handles all-day events   
- Saves new entries on sync  

#### Spotify
- Fetches most recently played track  
- Parses track & artist  

#### Discord
- Fetches joined servers  
- Shows recently active server

#### GitHub
- Fetches push events, stars, repo creation  
- Saves summary entries  
---

## Project Structure

### Backend
```
backend/
├── Controllers/
│ ├── AuthController.cs
│ ├── GoogleOAuthController.cs
│ ├── TimelineController.cs
│ ├── SummaryController.cs
├── Services/
│ ├── GoogleCalendarService.cs
│ ├── SpotifyService.cs
│ ├── DiscordService.cs
│ ├── GithubService.cs
├── Models/
│ ├── User.cs
│ ├── TimelineEntry.cs
│ ├── ApiConnection.cs
├── Data/
│ ├── AppDbContext.cs
├── Migrations/
├── Program.cs
├── appsettings.json
```

### Frontend
```
frontend/
├── components/
│ ├── ApiTile.jsx
│ ├── ProfileSidebar.jsx
│ ├── TimelineFeed.jsx
│ ├── TopBar.jsx
├── pages/
│ ├── LoginPage.jsx
│ ├── DashboardPage.jsx
│ ├── TimelinePage.jsx
├── App.jsx
├── index.jsx
```
---
## Installation Requirements

Before running this project, install all required dependencies.  
Assume the machine has **nothing installed**.

---

## Backend Requirements (C# / .NET 8)

### 1. Install .NET 8 SDK  
Download & install from:  
https://dotnet.microsoft.com/en-us/download/dotnet/8.0

Verify installation:
```
dotnet --version
```
---
### 2. Install Entity Framework Core Tools
These are required for migrations & SQLite database updates.
```
dotnet tool install --global dotnet-ef
```
Verify:
```
dotnet ef
```
---
### 3. Install SQLite
Mac:
```
brew install sqlite
```
Windows (using winget):
```
winget install SQLite
```
Check:
```
sqlite3 --version
```
---

### Required .NET Dependencies
(Automatically restored when running the backend)
```
Microsoft.EntityFrameworkCore.Sqlite
Microsoft.EntityFrameworkCore.Design
Microsoft.EntityFrameworkCore.Tools
Microsoft.AspNetCore.Authentication.Google
Microsoft.AspNetCore.Authentication.Cookies
```

## Frontend Requirements (React + Tailwind)
### 1. Install Node.js (LTS)
Download from:
https://nodejs.org/en/download/
Verify:
```
node -v
npm -v
```
---
### 2. Navigate to frontend folder
```
cd frontend
```
---
### 3. Install npm packages
```
npm install
```
---
### 4. Install Tailwind CSS (Required)
Run:
```
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init -p
```
Ensure ```index.css``` contains:
```
@tailwind base;
@tailwind components;
@tailwind utilities;
```
---
## Setup Instructions

## Backend Setup

### 1. Navigate to backend
```bash
cd backend
```
### 2. Configure OAuth in appsettings.json
```json
"GoogleOAuth": {
  "ClientId": "YOUR_GOOGLE_CLIENT_ID",
  "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET",
}
```
### 3. Apply database migrations
```bash
dotnet ef database update
```
### 4. Run Backend
```bash
dotnet run
```
Backend URL: http://localhost:5184

---
## Frontend Setup

### 1. Navigate to frontend
```bash
cd frontend
```
### 2. Install dependencies
```bash
npm install
```
### 3. Start the development server
```bash
npm run dev -- --host 127.0.0.1
```
Frontend URL:  http://127.0.0.1:5173/

---
## Swagger API Documentation

Available at: http://127.0.0.1:5184/swagger

Includes:

- `/auth/google/login`
    
- `/auth/google/callback`
    
- `/api/timeline`
    
- `/api/summary/\*`
    
- `/api/googlecalendar/sync`
    
- `/api/spotify/sync`
    
- `/api/discord/sync`
    
- `/api/github/sync`

---
## Key API Endpoints

### Timeline Endpoints
| Method| Endpoint          | Description        |
|------------|-------------------------|-------------------------|
| GET        | `/api/timeline`         | Fetch all entries       |
| POST       | `/api/timeline`         | Create a timeline entry |
| GET        | `/api/timeline/{id}`    | Get a specific entry    |
| DELETE     | `/api/timeline/{id}`    | Delete an entry         |

---

### Google Calendar Endpoints
| Method| Endpoint                  | Description        |
|------------|--------------------------------|------------------------|
| GET        | `/api/googlecalendar/sync`     | Sync calendar events   |

> _Other third-party APIs (Spotify, Discord, GitHub) follow the same `/sync` pattern._

---
## Testing

### Using SQLite
```bash
sqlite3 personal_timeline.db
.tables
SELECT * FROM TimelineEntries;
```
### Using Swagger
Open in browser:
```bash
http://127.0.0.1:5184/swagger
```