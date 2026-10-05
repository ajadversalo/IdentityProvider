# Identity Provider

.NET 8 OpenID Connect / OAuth 2.0 identity provider. Users live in ASP.NET Identity. Tokens are issued by OpenIddict. The account UI is a React (Vite + TypeScript) SPA in this same repo and is served by the ASP.NET app in production.

This is the core cut: email and password, email confirmation, password reset, authorization code + PKCE, refresh tokens, and a first-party SPA client. Social logins, MFA, admin UI, and extra clients come later.

## Prerequisites

- .NET 8 SDK (this repo pins `8.0.416` in `global.json`)
- Node.js 20 or later
- SQL Server LocalDB instance `MSSQLLocalDB`, or another SQL Server you point the connection string at

Check:

```powershell
dotnet --list-sdks
node --version
sqllocaldb info MSSQLLocalDB
```

If LocalDB is installed but stopped:

```powershell
sqllocaldb start MSSQLLocalDB
```

## Run locally

Install the SPA packages once:

```powershell
cd client
npm install
```

Then start the host from the repo root or from Visual Studio.

### Visual Studio / VS Code

Open `IdentityProvider.slnx`. Run the **https** profile on `IdentityProvider.Web`.

The profile starts Kestrel at **https://localhost:7120** and launches Vite in the background. Stay on https://localhost:7120 — that is the identity provider origin. `http://localhost:5173` is only Vite’s internal server; the app redirects you back to port 7120.

### Command line

```powershell
cd src\IdentityProvider.Web
dotnet run --launch-profile https
```

Open https://localhost:7120.

First start:

- Applies EF Core migrations to LocalDB database `IdentityProvider`
- Seeds roles `User` and `Admin`, the `api` scope, and the public client `identity-spa`
- Writes OpenIddict signing/encryption certificates and Data Protection keys under `src/IdentityProvider.Web/keys/` (gitignored)

### Try the sign-in flow

1. Open https://localhost:7120 and create an account.
2. Email is required before sign-in. In Development the register (and resend) response includes `confirmationUrl`. The same link is appended to `src/IdentityProvider.Web/logs/emails.log`.
3. Open that link, then sign in.
4. The SPA completes OIDC (authorization code + PKCE). The dashboard calls `GET /api/me` with the access token.

Password rules: at least 8 characters, one upper, one lower, and one digit. Accounts lock for 15 minutes after 5 failed logins.

SMTP is optional. With `Smtp:Host` empty, mail is logged only. Set SMTP in configuration when you want real delivery.

## Project layout

```
IdentityProvider.slnx
client/                         React + Vite + TypeScript SPA
src/IdentityProvider.Web/       ASP.NET Core 8 host (Identity, OpenIddict, APIs)
  Controllers/                  OIDC + account + /api/me
  Data/                         EF context, Identity user, seed, migrations
  keys/                         Signing certs and Data Protection (gitignored)
  wwwroot/                      Production SPA output from `npm run build`
```

Dev: the browser talks to https://localhost:7120. Unmatched routes go to Vite so you get React hot reload.

Production / publish: `npm ci` and `npm run build` copy the SPA into `wwwroot`. ASP.NET serves the UI and the identity endpoints from one site.

Skip the SPA build on publish:

```powershell
dotnet publish src/IdentityProvider.Web/IdentityProvider.Web.csproj -c Release /p:SkipSpaBuild=true
```

## OIDC

Seeded clients:

- `identity-spa` — public SPA for this repo’s React UI (implicit consent, PKCE required)
- `northstar` — public SPA for Northstar (implicit consent, PKCE required). Azure Static Web Apps Free cannot use custom Easy Auth, so Northstar talks to this IdP from the browser.

There is no admin UI yet; add more clients in seed/config.

| Endpoint | Purpose |
| --- | --- |
| `/.well-known/openid-configuration` | Discovery |
| `/.well-known/jwks` | Signing keys |
| `/connect/authorize` | Authorization code |
| `/connect/token` | Code exchange and refresh |
| `/connect/userinfo` | User claims |
| `/connect/logout` | End session |
| `/connect/revoke` | Token revocation |
| `/health` | Health (includes SQL) |

Account API (cookie session for the login UI):

