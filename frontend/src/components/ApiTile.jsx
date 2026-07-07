export default function ApiTile({
  icon,
  title,
  subtitle,
  lines,
  accent,
  onSync,
}) {
  return (
    <div className="bg-gray-900/60 border border-gray-800 p-5 rounded-2xl shadow-md flex flex-col justify-between hover:border-purple-500/40 transition">
      <div>
        <div className="flex items-center gap-3 mb-3">
          <div className="text-3xl">{icon}</div>
          <div>
            <h3 className="text-lg font-semibold">{title}</h3>
            {subtitle && (
              <p className="text-sm text-gray-400 mt-0.5">{subtitle}</p>
            )}
          </div>
        </div>

        <div className="space-y-1 mb-4">
          {lines.map((line, idx) => (
            <p key={idx} className="text-sm text-gray-300">
              {line}
            </p>
          ))}
        </div>
      </div>

      <button
        onClick={onSync}
        className={`${accent} mt-2 px-4 py-2 rounded-md text-sm font-medium text-white hover:opacity-90 transition`}
      >
        Sync
      </button>
    </div>
  );
}
