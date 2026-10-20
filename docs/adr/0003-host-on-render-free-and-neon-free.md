# Host on Render's free tier with Neon's free Postgres

The app serves the owner and at most eight friends. Paid hosts with the app and database together (Railway Hobby ~$5, Render Starter + Postgres ~$14) and a self-run Hetzner VPS were compared in #31, and the owner chose to start at $0: the Api container on Render's free web service and the database on Neon's free plan. Render's own free Postgres was ruled out because it is deleted after 30 days, and Supabase's free one pauses after a week idle; Neon's doesn't expire. The app is one container plus a connection string, so moving host later is a `pg_dump`/`pg_restore` and two environment variables.

## Consequences

- The Render service sleeps after 15 minutes idle and takes about a minute to wake; the fix, if it annoys, is Render Starter ($7/month), with nothing else changing.
- Neon keeps only 6 hours of history, so our own nightly `pg_dump` is the backup (#41). The repository is public and the dump holds emails and password hashes, so it is encrypted before upload or kept in the private archive.
- Registration is open to anyone with the link, so sign-ups are limited by a single invite code in the host's settings (#40), not per-user or expiring codes. The link isn't shared until #40, #42 and #41 are done.
