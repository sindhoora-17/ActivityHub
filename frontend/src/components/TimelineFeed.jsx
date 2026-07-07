import { useEffect, useState } from "react";

const apiCardStyles = {
  googlecalendar: {
    bg: "bg-gradient-to-r from-sky-400/30 to-cyan-500/30",
    border: "border-cyan-300/60",
    tag: "text-cyan-300 border-cyan-300/50",
  },
  spotify: {
    bg: "bg-gradient-to-r from-green-500/25 to-emerald-600/25",
    border: "border-green-400/50",
    tag: "text-green-300 border-green-300/40",
  },
  discord: {
    bg: "bg-gradient-to-r from-blue-900/40 to-blue-700/40",
    border: "border-blue-400/50",
    tag: "text-blue-300 border-blue-300/40",
  },
  github: {
    bg: "bg-gradient-to-r from-gray-800/60 to-gray-900/60",
    border: "border-gray-400/40",
    tag: "text-gray-300 border-gray-300/40",
  },
  default: {
    bg: "bg-gradient-to-r from-zinc-700/40 to-zinc-800/40",
    border: "border-zinc-400/40",
    tag: "text-zinc-300 border-zinc-300/40",
  },
};

const getIcon = (source) => {
  if (!source) return "⭐";
  switch (source.toLowerCase()) {
    case "spotify":
      return "🎧";
    case "discord":
      return "💬";
    case "googlecalendar":
      return "📅";
    case "github":
      return "🐙";
    default:
      return "⭐";
  }
};

