import { useAuth } from "react-oidc-context";
import { Link } from "react-router-dom";

export function HomePage() {
  const auth = useAuth();

  return (
    <section className="hero">
      <h1>A small identity provider you can grow.</h1>
      <p className="lede">
        OpenID Connect and OAuth 2.0 with authorization code + PKCE, ASP.NET Identity, and a React
        account UI. Email and password now. More later.
      </p>
      <div className="actions">
        {auth.isAuthenticated ? (
          <Link className="btn" to="/dashboard">
            Open dashboard
          </Link>
        ) : (
          <>
            <button className="btn" type="button" onClick={() => void auth.signinRedirect()}>
              Sign in
            </button>
            <Link className="btn secondary" to="/register">
              Create account
            </Link>
          </>
        )}
      </div>
    </section>
  );
}
