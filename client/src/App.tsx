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

function helloName(profile: { name?: string; preferred_username?: string; email?: string } | undefined) {
  const raw = profile?.name || profile?.preferred_username || profile?.email;
  if (!raw) {
    return null;
  }

  return raw.split("@")[0];
}

export function App() {
  const auth = useAuth();
  const hello = helloName(auth.user?.profile);

  return (
    <div className="shell">
      {auth.error ? (
        <p className="error banner">Sign-in service error: {auth.error.message}</p>
      ) : null}
      <header className="nav">
        <Link className="brand" to="/">
          <span className="mark" aria-hidden="true">
            <svg viewBox="0 0 24 24">
              <path
                fill="currentColor"
                d="M12 1.4c.28 0 .52.18.6.45l1.86 6.09 6.09 1.86c.27.08.45.32.45.6s-.18.52-.45.6l-6.09 1.86-1.86 6.09c-.08.27-.32.45-.6.45s-.52-.18-.6-.45l-1.86-6.09-6.09-1.86a.64.64 0 0 1-.45-.6c0-.28.18-.52.45-.6l6.09-1.86L11.4 1.85c.08-.27.32-.45.6-.45Z"
              />
            </svg>
          </span>
          <span className="brand-copy">
            <strong>Northstar</strong>
            <small>Identity</small>
          </span>
        </Link>
        <nav className="nav-links">
          {auth.isAuthenticated ? (
            <>
              {hello ? <span className="hello">Hi {hello}!</span> : null}
              <Link className="btn secondary" to="/dashboard">
                Dashboard
              </Link>
              <button className="btn secondary" type="button" onClick={() => void auth.signoutRedirect()}>
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true">
                  <path d="M10 7V6a2 2 0 0 1 2-2h7a2 2 0 0 1 2 2v12a2 2 0 0 1-2 2h-7a2 2 0 0 1-2-2v-1" />
                  <path d="M15 12H3" />
                  <path d="M6 9l-3 3 3 3" />
                </svg>
                Sign out
              </button>
            </>
          ) : (
            <>
              <Link className="btn secondary" to="/login">
                Sign in
              </Link>
              <Link className="btn secondary" to="/register">
                Register
              </Link>
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
