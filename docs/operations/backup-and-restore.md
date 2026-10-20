# Backing up and restoring the hosted database

The hosted database is Neon's free plan, which keeps only 6 hours of history (ADR 0003). The
friends' characters are the one thing that can't be rebuilt, so a GitHub Actions workflow takes
our own nightly dump. The reference data needs no backup: it is in the repository and reloads
on startup.

## What runs

[`.github/workflows/backup.yml`](../../.github/workflows/backup.yml) runs every night at 02:30
UTC, and from the Actions tab (Backup > Run workflow) on demand. It:

1. fails at once if either secret below is missing;
2. runs `pg_dump --format=custom --no-owner --no-privileges` from the `postgres:17` image;
3. encrypts the dump with `gpg --symmetric` (AES-256) and deletes the plaintext;
4. uploads the encrypted file as the artifact `backup-<date>-<run id>`, kept for 30 days.

A failed run is red on the Actions tab, and GitHub emails the repository owner about it.

The repository is public, so anyone signed in to GitHub can download the artifacts. Without
the passphrase they are unreadable, and nothing unencrypted is ever uploaded.

GitHub turns off scheduled workflows in a public repository after 60 days without activity in
it. If that happens, the Actions tab says so and has a button to turn the workflow back on.

## The secrets

In the repository's Settings > Secrets and variables > Actions:

- `BACKUP_DATABASE_URL`: Neon's **direct** connection string (the host without `-pooler`), in
  the `postgresql://<user>:<password>@<ep-…>.eu-central-1.aws.neon.tech/<db>?sslmode=require`
  form Neon's Connect dialog shows. This is not the Npgsql form the Render service uses.
- `BACKUP_PASSPHRASE`: a long random passphrase, for example from `openssl rand -base64 32`.
  **Keep a copy in a password manager.** GitHub never shows a secret again, and without the
  passphrase no dump can be decrypted.

If Neon's password is reset, update `BACKUP_DATABASE_URL` along with Render's connection string.

## Restoring into a scratch database (the test)

This proves a dump restores, without touching the hosted database. You need Docker, the `gh`
CLI and `gpg` (Git for Windows ships it in Git Bash).

1. Download the newest dump:

   ```bash
   gh run list --repo JosipK96/DnDCharacterCreator --workflow backup.yml --limit 5
   gh run download <run id> --repo JosipK96/DnDCharacterCreator --dir restore
   ```

2. Decrypt it. gpg asks for the passphrase:

   ```bash
   gpg --output restore/charactercreator.dump --decrypt restore/backup-*/charactercreator.dump.gpg
   ```

3. Start an empty PostgreSQL of the same major version as the dump, and restore into it:

   ```bash
   docker run -d --name charactercreator-restore -e POSTGRES_PASSWORD=restore -p 5434:5432 postgres:17
   docker cp restore/charactercreator.dump charactercreator-restore:/tmp/charactercreator.dump
   docker exec charactercreator-restore createdb -U postgres charactercreator
   docker exec charactercreator-restore pg_restore -U postgres --dbname=charactercreator --no-owner --no-privileges --exit-on-error /tmp/charactercreator.dump
   ```

   `--exit-on-error` makes any failure stop the restore and show the error, instead of a
   half-restored database that looks fine.

4. Point the Api at it. Run it from an up-to-date `main`, so it applies no migration the hosted
   app doesn't have. In PowerShell, from the repository root:

   ```powershell
   $env:ConnectionStrings__DefaultConnection = "Host=localhost;Port=5434;Database=charactercreator;Username=postgres;Password=restore"
   dotnet run --project src/CharacterCreatorApp.Api --launch-profile http
   ```

   The environment variable overrides both `appsettings.Development.json` and user secrets.
   Sign in at http://localhost:5207 as a user from the hosted app, and open one of its
   characters: the sheet should show the same character as the hosted app.

5. Clean up. The plaintext dump holds emails and password hashes:

   ```bash
   docker rm -f charactercreator-restore
   rm -rf restore
   ```

## Restoring the hosted app for real

If the Neon database is lost or damaged:

1. Create a new, empty database in Neon (a new project, or a new database in the existing one),
   in Frankfurt.
2. Download and decrypt the dump you want, as in steps 1 and 2 above.
3. Restore it with Neon's direct connection string for the new database:

   ```bash
   docker run --rm -v "$PWD/restore:/restore" postgres:17 pg_restore --dbname="<direct postgresql:// URL>" --no-owner --no-privileges --exit-on-error /restore/charactercreator.dump
   ```

4. In the Render dashboard, set `ConnectionStrings__DefaultConnection` to the new database
   (Npgsql form, as the README's Hosting section shows). Render redeploys. Set
   `BACKUP_DATABASE_URL` here to the new database too.
5. Delete the `restore` folder.

## Tested

- *Not yet: record the date and the dump's run here once a restore has been done.*
