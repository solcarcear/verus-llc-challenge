# Backend Setup Instructions

This backend persists data in **SQL Server** via **Entity Framework Core**. SQL Server runs in
**Docker**; the API itself runs directly with the .NET SDK (no API container yet).

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/get-started/) (Docker Desktop or Docker Engine + Compose plugin)

Verify both are installed:

```bash
dotnet --version   # should print an 8.x.x version
docker --version
```

## 1. Configure environment variables

Credentials are **not** committed to the repo. From the **repository root**, copy the example
environment file and set your own local password:

```bash
cp .env.example .env
```

Open `.env` and set a value for `SQL_SERVER_PASSWORD`, and update the matching `Password=` segment
in `ConnectionStrings__SqlServer` to the same value:

```bash
SQL_SERVER_PASSWORD='your_password_here'

ConnectionStrings__SqlServer='Server=localhost,1433;Database=CompanyManagement;User Id=sa;Password=your_password_here;TrustServerCertificate=True;'
```

Keep the values single-quoted: the connection string contains `;` and a space (`User Id`), which a
plain `source .env` (step 4 below) would otherwise interpret as separate shell commands.

`.env` is git-ignored — only `.env.example` (with placeholder values) is tracked. Never commit the
real `.env` file.

## 2. Start SQL Server

The SQL Server container is defined in `docker-compose.yml` at the repository root. It reads
`SQL_SERVER_PASSWORD` from `.env` automatically (Docker Compose loads a root-level `.env` for
variable substitution), so run it from the repository root:

```bash
# from the repository root
docker compose up -d
```

This starts a `mcr.microsoft.com/mssql/server:2022-latest` container named `verus-sqlserver`:

- Port: `1433` (mapped to `localhost:1433`)
- SA password: whatever you set as `SQL_SERVER_PASSWORD` in `.env`
- Data persisted in the `verus-sql-data` Docker volume

## 3. Verify SQL Server is running

```bash
docker compose ps                # sqlserver service should show "running"/"healthy"
docker logs verus-sqlserver      # wait for "SQL Server is now ready for client connections"
```

To stop it later: `docker compose down` (add `-v` to also delete the data volume — don't do this
unless you actually want to lose local data).

## 4. Export the environment variables into your shell

The API runs **outside** Docker, so it doesn't automatically see `.env`. Export its variables into
your current shell before starting the API — from the repository root:

```bash
set -a
source ../.env
set +a
```

This puts `ConnectionStrings__SqlServer` into the process environment. ASP.NET Core's built-in
configuration system maps the double-underscore (`__`) syntax to the nested configuration key
`ConnectionStrings:SqlServer`, and environment variables override `appsettings.json` — which no
longer contains any connection string or credentials. The API reads it via:

```csharp
builder.Configuration.GetConnectionString("SqlServer")
```

Keep this shell open (or re-run `set -a && source .env && set +a` from the repo root in any new
shell) for every step below.

## 5. Restore, build, and run the API

```bash
cd backend
dotnet restore
dotnet build
```

Apply EF Core migrations against the database explicitly:

```bash
dotnet ef database update \
  --project src/CompanyManagement.Infrastructure \
  --startup-project src/CompanyManagement.Api
```

This step is optional in practice — `Program.cs` also calls `dbContext.Database.Migrate()`
automatically on startup, so `dotnet run` alone creates/updates the `CompanyManagement` database
too. Running it explicitly here is a useful way to catch a bad connection string or a SQL Server
that isn't ready yet, before starting the API.

Start the API:

```bash
dotnet run --project src/CompanyManagement.Api
```

The API is available at:

- `http://localhost:5000`
- `https://localhost:7000`

Swagger UI is served at `/swagger` (Development environment only).

### (Optional) Manage migrations manually

Only needed if you change the EF model and want to add a new migration. Requires the EF Core CLI
tool:

```bash
dotnet tool install --global dotnet-ef   # one-time install
cd backend

# add a new migration after changing the model
dotnet ef migrations add <MigrationName> \
  --project src/CompanyManagement.Infrastructure \
  --startup-project src/CompanyManagement.Api
```

## 6. Development seed data

In the `Development` environment only, the API automatically seeds the database with **~5,000
realistic, fictional companies** the first time it starts against an empty/unseeded database.
This is controlled by `appsettings.Development.json`:

```json
"SeedData": {
  "Enabled": true,
  "CompanyCount": 5000
}
```

What to expect:

- **First run**: `dotnet run` applies migrations, then seeds ~5,000 companies. You'll see
  `Seeded 5000 development companies.` in the console output.
- **Every run after that**: seeding is skipped — you'll see
  `Development seed data already present; skipping company seeding.` instead. Restarting the API
  (any number of times) will **not** create more companies.
