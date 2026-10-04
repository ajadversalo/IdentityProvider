import { Link, Navigate, Route, Routes } from "react-router-dom";
import { useAuth } from "react-oidc-context";
import { HomePage } from "./pages/HomePage";
import { LoginPage } from "./pages/LoginPage";
import { RegisterPage } from "./pages/RegisterPage";
import { ForgotPasswordPage } from "./pages/ForgotPasswordPage";
import { ResetPasswordPage } from "./pages/ResetPasswordPage";
import { ConfirmEmailPage } from "./pages/ConfirmEmailPage";
import { DashboardPage } from "./pages/DashboardPage";
import { CallbackPage } from "./pages/CallbackPage";

export function App() {
  const auth = useAuth();

  return (
    <div className="shell">
      {auth.error ? (
        <p className="error">Sign-in service error: {auth.error.message}</p>
      ) : null}
      <header className="nav">
        <Link className="brand" to="/">
          <small>OpenID Connect</small>
          <strong>Identity Provider</strong>
        </Link>
        <nav className="nav-links">
          {auth.isAuthenticated ? (
            <>
              <Link to="/dashboard">Dashboard</Link>
              <button className="btn secondary" type="button" onClick={() => void auth.signoutRedirect()}>
                Sign out
              </button>
            </>
          ) : (
            <>
              <Link to="/login">Sign in</Link>
              <Link to="/register">Register</Link>
            </>
          )}
        </nav>
      </header>
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/reset-password" element={<ResetPasswordPage />} />
        <Route path="/confirm-email" element={<ConfirmEmailPage />} />
        <Route path="/callback" element={<CallbackPage />} />
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </div>
  );
}
