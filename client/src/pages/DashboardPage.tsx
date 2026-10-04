import { useEffect, useState } from "react";
import { useAuth } from "react-oidc-context";

type MeResponse = {
  id: string;
  email: string;
  displayName: string;
  emailConfirmed: boolean;
  roles: string[];
  createdUtc: string;
};

export function DashboardPage() {
  const auth = useAuth();
  const [me, setMe] = useState<MeResponse | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!auth.isAuthenticated || !auth.user?.access_token) {
      return;
    }

    void fetch("/api/me", {
      headers: { Authorization: `Bearer ${auth.user.access_token}` }
    })
      .then(async (response) => {
        if (!response.ok) {
          throw new Error("Could not load profile.");
        }
        setMe((await response.json()) as MeResponse);
      })
      .catch((err: unknown) => setError(err instanceof Error ? err.message : "Could not load profile."));
  }, [auth.isAuthenticated, auth.user?.access_token]);

  if (!auth.isAuthenticated) {
    return (
      <section className="card">
        <h1>Dashboard</h1>
        <p className="hint">You need an access token to view this page.</p>
        <button className="btn" type="button" onClick={() => void auth.signinRedirect()}>
          Sign in
        </button>
      </section>
    );
  }

  return (
    <section className="card" style={{ maxWidth: 640 }}>
      <h1>Signed in</h1>
      <p className="hint">This page calls /api/me with the access token issued by this provider.</p>
      {error ? <p className="error">{error}</p> : null}
      {me ? (
        <div className="meta">
          <div>id: {me.id}</div>
          <div>email: {me.email}</div>
          <div>name: {me.displayName}</div>
          <div>roles: {me.roles.join(", ") || "none"}</div>
          <div>created: {new Date(me.createdUtc).toLocaleString()}</div>
        </div>
      ) : (
        <p className="hint">Loading profile…</p>
      )}
    </section>
  );
}
