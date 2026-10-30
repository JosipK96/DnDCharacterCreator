# Api

- Controllers map a `*ApiRequest` onto a command or query, and map what comes back (a read
  model, `PagedResult<T>`, `LoginResult`) onto a `*ApiResponse`.
- The Api serves the Blazor client (`MapStaticAssets` + `MapFallbackToFile`), so the client
  calls the API on its own origin: no CORS, no client-side API address. Run the Api to use it.

## Configuration

- The JWT signing key is never in appsettings: user secrets in development, `JWT_KEY` in the
  gitignored `.env` for docker compose (template: `.env.example`), `JwtSettings__Key` on a host.
  The only connection string in appsettings is the Development one, for the compose database
  on `localhost:5433`; elsewhere it comes from `ConnectionStrings__DefaultConnection`. The app
  refuses to start without either.
- Registration asks for an invite code only when the host sets `Registration__InviteCode`
  (`RegistrationSettings`, checked by `InviteCodeGate`). It is never in appsettings, and left
  unset registration stays open, as in local development and the integration tests.
- Sign-in and registration share one per-address rate limit (`RateLimitingExtensions`,
  `RateLimiting__PermitLimit`/`WindowSeconds`, 429 `RateLimit.TooManyRequests`); the address is
  what `ASPNETCORE_FORWARDEDHEADERS_ENABLED` leaves on the connection, so the limiter sits
  after it. Account lockout is `Identity__Lockout__*`, applied in `IdentityService`; every failed
  sign-in answers `UserErrors.InvalidCredentials`, so none says whether an email has an account.
  The test host sets no forwarded headers: its tests put the address on the connection themselves.
- `docker-compose.yml` (+ `.override.yml`, `.dcproj`) sits at the repository root because the
  Dockerfile builds from there: restore needs `Directory.*.props`, `.editorconfig` and
  `analyzers/`. Non-secret container settings are in `configurations/Env_files/api.env`.
- `Program.cs` registers services while the `WebApplicationBuilder` is built, which is before
  `WebApplicationFactory` applies test configuration — which is why the API integration tests
  pass config through environment variables.