| Method | Path |
| --- | --- |
| GET | `/api/account/status` |
| POST | `/api/account/register` |
| POST | `/api/account/login` |
| POST | `/api/account/logout` |
| POST | `/api/account/confirm-email` |
| POST | `/api/account/resend-confirmation` |
| POST | `/api/account/forgot-password` |
| POST | `/api/account/reset-password` |

Resource API (bearer access token):

| Method | Path |
| --- | --- |
| GET | `/api/me` |

SPA OIDC settings (`client/src/oidc.ts`):

- `client_id`: `identity-spa`
- `redirect_uri`: `{origin}/callback`
- `post_logout_redirect_uri`: `{origin}/`
- `scope`: `openid profile email roles offline_access api`

## Use from another app

This project is the **authority**. Your other app is an **OIDC client**. Users sign in on https://localhost:7120; your app receives an authorization code, exchanges it for tokens, then calls APIs with the access token.

Discovery document: https://localhost:7120/.well-known/openid-configuration

### 1. Register your app as a client

Today only `identity-spa` is seeded. Add a second public client in `SeedData` (or we can add a config list next). Minimum fields:

| Field | Example |
| --- | --- |
| `ClientId` | `my-other-app` |
| `ClientType` | Public (SPA/mobile) or Confidential (server app with a secret) |
| Redirect URI | `https://localhost:7200/callback` (must match exactly) |
| Post-logout URI | `https://localhost:7200/` |
| Permissions | authorization, token, end session, `authorization_code`, `refresh_token`, `code`, email/profile/roles, `api` |
| Requirement | PKCE (`ProofKeyForCodeExchange`) |

Restart the identity provider after seeding so the client exists in LocalDB.

Also add your app origin to `Cors:Origins` if the browser talks to this host directly (token, userinfo, or APIs).

### 2. Point your app at this issuer

**React / SPA** (`oidc-client-ts` or `react-oidc-context`):

```ts
{
  authority: "https://localhost:7120",
  client_id: "my-other-app",
  redirect_uri: "https://localhost:7200/callback",
  post_logout_redirect_uri: "https://localhost:7200/",
  response_type: "code",
  scope: "openid profile email roles offline_access api"
}
```

PKCE is required. Do not put a client secret in a browser app.

**ASP.NET Core MVC / Razor** (confidential client):

```csharp
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie()
.AddOpenIdConnect(options =>
{
    options.Authority = "https://localhost:7120";
    options.ClientId = "my-other-app";
    options.ClientSecret = "<from your confidential client>";
    options.ResponseType = "code";
    options.UsePkce = true;
    options.SaveTokens = true;
    options.GetClaimsFromUserInfoEndpoint = true;
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Scope.Add("roles");
    options.Scope.Add("offline_access");
    options.Scope.Add("api");
    options.CallbackPath = "/signin-oidc";
    options.SignedOutCallbackPath = "/signout-callback-oidc";
});
```

Redirect URI registered on the IdP must be `https://localhost:<your-port>/signin-oidc`.

**Your API** validating access tokens:

```csharp
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.Authority = "https://localhost:7120";
        options.Audience = "identity-api"; // resource on the `api` scope
        options.TokenValidationParameters.ValidateAudience = true;
    });
```

Audience is the OpenIddict resource `identity-api` (from the `api` scope). If validation fails on `aud`, inspect a token at https://jwt.io and align `Audience` with the `aud` claim.

### 3. Runtime flow

1. User hits a protected page in your app.
2. Your app redirects to `/connect/authorize` on this IdP.
3. If they have no session, they see this project’s login/register pages.
4. IdP redirects back to your `redirect_uri` with `?code=...`.
5. Your app exchanges the code at `/connect/token` (PKCE).
6. You get `id_token`, `access_token`, and (with `offline_access`) `refresh_token`.
7. Call APIs with `Authorization: Bearer <access_token>`.

The identity provider process must be running while you use the other app.

### Not wired yet

- Admin UI to create clients
- Client credentials (machine-to-machine) grant

## Configuration

Edit `src/IdentityProvider.Web/appsettings.json` locally. On Azure, override with App Settings (double underscore in names).

