import { useState } from "react";
import { useAuth } from "../auth/useAuth";

export default function TopBar({ onSyncComplete }) {
  const [syncing, setSyncing] = useState(false);
  const [toast, setToast] = useState(null);

  const { setUser } = useAuth();

  const baseUrl = "http://127.0.0.1:5184/api";
  const syncAllApis = async () => {
    setSyncing(true);
    showToast("Syncing data from all APIs...", "info");

    try {
      const apis = [
        `${baseUrl}/discord/sync`,
        `${baseUrl}/spotify/sync`,
        `${baseUrl}/gcal/sync`,
        `${baseUrl}/github/sync`,
      ];

      const results = await Promise.allSettled(
        apis.map((url) =>
          fetch(url, {
            method: "POST",
            credentials: "include",
          }).then((res) => res.json())
        )
      );

      const summaries = results.map((r, i) =>
        r.status === "fulfilled"
          ? `✅ ${apis[i].split("/").slice(-2, -1)[0]} (${
              r.value.added || 0
            } new)`
          : `⚠️ ${apis[i].split("/").slice(-2, -1)[0]} failed`
      );

      showToast(summaries.join(" | "), "success");
      if (onSyncComplete) onSyncComplete();
    } catch (err) {
      console.error(err);
      showToast("❌ Sync failed. Please try again.", "error");
    } finally {
      setSyncing(false);
    }
  };

  const logout = async () => {
    try {
      await fetch("http://127.0.0.1:5184/auth/logout", {
        method: "POST",
        credentials: "include",
      });

      setUser(null);
      showToast("You've been logged out!", "info");
      setTimeout(() => {
        window.location.href = "/login";
      }, 800);
    } catch (e) {
      console.error("Logout error:", e);
      showToast("Logout failed.", "error");
    }
  };

  const showToast = (message, type = "info") => {
    setToast({ message, type });
    setTimeout(() => setToast(null), 4000);
  };

  const toastColors = {
    info: "bg-blue-600/90",
    success: "bg-green-600/90",
    error: "bg-red-600/90",
  };

  return (
    <>
      <div className="flex justify-between items-center px-6 py-3 border-b border-gray-800 bg-gray-900/60 backdrop-blur-md relative">
        <h1 className="text-lg font-semibold text-purple-300 tracking-tight">
          Personal Timeline
        </h1>

        <div className="flex items-center gap-4">
          <button
            onClick={syncAllApis}
            disabled={syncing}
            className={`${
              syncing ? "opacity-60 cursor-not-allowed" : "hover:bg-purple-500"
            } bg-purple-600 px-4 py-2 rounded-md text-sm font-medium transition`}
          >
            {syncing ? "🔄 Syncing..." : "🔁 Sync All"}
          </button>

          <button
            onClick={logout}
            className="text-gray-400 hover:text-white text-sm"
          >
            Logout
          </button>
        </div>
      </div>

      {toast && (
        <div
          className={`fixed bottom-5 left-1/2 transform -translate-x-1/2 px-5 py-3 rounded-lg shadow-lg text-sm text-white ${
            toastColors[toast.type]
          } transition-all duration-500 animate-fadeIn`}
        >
          {toast.message}
        </div>
      )}
    </>
  );
}
