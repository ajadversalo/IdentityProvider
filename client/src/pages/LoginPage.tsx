import { FormEvent, useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useAuth } from "react-oidc-context";
import { api } from "../api";

type StatusResponse = { isAuthenticated: boolean };

export function LoginPage() {
  const auth = useAuth();
  const [params] = useSearchParams();
  const returnUrl = params.get("ReturnUrl");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [rememberMe, setRememberMe] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  useEffect(() => {
    void api<StatusResponse>("/api/account/status").then((status) => {
      if (status.isAuthenticated && returnUrl) {
        window.location.assign(returnUrl);
      }
    });
  }, [returnUrl]);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setPending(true);
    try {
      await api("/api/account/login", {
        method: "POST",
        body: JSON.stringify({ email, password, rememberMe })
      });
      if (returnUrl) {
        window.location.assign(returnUrl);
      } else {
        await auth.signinRedirect();
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Sign-in failed.");
    } finally {
      setPending(false);
    }
  }

  return (
    <section className="card">
      <h1>Sign in</h1>
      <p className="hint">Use the email and password for this identity provider.</p>
      <form onSubmit={onSubmit}>
        <label>
          Email
          <input
            type="email"
            autoComplete="username"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
        </label>
        <label>
          Password
          <input
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </label>
        <label className="row">
          <span>Remember me</span>
          <input
            type="checkbox"
            checked={rememberMe}
            onChange={(e) => setRememberMe(e.target.checked)}
          />
        </label>
        {error ? <p className="error">{error}</p> : null}
        <button className="btn" type="submit" disabled={pending}>
          {pending ? "Signing in…" : "Sign in"}
        </button>
      </form>
      <p className="hint">
        <Link to="/forgot-password">Forgot password</Link>
        {" · "}
        <Link to="/register">Create account</Link>
      </p>
    </section>
  );
}
