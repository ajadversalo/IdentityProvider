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
    <section>
      {error ? <p className="error banner">{error}</p> : null}
      {me ? (
        <>
          <div className="stats">
            <article className="stat stat-mint">
              <p className="eyebrow">Account</p>
              <p className="stat-value">{me.displayName || "Signed in"}</p>
              <p className="stat-note">Profile from this identity provider</p>
            </article>
            <article className="stat stat-cyan">
              <p className="eyebrow">Email</p>
              <p className="stat-value">{me.email}</p>
              <p className="stat-note">{me.emailConfirmed ? "Email confirmed" : "Email not confirmed"}</p>
            </article>
            <article className="stat stat-violet">
              <p className="eyebrow">Roles</p>
              <p className="stat-value">{me.roles.join(", ") || "None"}</p>
              <p className="stat-note">Created {new Date(me.createdUtc).toLocaleString()}</p>
            </article>
          </div>
          <p className="hint">Account id {me.id}</p>
        </>
      ) : (
        <section className="card">
          <h1>Signed in</h1>
          <p className="hint">Loading profile…</p>
        </section>
      )}
    </section>
  );
}
