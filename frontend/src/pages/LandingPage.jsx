export default function LandingPage() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-gray-900 via-purple-900 to-gray-950 text-white">
      <div className="max-w-md w-full bg-black/40 border border-purple-700/40 rounded-2xl p-8 shadow-xl">
        <h1 className="text-2xl font-bold mb-3 text-center">
          Personal Timeline
        </h1>
        <p className="text-sm text-gray-300 mb-6 text-center">
          Connect your apps and see your Spotify, GitHub, Calendar, and Discord
          activity in one place.
        </p>

        <button
          onClick={() => {
            window.location.href = "http://127.0.0.1:5184/auth/google/login";
          }}
          className="w-full bg-purple-600 hover:bg-purple-500 transition px-4 py-2 rounded-lg font-medium text-sm"
        >
          Sign in with Google
        </button>
        <p className="text-xs text-gray-400 mt-4 text-center">
          You'll be redirected to your dashboard after login.
        </p>
      </div>
    </div>
  );
}
