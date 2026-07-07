import { useEffect, useState } from "react";
import ProfileSidebar from "../components/ProfileSidebar";
import TopBar from "../components/TopBar";
import { Link } from "react-router-dom";

export default function Dashboard() {
  const [user, setUser] = useState(null);
  const [connections, setConnections] = useState({
    spotify: false,
    github: false,
    discord: false,
    gcal: false,
  });

  const [summaries, setSummaries] = useState({
    spotify: {},
    github: {},
    discord: {},
    gcal: {},
  });

  const baseUrl = "http://127.0.0.1:5184";

  useEffect(() => {
    fetch(`${baseUrl}/auth/me`, { credentials: "include" })
      .then((r) => (r.ok ? r.json() : null))
      .then((data) => setUser(data))
      .catch(() => {});
  }, []);

  const loadConnections = async () => {
    try {
      const res = await fetch(`${baseUrl}/api/connections`, {
        credentials: "include",
      });
      if (!res.ok) return;

      const data = await res.json();
      setConnections({
        spotify: data.spotify,
        github: data.github,
        discord: data.discord,
        gcal: data.gcal,
      });
    } catch (err) {
      console.warn("Ignored error:", err);
    }
  };

  const loadSummaries = async () => {
    try {
      const [spotify, github, discord, gcal] = await Promise.all([
        fetch(`${baseUrl}/api/summary/spotify`, {
          credentials: "include",
        }).then((r) => (r.ok ? r.json() : {})),
        fetch(`${baseUrl}/api/summary/github`, { credentials: "include" }).then(
          (r) => (r.ok ? r.json() : {})
        ),
        fetch(`${baseUrl}/api/summary/discord`, {
          credentials: "include",
        }).then((r) => (r.ok ? r.json() : {})),
        fetch(`${baseUrl}/api/summary/gcal`, { credentials: "include" }).then(
          (r) => (r.ok ? r.json() : {})
        ),
      ]);

      setSummaries({
        spotify: spotify || {},
        github: github || {},
        discord: discord || {},
        gcal: gcal || {},
      });
    } catch (e) {
      console.error("Summary load failed:", e);
    }
  };

  useEffect(() => {
    loadConnections();
    loadSummaries();
  }, []);

  const syncApi = async (endpoint) => {
    try {
      const res = await fetch(`${baseUrl}${endpoint}`, {
        method: "POST",
        credentials: "include",
      });

      const json = res.ok ? await res.json() : null;
      if (json?.redirect) {
        window.location.href = json.redirect;
        return;
      }

      await loadSummaries();
      alert("Synced successfully!");
    } catch {
      alert("Sync failed.");
    }
  };
  return (
    <div className="flex flex-col min-h-screen bg-gradient-to-r from-gray-900 via-purple-900 to-gray-900 text-white">
      <TopBar />

      <div className="flex flex-1">
        <ProfileSidebar />

        <div className="flex-1 p-8 overflow-y-auto">
          <div className="mb-10">
            <h1 className="text-4xl font-bold">
              Hi, {user?.displayName?.split(" ")[0] || "there"} 👋
            </h1>
            <p className="text-gray-300 mt-2">
              Here's a quick look at your recent activity.
            </p>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
            <DashboardTile
              title="Spotify"
              connected={connections.spotify}
              summary={
                summaries.spotify?.lastPlayedTrack
                  ? `${summaries.spotify.lastPlayedTrack} — ${summaries.spotify.artist}`
                  : "No recent listening activity"
              }
              icon="🎧"
              gradient="from-[#1DB954]/20 to-[#1DB954]/10"
              onSync={() => syncApi("/api/spotify/sync")}
            />

            <DashboardTile
              title="GitHub"
              connected={connections.github}
              summary={
                summaries.github?.lastActivity &&
                summaries.github.lastActivity !== "—"
                  ? `${summaries.github.lastActivity} (${summaries.github.repo})`
                  : "No recent GitHub activity"
              }
              icon="🐙"
              gradient="from-gray-700/30 to-gray-800/20"
              onSync={() => syncApi("/api/github/sync")}
            />

            <DashboardTile
              title="Google Calendar"
              connected={connections.gcal}
              summary={
                summaries.gcal?.nextEvent
                  ? `${summaries.gcal.nextEvent} — ${summaries.gcal.date}`
                  : "No upcoming events"
              }
              icon="📅"
              gradient="from-[#4285F4]/20 to-[#4285F4]/10"
              onSync={() => syncApi("/api/gcal/sync")}
            />

            <DashboardTile
              title="Discord"
              connected={connections.discord}
              summary={
                summaries.discord?.servers
                  ? `${summaries.discord.servers} servers — Last: ${summaries.discord.lastServer}`
                  : "No Discord activity"
              }
              icon="💬"
              gradient="from-[#5865F2]/20 to-[#5865F2]/10"
              onSync={() => syncApi("/api/discord/sync")}
            />
          </div>

          <div className="mt-12">
            <Link
              to="/timeline"
              className="bg-purple-600 hover:bg-purple-500 px-6 py-3 rounded-lg text-lg transition font-medium"
            >
              View Full Timeline →
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
}

function DashboardTile({ title, summary, icon, gradient, onSync, connected }) {
  return (
    <div
      className={`rounded-xl bg-gradient-to-br ${gradient} p-6 shadow-md border border-white/10`}
    >
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-xl font-bold flex items-center gap-2">
          {title}
          <span
            className={`w-3 h-3 rounded-full ${
              connected ? "bg-green-400" : "bg-red-400"
            }`}
          ></span>
        </h2>
        <span className="text-3xl">{icon}</span>
      </div>

      <p className="text-gray-300 text-sm leading-snug mb-4 h-14 overflow-hidden">
        {summary}
      </p>

      <button
        onClick={onSync}
        className="bg-purple-600 hover:bg-purple-500 px-4 py-2 rounded-md text-sm transition"
      >
        Sync Now
      </button>
    </div>
  );
}