| Key | Meaning |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | SQL Server / LocalDB |
| `OpenIddict:Issuer` | Public HTTPS origin, including trailing slash |
| `OpenIddict:SpaClientId` | Seeded SPA client id (`identity-spa`) |
| `OpenIddict:RedirectUris` | Semicolon-separated redirect URIs |
| `OpenIddict:PostLogoutRedirectUris` | Semicolon-separated post-logout URIs |
| `OpenIddict:CertificatePassword` | Password for the PFX files under `keys/` |
| `OpenIddict:NorthstarClientId` | Public client id for Northstar (`northstar`) |
| `OpenIddict:NorthstarRedirectUris` | Semicolon-separated Northstar `/callback/` URIs |
| `OpenIddict:NorthstarPostLogoutRedirectUris` | Semicolon-separated Northstar `/signin/` URIs |
| `PublicAppUrl` | Origin used in confirmation and reset links |
| `Cors:Origins` | Allowed SPA origins |
| `Identity:RequireConfirmedEmail` | Default `true` |
| `Smtp:Host` / `Port` / `User` / `Password` / `From` | Optional real email |

Default LocalDB connection string:

```
Server=(localdb)\mssqllocaldb;Database=IdentityProvider;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True
```

Dev URLs already seeded for the SPA client:

- https://localhost:7120/callback
- http://localhost:5101/callback
- http://localhost:5173/callback

## Azure App Service Free (later)

One App Service for API + UI. SQL on Azure SQL Free (or any SQL Server). There is no Always On on Free; the app idles and cold-starts. That is expected.

1. Create an App Service with .NET 8, and an Azure SQL database.

Windows plan + web app. `az webapp list-runtimes --os-type windows` lists the stack as `dotnet|8`. In PowerShell, wrap with `cmd /c` so `|` is not a pipe:

```powershell
az appservice plan create --name plan-idp-dev --resource-group rg-idp-dev --sku F1 --is-linux false
cmd /c "az webapp create --name idp-<yourname> --resource-group rg-idp-dev --plan plan-idp-dev --runtime ""dotnet|8"""
```

2. Publish:

```powershell
dotnet publish src/IdentityProvider.Web/IdentityProvider.Web.csproj -c Release
```

3. Set App Settings:

```
ConnectionStrings__DefaultConnection = <azure sql>
OpenIddict__Issuer = https://<app>.azurewebsites.net/
OpenIddict__RedirectUris = https://<app>.azurewebsites.net/callback
OpenIddict__PostLogoutRedirectUris = https://<app>.azurewebsites.net/
PublicAppUrl = https://<app>.azurewebsites.net
Cors__Origins__0 = https://<app>.azurewebsites.net
Cors__Origins__1 = https://northstar.ajhub.ca
Cors__Origins__2 = https://purple-coast-001ea300f.7.azurestaticapps.net
OpenIddict__CertificatePassword = <strong secret>
```

4. Persist `src/IdentityProvider.Web/keys/` (OpenIddict certificates + Data Protection). If those files are lost on deploy or restart, previously issued tokens fail validation. On a single Free-tier instance use a durable path or Azure Files.

5. Forwarded headers are already enabled so TLS terminated at Azure still looks like HTTPS to OpenIddict.

6. Wire real email (`Smtp:*`, or replace `LoggingEmailSender` with Azure Communication Services / SendGrid).

## Troubleshooting

**Blank page at http://localhost:5173.** That is Vite’s internal URL. Open **https://localhost:7120** instead. The app now redirects 5173 to 7120.

**SPA proxy never starts.** Run `npm install` in `client/` first. Port 5173 must be free.

**Database errors on startup.** Start LocalDB: `sqllocaldb start MSSQLLocalDB`. Confirm the connection string.

**Email confirmation.** In Development, use `confirmationUrl` from the API or `src/IdentityProvider.Web/logs/emails.log`. Nothing is sent until SMTP is configured.

**HTTPS certificate warning.** Trust the .NET dev cert: `dotnet dev-certs https --trust`.

**Tokens stop working after a clean clone or deleted `keys/`.** Signing keys were regenerated. Sign in again. Do not delete `keys/` on a deployed instance.

## Later increments

- Microsoft / Google external login
- MFA
- Consent screen and extra clients
- Admin UI for users, roles, and applications
- Azure Communication Services or SendGrid
- Azure Blob Data Protection key ring
