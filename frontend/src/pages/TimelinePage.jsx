import { useEffect } from "react";
import { useNavigate } from "react-router-dom";
import ProfileSidebar from "../components/ProfileSidebar";
import TopBar from "../components/TopBar";
import TimelineFeed from "../components/TimelineFeed";

export default function TimelinePage() {
  const navigate = useNavigate();

  useEffect(() => {
    const checkAuth = async () => {
      try {
        const res = await fetch("http://127.0.0.1:5184/auth/me", {
          credentials: "include",
        });

        if (res.status === 401) {
          navigate("/login");
        }
      } catch (e) {
        console.error("Auth check failed:", e);
      }
    };

    checkAuth();
  }, [navigate]);

  return (
    <div className="flex flex-col min-h-screen bg-gradient-to-r from-gray-900 via-purple-900 to-gray-900 text-white">
      <TopBar />
      <div className="flex flex-1">
        <ProfileSidebar />
        <div className="w-2/3 p-6 overflow-y-auto">
          <h1 className="text-3xl font-bold mb-6">Timeline</h1>
          <TimelineFeed />
        </div>
      </div>
    </div>
  );
}
