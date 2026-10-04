import { FormEvent, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api";

type RegisterResponse = { message: string; confirmationUrl?: string | null };

export function RegisterPage() {
  const [email, setEmail] = useState("");
  const [displayName, setDisplayName] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<RegisterResponse | null>(null);
  const [pending, setPending] = useState(false);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setPending(true);
    try {
      const response = await api<RegisterResponse>("/api/account/register", {
        method: "POST",
        body: JSON.stringify({ email, password, displayName })
      });
      setResult(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Registration failed.");
    } finally {
      setPending(false);
    }
  }

  if (result) {
    return (
      <section className="card">
        <h1>Check your email</h1>
        <p className="ok">{result.message}</p>
        {result.confirmationUrl ? (
          <p className="hint">
            Development shortcut:{" "}
            <a href={result.confirmationUrl}>confirm email now</a>
          </p>
        ) : null}
        <p>
          <Link to="/login">Back to sign in</Link>
        </p>
      </section>
    );
  }

  return (
    <section className="card">
      <h1>Create account</h1>
      <form onSubmit={onSubmit}>
        <label>
          Display name
          <input value={displayName} onChange={(e) => setDisplayName(e.target.value)} />
        </label>
        <label>
          Email
          <input
            type="email"
            autoComplete="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
        </label>
        <label>
          Password
          <input
            type="password"
            autoComplete="new-password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
            minLength={8}
          />
        </label>
        <p className="hint">At least 8 characters, with upper, lower, and a digit.</p>
        {error ? <p className="error">{error}</p> : null}
        <button className="btn" type="submit" disabled={pending}>
          {pending ? "Creating…" : "Create account"}
        </button>
      </form>
      <p className="hint">
        Already have an account? <Link to="/login">Sign in</Link>
      </p>
    </section>
  );
}
