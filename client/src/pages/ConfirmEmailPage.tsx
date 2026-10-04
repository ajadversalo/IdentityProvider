import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { api } from "../api";

export function ConfirmEmailPage() {
  const [params] = useSearchParams();
  const userId = params.get("userId") ?? "";
  const token = params.get("token") ?? "";
  const [message, setMessage] = useState("Confirming your email…");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!userId || !token) {
      setError("This confirmation link is missing required values.");
      return;
    }

    void api<{ message: string }>("/api/account/confirm-email", {
      method: "POST",
      body: JSON.stringify({ userId, token })
    })
      .then((result) => setMessage(result.message))
      .catch((err: unknown) => setError(err instanceof Error ? err.message : "Confirmation failed."));
  }, [token, userId]);

  return (
    <section className="card">
      <h1>Email confirmation</h1>
      {error ? <p className="error">{error}</p> : <p className="ok">{message}</p>}
      <p>
        <Link to="/login">Continue to sign in</Link>
      </p>
    </section>
  );
}
