import { FormEvent, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api";

type ForgotResponse = { message: string; resetUrl?: string | null };

export function ForgotPasswordPage() {
  const [email, setEmail] = useState("");
  const [result, setResult] = useState<ForgotResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  async function onSubmit(event: FormEvent) {
    event.preventDefault();
    setError(null);
    setPending(true);
    try {
      setResult(
        await api<ForgotResponse>("/api/account/forgot-password", {
          method: "POST",
          body: JSON.stringify({ email })
        })
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : "Request failed.");
    } finally {
      setPending(false);
    }
  }

  return (
    <section className="card">
      <h1>Forgot password</h1>
      {result ? (
        <>
          <p className="ok">{result.message}</p>
          {result.resetUrl ? (
            <p className="hint">
              Development shortcut: <a href={result.resetUrl}>reset password now</a>
            </p>
          ) : null}
        </>
      ) : (
        <form onSubmit={onSubmit}>
          <label>
            Email
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              required
            />
          </label>
          {error ? <p className="error">{error}</p> : null}
          <button className="btn" type="submit" disabled={pending}>
            {pending ? "Sending…" : "Send reset link"}
          </button>
        </form>
      )}
      <p className="hint">
        <Link to="/login">Back to sign in</Link>
      </p>
    </section>
  );
}
