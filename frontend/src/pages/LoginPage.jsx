export default function LoginPage() {
  const backendUrl = "http://127.0.0.1:5184";

  const login = () => {
    window.location.href = `${backendUrl}/auth/google/login`;
  };

  return (
    <div className="h-screen flex items-center justify-center bg-gray-900 text-white">
      <div className="text-center">
        <h1 className="text-4xl font-bold mb-6">
          Welcome to Your Timeline App
        </h1>
        <button
          onClick={login}
          className="bg-purple-600 hover:bg-purple-500 px-6 py-3 rounded-lg text-lg"
        >
          Sign in with Google
        </button>
      </div>
    </div>
  );
}
