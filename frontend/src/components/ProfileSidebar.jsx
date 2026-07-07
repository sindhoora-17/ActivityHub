import { Link, useLocation } from "react-router-dom";

export default function ProfileSidebar() {
  const user = {
    name: "Naga Sai Lakshmi Sindhoora",
    email: "nrajasek@depaul.edu",
    program: "MS in Computer Science",
    university: "DePaul University",
    skills: ["Python", "ML", "SQL", "React"],
  };

  const location = useLocation();

  const linkClasses = (path) =>
    `block px-3 py-2 rounded-md text-sm font-medium ${
      location.pathname === path
        ? "bg-purple-700 text-white"
        : "text-gray-300 hover:bg-purple-600/30 hover:text-white"
    }`;

  return (
    <div className="w-1/3 bg-gray-900/70 p-6 text-white border-r border-gray-800 min-h-screen flex flex-col">
      {/* Profile information */}
      <div className="flex flex-col items-center text-center mb-6">
        <img
          src="/placeholder.png"
          alt="Profile"
          className="w-24 h-24 rounded-full mb-3 border-2 border-purple-500"
        />
        <h2 className="text-xl font-bold">{user.name}</h2>
        <p className="text-sm text-gray-400">{user.email}</p>
        <p className="text-sm mt-1">{user.program}</p>
      </div>

      {/* Navigation */}
      <nav className="space-y-2 mb-8">
        <Link to="/" className={linkClasses("/")}>
          Dashboard
        </Link>

        <Link to="/timeline" className={linkClasses("/timeline")}>
          Timeline
        </Link>
      </nav>

      {/* skills */}
      <h3 className="font-semibold mb-2">Skills</h3>
      <div className="flex flex-wrap gap-2 mb-6">
        {user.skills.map((s) => (
          <span
            key={s}
            className="bg-purple-700/40 px-2 py-1 text-xs rounded-md"
          >
            {s}
          </span>
        ))}
      </div>

      {/* connected APIs */}
      <h3 className="font-semibold mb-2">Connected APIs</h3>
      <ul className="text-sm mb-6 space-y-1">
        <li>✅ Spotify</li>
        <li>✅ Discord</li>
        <li>✅ Google Calendar</li>
        <li>✅ GitHub</li>
      </ul>
    </div>
  );
}
