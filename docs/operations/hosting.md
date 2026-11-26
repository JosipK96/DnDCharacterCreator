# Hosting

How the live site is deployed and run: the settings a host needs, where the group's app runs,
and the protections around sign-in. How a version goes live is in [releasing.md](releasing.md);
backups and restoring one are in [backup-and-restore.md](backup-and-restore.md).

## Deploying

The image built from `src/CharacterCreatorApp.Api/Dockerfile` is the whole app. A host needs
two settings as environment variables:

- `ConnectionStrings__DefaultConnection`, the host's PostgreSQL connection string
- `JwtSettings__Key`, the signing key

and one it should have before the link is shared:

- `Registration__InviteCode`, the code registering asks for. Give it to the group by hand; it
  is never in the repository. Without it anyone with the address can make an account. Make it
  long and random (a password manager's generator will do): registration is rate limited
  (see Sign-in protection), but the code is still what stops someone guessing.

and one the owner needs to help a friend who forgot their password (see Resetting a password):

- `Admin__Email`, the email of the owner's own account. Only that account can create reset links;
  left unset, nobody can.

`configurations/Env_files/api.env` holds the non-secret container settings. To run the compose
file without its Development override, use `docker compose -f docker-compose.yml up -d`.

## The group's app

The group's app runs on [Render](https://render.com)'s free web service, with the database on
[Neon](https://neon.tech)'s free Postgres, both in Frankfurt (decided in #31). The address is
Render's `*.onrender.com` one, over Render's HTTPS.

- **The service** is described by [render.yaml](../../render.yaml), a Render Blueprint: the
  Docker build, the region, the `/health` check, port 8080, and the non-secret settings from
  `configurations/Env_files/api.env`. It was created in the Render dashboard with New >
  Blueprint, and changes to the file are applied from there.
- **Deploys:** publishing a release deploys it ([releasing.md](releasing.md)); merging to `main`
  does not. Migrations and the reference data load run at startup; the deploy log shows
  "Applying pending migrations" and "Seeding reference data". If either fails, the new version
  never passes the health check and the previous one keeps serving.
- **Secrets** live only in the Render dashboard (the service's Environment page), never here:
  - `ConnectionStrings__DefaultConnection`: Neon's *direct* endpoint (not the `-pooler` one, so
    migrations get a normal session), in Npgsql's form rather than the `postgres://` URL Neon
    shows: `Host=<ep-…>.eu-central-1.aws.neon.tech;Database=<db>;Username=<user>;Password=<password>;SSL Mode=VerifyFull;GSS Encryption Mode=Disable`
  - `JwtSettings__Key`: a random key of at least 32 characters
  - `Registration__InviteCode`: the invite code (see Deploying)
  - `Admin__Email`: the owner's email (see Resetting a password)
  - `Sentry__Dsn`: optional, the error tracker's DSN (see Errors)
- **Database version:** Neon runs PostgreSQL 18. `docker-compose.yml` and the three
  Testcontainers fixtures use `postgres:18-alpine` so CI and local runs see what production
  sees; when Neon upgrades, move them (and the `postgres:18` in the backup workflow) in the same
  change.
- **Backups:** Neon's free plan keeps only 6 hours of history, so a nightly workflow keeps an
  encrypted `pg_dump` for 30 days, and a watch workflow fails when no backup is newer than 48
  hours or the schedule was switched off. Its secrets, and how to restore a dump, are in
  [backup-and-restore.md](backup-and-restore.md).
- **Sleeping:** the free service sleeps after 15 minutes without traffic and takes about a
  minute to wake; Neon's free database suspends after 5 minutes and wakes in under a second.
  To stop the sleeping, change the service's instance type to Starter ($7/month) on its
  Settings page, and `plan: free` in `render.yaml` to `plan: starter`. Nothing else changes.

## Errors

Unhandled exceptions on the server, and the errors the client hits, go to
[Sentry](https://sentry.io)'s free plan (#77). Each event has the release (the version `/health`
shows), the request path and a trace id: the `traceId` in the 500 answer's body, so a friend's
report can be matched to its event. Nothing else leaves the app: no request body, headers,
cookies, query string, user or address, no log lines, and email-like text is masked. The client
posts its errors to `POST api/client-errors` (rate limited, bounded), and the API passes them
on, so there is one key and one place to switch it off.

- `Sentry__Dsn`: the Sentry project's DSN, a secret in the Render dashboard (service >
  Environment) like the others, never in this repository. **Empty or removed turns error
  reporting off**; nothing is sent and the app runs as before. Local runs and the tests have
  none.
- **Setting it up** (owner only): create a free Sentry project of platform ASP.NET Core, copy
  its DSN into `Sentry__Dsn`, and redeploy (the setting is read at start). To check it works,
  sign in as the owner (`Admin__Email`) and call `POST api/admin/test-error` (the Swagger page
  exists only in development, so use any HTTP client with the sign-in token). It always answers
  500, and the event appears in Sentry with its version and path. Open the event's JSON to see
  nothing personal is in it.

## Sign-in protection

All optional, each with a default (#72). Environment variables on the service, like the
secrets, but not secret:

- `Identity__Lockout__MaxFailedAccessAttempts` (default 5) and
  `Identity__Lockout__DefaultLockoutTimeSpan` (default `00:15:00`): wrong passwords before an
  account is locked, and for how long. A locked account is refused even with the right
  password, with the same "Wrong email or password" answer as any failed sign-in.
- `RateLimiting__PermitLimit` (default 10) and `RateLimiting__WindowSeconds` (default 60):
  sign-in and registration attempts one client address may make per window, counted together
  and including wrong invite codes; past it the API answers 429. The address is the one
  `ASPNETCORE_FORWARDEDHEADERS_ENABLED` (already set in `render.yaml`) takes from Render's
  `X-Forwarded-For`: only the last entry, which Render's proxy appended, so a header a caller
  adds is ignored. That setting trusts the connection it comes in on, which is right because
  the container is reachable only through Render; behind a host that exposes it directly,
  a caller could forge the address.
- Passwords need at least 8 characters, an uppercase letter and a digit. Accounts made before
  keep working with their old password.

## Resetting a password

The app sends no email, so a friend who forgot their password asks the owner (#71). The owner
signs in with the account named by `Admin__Email`, opens `/admin/reset-link`, types the friend's
email and sends them the link it shows, by any message. The link opens a page where the friend
chooses a new password; it works once and expires after 24 hours, and a newer link or any
password change makes an older one stop working. A used, expired or invalid link, and an email
with no account, all get the same answer. A reset also ends a lockout.

Anyone signed in can change their own password at `/account/password` with their current one.
The same limits as sign-in apply to all of it (the per-address rate limit; a wrong current
password counts toward lockout). A sign-in token issued before a reset or change stays valid
until it expires (`JwtSettings__ExpiryInMinutes`, 60 by default): tokens are not checked
against the account's security stamp, which would cost a database read on every request.