- **Any companies you create by hand** (via Swagger, `curl`, or the frontend) are never deleted,
  overwritten, or duplicated by the seeder, before or after seeding runs.
- This only runs when `ASPNETCORE_ENVIRONMENT=Development` (the default for `dotnet run`).
  `appsettings.json` — used by every other environment, including Production — has no `SeedData`
  section, so seeding is disabled there by default even without the environment check.

How idempotency works: seeded companies get deterministic IDs (every one starts with
`53454544-0000-0000-...`, ASCII "SEED" in hex) instead of random GUIDs. On startup, the seeder
checks for the presence of just the *first* deterministic seed ID — not `Companies.Any()` — so it
correctly recognizes "already seeded" even if you'd already created a few companies of your own
beforehand, and never touches those companies.

The generated data is created programmatically (combining realistic name/industry word components
and domains — see `CompanySeedDataGenerator` in
`src/CompanyManagement.Infrastructure/Persistence/Seed/`), not hardcoded, and deliberately includes
companies whose name is an exact match, a partial match, a loose/token-level match, or **not**
relevant to their website, so `CompanyRelevanceEvaluator` and search/filtering can be exercised
against realistic variety. Generation is deterministic (fixed per-record seed), so re-running the
generator against an empty database always produces the same dataset.

No new EF Core migration was needed for this — seeding only inserts rows into the existing
`Companies` table (`Id`, `Name`, `WebsiteUrl`), which the `InitialCreate` migration already created.

### Inspect the seeded data in SSMS / sqlcmd

Connect to `localhost,1433` with user `sa` and your `SQL_SERVER_PASSWORD`, database
`CompanyManagement`, then:

```sql
-- Total companies (should be ~5,000, plus any you created yourself)
SELECT COUNT(*) AS TotalCompanies FROM Companies;

-- How many of those are seed-generated vs. user-created
SELECT COUNT(*) AS SeedGenerated
FROM Companies
WHERE CONVERT(varchar(36), Id) LIKE '53454544-0000-0000-%';

-- Sample a handful of rows
SELECT TOP 20 Id, Name, WebsiteUrl FROM Companies ORDER BY NEWID();

-- Companies matching a specific industry/keyword
SELECT Id, Name, WebsiteUrl FROM Companies WHERE Name LIKE '%Robotics%';
```

### Resetting/regenerating the seed dataset

The container uses a **persistent Docker volume** (`verus-sql-data`), so data survives container
restarts. Do **not** run `docker compose down -v` just to reset seed data — that destroys the
volume and everything in it (including any of your own data), which is almost never what you want
for this purpose.

To regenerate just the seed dataset while keeping the volume and any companies you created by
hand, delete only the seed-marked rows (identifiable by the `53454544-0000-0000-...` Id prefix)
and restart the API:

```sql
DELETE FROM Companies WHERE CONVERT(varchar(36), Id) LIKE '53454544-0000-0000-%';
```

```bash
dotnet run --project src/CompanyManagement.Api
```

If you genuinely want to wipe everything (seed data *and* anything you created) and start over,
that's a deliberate, explicit choice — do it yourself with `docker compose down -v` (removing the
volume) or `TRUNCATE TABLE Companies;`, understanding it is destructive and not reversible.

## 7. Run the tests

Unit and API tests do **not** require the SQL Server container, `.env`, or any exported
environment variables — API/integration tests swap in the EF Core InMemory provider (see
`TestWebApplicationFactory`), and unit tests exercise repositories/services directly.

```bash
cd backend
dotnet test
```

## 8. Verify manually

With the container running and the environment variables exported (steps 2–4) and the API running
(step 5):

```bash
curl http://localhost:5000/api/companies
```

Or open `http://localhost:5000/swagger` in a browser and try the endpoints interactively.

## Troubleshooting

- **`InvalidOperationException: Connection string 'SqlServer' is not configured.`**: the
  `ConnectionStrings__SqlServer` environment variable isn't set in the shell running the API. Make
  sure you ran `set -a && source .env && set +a` from the repository root in *this* shell before
  `dotnet run`.
- **API fails at startup with a connection error**: the SQL Server container may still be
  initializing, or the password in `.env`'s `ConnectionStrings__SqlServer` doesn't match
  `SQL_SERVER_PASSWORD`. Re-check `docker logs verus-sqlserver` for "SQL Server is now ready for
  client connections", and confirm both values in `.env` use the same password.
- **Port `1433` already in use**: stop any other local SQL Server instance, or change the host
  port mapping in `docker-compose.yml` and the port in `ConnectionStrings__SqlServer` to match.
- **Reset just the seed data** (keep the volume and any companies you created): see
  "Resetting/regenerating the seed dataset" under step 6.
- **Reset the entire database** (destroys the volume and everything in it): `docker compose down -v
  && docker compose up -d`, then restart the API so migrations reapply against the fresh database.
