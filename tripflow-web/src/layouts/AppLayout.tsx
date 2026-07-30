import { Link, Outlet, useNavigate } from "react-router-dom";
import * as authApi from "../api/auth";
import { useAuthStore } from "../auth/authStore";
import { Button } from "../components/Button";
import { FlightTrail } from "../components/FlightTrail";

export function AppLayout() {
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);
  const clearSession = useAuthStore((s) => s.clearSession);

  const handleLogout = async () => {
    try {
      await authApi.logout();
    } finally {
      clearSession();
      navigate("/login");
    }
  };

  return (
    <div className="min-h-screen bg-cream-200">
      <header className="relative overflow-hidden border-b border-cream-300 bg-white">
        <FlightTrail className="pointer-events-none absolute -right-4 -top-6 h-16 w-40 text-brand-600/10" />
        <div className="relative mx-auto flex max-w-5xl items-center justify-between px-4 py-3">
          <Link to="/" className="font-display text-xl font-extrabold text-brand-700">
            TripFlow
          </Link>
          <div className="flex items-center gap-4">
            <span className="text-sm text-navy-700">{user?.name}</span>
            <Button variant="ghost" onClick={handleLogout}>
              Sair
            </Button>
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-5xl px-4 py-8">
        <Outlet />
      </main>
    </div>
  );
}
