import { FormEvent, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { api } from "../api";

export function ResetPasswordPage() {
  const [params] = useSearchParams();
  const userId = params.get("userId") ?? "";
  const token = params.get("token") ?? "";
  const [password, setPassword] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setPending(true);
    try {
      const result = await api<{ message: string }>("/api/account/reset-password", {
        method: "POST",
        body: JSON.stringify({ userId, token, password })
      });
      setMessage(result.message);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Reset failed.");
    } finally {
      setPending(false);
    }
  }

  if (!userId || !token) {
    return (
      <section className="card">
        <h1>Reset password</h1>
        <p className="error">This reset link is missing required values.</p>
      </section>
    );
  }

  return (
    <section className="card">
      <h1>Choose a new password</h1>
      {message ? (
        <>
          <p className="ok">{message}</p>
          <Link to="/login">Sign in</Link>
        </>
      ) : (
        <form onSubmit={onSubmit}>
          <label>
            New password
            <input
              type="password"
              autoComplete="new-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              required
              minLength={8}
            />
          </label>
          {error ? <p className="error">{error}</p> : null}
          <button className="btn" type="submit" disabled={pending}>
            {pending ? "Updating…" : "Update password"}
          </button>
        </form>
      )}
    </section>
  );
}
