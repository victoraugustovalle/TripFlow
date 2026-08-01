import { BrowserRouter, Navigate, Route, Routes } from "react-router-dom";
import { AuthBootstrap } from "./auth/AuthBootstrap";
import { ConfirmEmailPage } from "./auth/ConfirmEmailPage";
import { LoginPage } from "./auth/LoginPage";
import { RegisterPage } from "./auth/RegisterPage";
import { TwoFactorVerifyPage } from "./auth/TwoFactorVerifyPage";
import { Toaster } from "./components/Toaster";
import { AppLayout } from "./layouts/AppLayout";
import { ProtectedRoute } from "./layouts/ProtectedRoute";
import { ProfilePage } from "./profile/ProfilePage";
import { NewTripPage } from "./trips/NewTripPage";
import { TripDetailPage } from "./trips/TripDetailPage";
import { TripsListPage } from "./trips/TripsListPage";

export function App() {
  return (
    <BrowserRouter>
      <Toaster />
      <AuthBootstrap>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/confirm-email" element={<ConfirmEmailPage />} />
          <Route path="/2fa/verify" element={<TwoFactorVerifyPage />} />

          <Route element={<ProtectedRoute />}>
            <Route element={<AppLayout />}>
              <Route path="/" element={<TripsListPage />} />
              <Route path="/trips/new" element={<NewTripPage />} />
              <Route path="/trips/:tripId" element={<TripDetailPage />} />
              <Route path="/profile" element={<ProfilePage />} />
            </Route>
          </Route>

          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </AuthBootstrap>
    </BrowserRouter>
  );
}
