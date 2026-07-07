import { useState } from "react";

export default function SyncButton({ apiName, endpoint, onDone }) {
  const [status, setStatus] = useState("idle");

  async function handleSync() {
    setStatus("syncing");

    try {
      const res = await fetch(endpoint, {
        method: "POST",
        credentials: "include",
      });

      const data = await res.json();
      console.log(`${apiName} sync result:`, data);

      setStatus("done");

      if (onDone) onDone(); // refresh dashboard
      setTimeout(() => setStatus("idle"), 1500);
    } catch (err) {
      console.error(err);
      setStatus("error");
      setTimeout(() => setStatus("idle"), 1500);
    }
  }

  return (
    <button
      onClick={handleSync}
      className="mt-3 bg-purple-600 px-3 py-1 rounded-md text-sm hover:bg-purple-500 transition"
    >
      {status === "idle" && "Sync"}
      {status === "syncing" && "⏳ Syncing..."}
      {status === "done" && "✓ Synced!"}
      {status === "error" && "⚠️ Error"}
    </button>
  );
}