export default function TimelineFeed() {
  const [entries, setEntries] = useState([]);
  const [filter, setFilter] = useState("All");
  const [showModal, setShowModal] = useState(false);

  const [newTitle, setNewTitle] = useState("");
  const [newDesc, setNewDesc] = useState("");
  const [newDate, setNewDate] = useState("");
  const [newSource, setNewSource] = useState("GoogleCalendar");

  const [userId, setUserId] = useState(null);

  useEffect(() => {
    fetch("http://127.0.0.1:5184/auth/me", { credentials: "include" })
      .then((res) => res.json())
      .then((u) => setUserId(u.id))
      .catch(() => setUserId(null));
  }, []);

  useEffect(() => {
    fetch("http://127.0.0.1:5184/api/timeline", { credentials: "include" })
      .then((res) => res.json())
      .then((data) => setEntries(data))
      .catch(() => setEntries([]));
  }, []);

  const formatDate = (dateString) => {
    const date = new Date(dateString);
    return (
      date.toLocaleDateString("en-US", {
        month: "short",
        day: "numeric",
        year: "numeric",
        timeZone: "UTC",
      }) + " (UTC)"
    );
  };

  const getStyle = (source) => {
    const key = source?.toLowerCase();
    return apiCardStyles[key] || apiCardStyles.default;
  };

  const filteredEntries =
    filter === "All"
      ? entries
      : entries.filter(
          (e) => e.sourceApi.toLowerCase() === filter.toLowerCase()
        );

  const categories = [
    "All",
    ...Array.from(new Set(entries.map((e) => e.sourceApi))),
  ];

  const deleteEntry = async (id) => {
    const confirmDelete = window.confirm("Delete this entry?");
    if (!confirmDelete) return;

    const res = await fetch(`http://127.0.0.1:5184/api/timeline/${id}`, {
      method: "DELETE",
      credentials: "include",
    });

    if (res.ok) {
      setEntries((prev) => prev.filter((e) => e.id !== id));
    } else {
      alert("Failed to delete entry.");
    }
  };

  const saveEntry = async () => {
    if (!newTitle || !newDate || !newSource) {
      alert("Please fill in title, date, and source.");
      return;
    }

    const body = {
      userId: userId,
      title: newTitle,
      description: newDesc,
      eventDate: newDate,
      entryType: "Event",
      category: newSource,
      imageUrl: "",
      externalUrl: "",
      sourceApi: newSource,
      externalId: "",
      metadata: "",
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString(),
    };

    const res = await fetch("http://127.0.0.1:5184/api/timeline", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      credentials: "include",
      body: JSON.stringify(body),
    });

    if (res.ok) {
      setShowModal(false);
      setNewTitle("");
      setNewDesc("");
      setNewDate("");
      setNewSource("GoogleCalendar");

      const updated = await fetch("http://127.0.0.1:5184/api/timeline", {
        credentials: "include",
      }).then((r) => r.json());
      setEntries(updated);
    } else {
      alert("Failed to add entry.");
    }
  };

  return (
    <div className="max-w-3xl mx-auto pt-2 px-2">
      <h2 className="text-2xl font-semibold mb-6">Your Timeline</h2>

      <div className="flex items-center justify-between mb-8">
        <div className="flex flex-wrap gap-3">
          {categories.map((cat) => (
            <button
              key={cat}
              onClick={() => setFilter(cat)}
              className={`px-4 py-1.5 rounded-full border text-sm transition
                ${
                  filter === cat
                    ? "bg-white/20 border-white text-white"
                    : "bg-black/20 border-gray-500 text-gray-300 hover:bg-white/10"
                }`}
            >
              {cat === "googlecalendar" ? "Google Calendar" : cat}
            </button>
          ))}
        </div>

        <button
          onClick={() => setShowModal(true)}
          className="px-4 py-2 bg-purple-600 hover:bg-purple-500 rounded-lg text-white font-medium shadow-md"
        >
          + Add Entry
        </button>
      </div>

      {showModal && (
        <div className="fixed inset-0 bg-black/60 backdrop-blur-sm flex items-center justify-center z-50">
          <div className="bg-gray-900 p-6 rounded-2xl w-96 border border-white/10">
            <h3 className="text-xl font-semibold mb-4">Add New Entry</h3>

            <label className="text-sm">Title</label>
            <input
              className="w-full mt-1 mb-3 px-3 py-2 rounded bg-gray-800 border border-gray-700 text-white"
              value={newTitle}
              onChange={(e) => setNewTitle(e.target.value)}
            />

            <label className="text-sm">Description</label>
            <textarea
              className="w-full mt-1 mb-3 px-3 py-2 rounded bg-gray-800 border border-gray-700 text-white"
              rows={2}
              value={newDesc}
              onChange={(e) => setNewDesc(e.target.value)}
            />

            <label className="text-sm">Date</label>
            <input
              type="date"
              className="w-full mt-1 mb-3 px-3 py-2 rounded bg-gray-800 border border-gray-700 text-white"
              value={newDate}
              onChange={(e) => setNewDate(e.target.value)}
            />

            <label className="text-sm">Source</label>
            <select
              className="w-full mt-1 mb-6 px-3 py-2 rounded bg-gray-800 border border-gray-700 text-white"
              value={newSource}
              onChange={(e) => setNewSource(e.target.value)}
            >
              <option value="GoogleCalendar">Google Calendar</option>
              <option value="Spotify">Spotify</option>
              <option value="Discord">Discord</option>
              <option value="GitHub">GitHub</option>
              <option value="Manual">Manual</option>
            </select>

            <div className="flex justify-end gap-3">
              <button
                onClick={() => setShowModal(false)}
                className="px-4 py-2 bg-gray-700 hover:bg-gray-600 rounded-lg"
              >
                Cancel
              </button>
              <button
                onClick={saveEntry}
                className="px-4 py-2 bg-purple-600 hover:bg-purple-500 rounded-lg"
              >
                Save
              </button>
            </div>
          </div>
        </div>
      )}

      <div className="space-y-6">
        {filteredEntries.map((entry) => {
          const style = getStyle(entry.sourceApi);
          const icon = getIcon(entry.sourceApi);

          return (
            <div
              key={entry.id}
              className={`rounded-2xl shadow-lg p-5 border ${style.bg} ${style.border}
                hover:shadow-2xl transition-all duration-300 relative`}
            >
              <button
                onClick={() => deleteEntry(entry.id)}
                className="absolute top-3 right-3 text-red-400 hover:text-red-300 text-sm opacity-60 hover:opacity-100 transition"
              >
                🗑️
              </button>

              <p className="text-sm text-gray-300 mb-1">
                {formatDate(entry.eventDate)}
              </p>

              <div className="flex items-center gap-3 mb-2">
                <div className="w-9 h-9 rounded-full bg-black/30 border border-white/20 flex items-center justify-center text-lg">
                  {icon}
                </div>
                <h3 className="text-xl font-semibold text-white">
                  {entry.title}
                </h3>
              </div>

              {entry.description && (
                <p className="text-gray-300 mb-3">{entry.description}</p>
              )}

              {entry.imageUrl && entry.imageUrl !== "" && (
                <img
                  src={entry.imageUrl}
                  alt="event"
                  className="rounded-xl max-h-52 object-cover mb-3"
                />
              )}

              <span
                className={`inline-block text-xs font-semibold uppercase tracking-wide px-3 py-1 rounded-full border ${style.tag}`}
              >
                {entry.sourceApi}
              </span>
            </div>
          );
        })}
      </div>
    </div>
  );
}
