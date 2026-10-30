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
  `RateLimiting__PermitLimit`/`WindowSeconds`, 429 with `TooManyRequestsCode`); the address is
  what `ASPNETCORE_FORWARDEDHEADERS_ENABLED` leaves on the connection, so the limiter sits
  after it. Account lockout is `Identity__Lockout__*`, applied in `IdentityService`; every failed
  sign-in answers `UserErrors.InvalidCredentials`, so none says whether an email has an account.
  The test host sets no forwarded headers: its tests put the address on the connection themselves.
- Password recovery (#71): no email is sent. The owner, the account whose email is
  `Admin__Email` (`AdminSettings`, checked by `AdminGate`; unset means nobody), makes a one-time
  link at `POST api/admin/password-reset-links`; `POST api/users/reset-password` uses it and
  `POST api/users/change-password` changes a signed-in user's password, both on the sign-in
  rate limit. A used, expired or wrong link and an unknown email all answer
  `UserErrors.InvalidResetLink`. The link lasts 24 hours (the data-protection token provider options
  in `AddIdentity`) and the reset changes the security stamp, which is what makes it single-use.
  The JWT carries no stamp, so tokens issued earlier live until they expire.
- `docker-compose.yml` (+ `.override.yml`, `.dcproj`) sits at the repository root because the
  Dockerfile builds from there: restore needs `Directory.*.props`, `.editorconfig` and
  `analyzers/`. Non-secret container settings are in `configurations/Env_files/api.env`.
- `Program.cs` registers services while the `WebApplicationBuilder` is built, which is before
  `WebApplicationFactory` applies test configuration — which is why the API integration tests
  pass config through environment variables.
