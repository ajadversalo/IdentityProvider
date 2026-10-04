import { useAuth } from "react-oidc-context";
import { Navigate } from "react-router-dom";

export function CallbackPage() {
  const auth = useAuth();

  if (auth.error) {
    return (
      <section className="card">
        <h1>Sign-in failed</h1>
        <p className="error">{auth.error.message}</p>
      </section>
    );
  }

  if (auth.isAuthenticated) {
    return <Navigate to="/dashboard" replace />;
  }

  return (
    <section className="card">
      <h1>Completing sign-in</h1>
      <p className="hint">Exchanging the authorization code for tokens…</p>
    </section>
  );
}
