import React from "react";
import { format } from "date-fns";

const apiStyles: Record<string, string> = {
  GoogleCalendar: "from-blue-500/20 to-purple-500/20 border-blue-400/30",
  Spotify: "from-green-500/20 to-emerald-500/20 border-green-400/30",
  Discord: "from-indigo-500/20 to-purple-500/20 border-indigo-400/30",
  GitHub: "from-gray-700/40 to-gray-900/40 border-gray-400/30",
};

const defaultStyle = "from-purple-500/20 to-pink-500/20 border-purple-400/30";

interface TimelineCardProps {
  title: string;
  description?: string;
  eventDate: string;
  sourceApi: string;
  imageUrl?: string;
  externalUrl?: string;
}

export default function TimelineCard({
  title,
  description,
  eventDate,
  sourceApi,
  imageUrl,
  externalUrl,
}: TimelineCardProps) {

  const style = apiStyles[sourceApi] || defaultStyle;

  return (
    <div
      className={`w-full rounded-2xl p-5 mb-6 bg-gradient-to-r border ${style} shadow-lg hover:shadow-2xl transition-all duration-200`}
    >
      <p className="text-sm text-gray-300 mb-1">
        {format(new Date(eventDate), "MMM dd, yyyy")}
      </p>

      <h3 className="text-lg font-semibold text-white mb-2">{title}</h3>

      {description && description.trim() !== "" && (
        <p className="text-gray-300 mb-3">{description}</p>
      )}

      {imageUrl && imageUrl !== "" && (
        <div className="mb-3">
          <img
            src={imageUrl}
            alt="event"
            className="rounded-xl max-h-52 object-cover"
          />
        </div>
      )}

      <div className="inline-block px-3 py-1 rounded-full text-xs font-medium bg-black/30 border border-white/20 text-white uppercase tracking-wide">
        {sourceApi}
      </div>

      {externalUrl && externalUrl !== "" && (
        <div className="mt-3">
          <a
            href={externalUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="text-blue-300 underline text-sm"
          >
            View on {sourceApi}
          </a>
        </div>
      )}
    </div>
  );
}
