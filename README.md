# Streak — Backend

ASP.NET Core 8 Web API for the Streak social habit-tracking platform. Implements authentication (via Firebase ID tokens), shared streak management, daily check-ins with media uploads, dynamic streak count calculation, and a points-based protection economy.

---

## Tech stack

| Layer | Technology |
|---|---|
| Runtime | .NET 8 |
| Web framework | ASP.NET Core 8 (Web API + MVC controllers) |
| Database | SQL Server (LocalDB / Express / Developer / Azure SQL) |
| ORM | Entity Framework Core 8 (Code-First migrations) |
| Auth | Firebase Admin SDK (verifies ID tokens issued by Firebase Authentication) |
| Logging | Serilog (console + daily rolling file) — captures EF Core SQL command logging |
| API docs | Swashbuckle / Swagger UI |
| File storage | Local disk (abstracted behind `IMediaStorage` for future S3/Firebase Storage swap) |

---

## Architecture

Clean architecture with one-directional dependencies: `Api → Infrastructure → Application → Domain`. Domain has zero outward dependencies.

```
backend/
├── StreakPlatform.sln
├── StreakPlatform.Domain/                 Entities (no dependencies)
│   └── Entities/
│       ├── User.cs
│       ├── Streak.cs
│       ├── Participant.cs
│       ├── CheckIn.cs
│       ├── StreakProtection.cs
│       ├── PointsTransaction.cs
│       └── enums (ProtectionStatus, PointsTransactionReason)
│
├── StreakPlatform.Application/            DTOs, interfaces, services (depends on Domain)
│   ├── Common/                            AppOptions, AppExceptions
│   ├── DTOs/                              Request/response shapes
│   ├── Interfaces/                        IRepository<T>, I*Service contracts
│   └── Services/
│       ├── UserService.cs
│       ├── StreakService.cs
│       ├── CheckInService.cs              ← protection-aware check-in flow
│       ├── PointsService.cs
│       ├── StreakProtectionService.cs
│       ├── StreakCountCalculator.cs       ← pure helper, dynamic count from check-ins + protections
│       ├── InviteCodeGenerator.cs
│       └── InviteUrlBuilder.cs
│
├── StreakPlatform.Infrastructure/         EF Core, repositories, storage (depends on Application)
│   ├── DependencyInjection.cs             AddInfrastructure(...)
│   ├── Persistence/
│   │   ├── AppDbContext.cs                DbSets, fluent config, indexes
│   │   ├── *Repository.cs                 EF-backed repository implementations
│   │   ├── UnitOfWork.cs
│   │   └── Migrations/                    EF migration history
│   └── Storage/
│       └── LocalMediaStorage.cs           IMediaStorage implementation (./Media folder)
│
└── StreakPlatform.Api/                    Web host (depends on Infrastructure)
    ├── Program.cs                         Bootstrap: Serilog, Firebase, CORS, middleware
    ├── Auth/
    │   ├── FirebaseAuthMiddleware.cs      Verifies Bearer tokens on /api/* paths
    │   └── CurrentUserAccessor.cs         Per-request convenience for FirebaseUid/Email
    ├── Middleware/
    │   └── ExceptionHandlingMiddleware.cs ProblemDetails errors with machine code
    ├── Controllers/                       Thin — controllers only call services
    └── appsettings*.json
```

### Why clean architecture
- **Domain** never depends on EF/HTTP/Firebase — easy to unit-test.
- **Application** owns business rules (e.g. protection auto-consume, dynamic count) and is provider-agnostic.
- **Infrastructure** is the only place EF Core / SQL Server / file system specifics live.
- **Api** is the thin transport layer.

---

## Prerequisites

| Requirement | How to check / install |
|---|---|
| .NET 8 SDK | `dotnet --version` should print `8.x`. Install: <https://dotnet.microsoft.com/download> |
| SQL Server | Any flavor: LocalDB (Express LocalDB), SQL Server Express, Developer, or Azure SQL. Default config in this repo points at `.\SQLEXPRESS`. |
| Firebase project | Email/Password sign-in enabled. Generate a service account JSON: Project Settings → Service Accounts → Generate new private key. |

---

## First-time setup

1. **Drop the Firebase service account JSON** at:
   ```
   backend/StreakPlatform.Api/firebase-service-account.json
   ```
   This file is gitignored.

2. **Configure your SQL Server connection** in `backend/StreakPlatform.Api/appsettings.json` (or `appsettings.Development.json` to override locally). Examples:

   ```json
   "ConnectionStrings": {
     "Default": "Server=.\\SQLEXPRESS;Database=StreakPlatform;Trusted_Connection=True;TrustServerCertificate=True;"
   }
   ```
   Other valid forms:
   - LocalDB: `Server=(localdb)\\MSSQLLocalDB;Database=StreakPlatform;Trusted_Connection=True;`
   - SQL auth: `Server=.;Database=StreakPlatform;User Id=sa;Password=Pwd;TrustServerCertificate=True;`

3. **Restore + install EF tools** (once):
   ```powershell
   cd backend
   dotnet restore
   dotnet tool install --global dotnet-ef
   ```

4. **Generate the initial migration** (first time only — or after model changes):
   ```powershell
   dotnet ef migrations add Initial `
     --project StreakPlatform.Infrastructure `
     --startup-project StreakPlatform.Api `
     --output-dir Persistence/Migrations
   ```

5. **Run the API**:
   ```powershell
   dotnet run --project StreakPlatform.Api
   ```
   On startup the host calls `db.Database.Migrate()` which applies any pending migrations and creates the database if needed.

   You should see Serilog output similar to:
   ```
   2026-04-27 12:00:01.123 [INF] Streak API
   Now listening on: http://localhost:5000
   ```

   Test it:
   - Swagger UI: <http://localhost:5000/swagger>
   - Health: <http://localhost:5000/health>

---

## Database & migrations

### Schema (current)
| Table | Purpose | Notable indexes |
|---|---|---|
| `Users` | Local row mirroring a Firebase identity. `PointsBalance` default 100. | Unique on `FirebaseUid`, unique on `Email` |
| `Streaks` | Habit definition. `InviteCode` unique. `RequiresProof` toggles per-streak proof requirement. | Unique on `InviteCode` |
| `Participants` | Many-to-many: a user joined a streak. | Unique on `(UserId, StreakId)` |
| `CheckIns` | Daily check-in. `Note` (≤500), `MediaUrl` (≤500), `MediaContentType`. | Unique on `(UserId, StreakId, Date)`; index on `(StreakId, CreatedAt)` for feed |
| `StreakProtections` | A purchased protection day for a (user, streak). | Filtered unique index: at most one `Pending` per `(User, Streak)` |
| `PointsTransactions` | Append-only audit log of every points change. | Index on `(UserId, CreatedAt)` |

### Common EF commands

```powershell
# Add a migration after changing entities or DbContext:
dotnet ef migrations add <NameOfMigration> `
  --project StreakPlatform.Infrastructure `
  --startup-project StreakPlatform.Api `
  --output-dir Persistence/Migrations

# Apply pending migrations to the configured database (also done automatically on app startup):
dotnet ef database update `
  --project StreakPlatform.Infrastructure `
  --startup-project StreakPlatform.Api

# Roll back the last migration (without dropping it from the project):
dotnet ef database update <PreviousMigrationName> ...

# Remove the most recent migration before it's been applied:
dotnet ef migrations remove --project StreakPlatform.Infrastructure --startup-project StreakPlatform.Api

# Generate a SQL script for review or out-of-band deployment:
dotnet ef migrations script --project StreakPlatform.Infrastructure --startup-project StreakPlatform.Api -o migrate.sql
```

> If you ever see "no project was found" — make sure `--project` and `--startup-project` flags are present. EF can't infer them from PowerShell's working directory alone.

---

## Logging (database + application)

The API uses **Serilog** as its logging pipeline. EF Core's logger automatically pipes through it, so SQL commands, parameter values, transactions, and connection events all appear in both **console** and a **daily rolling file**.

### Where logs go
- **Console**: structured colorized output during `dotnet run`.
- **File**: `backend/StreakPlatform.Api/Logs/streak-YYYYMMDD.log` (rolls daily, keeps last 14 files). Folder is gitignored.

### What's logged
| Source | Default level | What you see |
|---|---|---|
| HTTP requests (Serilog request middleware) | Information | One line per request: `HTTP GET /api/streaks responded 200 in 12.3 ms` |
| EF Core **commands** (SQL) | Information | Every executed SQL statement |
| EF Core **connections** | Information | "Opened connection to database 'StreakPlatform'" |
| EF Core **transactions** | Information | Begin/commit/rollback events |
| EF Core **updates** | Information | "Saved 3 entities to database in 14ms" |
| Application code (`ILogger<T>`) | Information / Debug in dev | Whatever services call `_log.LogInformation(...)` |
| Unhandled exceptions | Error | Full stack via `ExceptionHandlingMiddleware` + Serilog |

### Sample log line — SQL command

```
2026-04-27 12:00:42.318 [INF] Microsoft.EntityFrameworkCore.Database.Command
Executed DbCommand (5ms) [Parameters=[@__userId_0='?' (DbType = Guid)], CommandType='Text', CommandTimeout='30']
SELECT [s].[Id], [s].[Name], [s].[Description], [s].[InviteCode], [s].[CreatedAt]
FROM [Streaks] AS [s]
INNER JOIN [Participants] AS [p] ON [s].[Id] = [p].[StreakId]
WHERE [p].[UserId] = @__userId_0
ORDER BY [s].[CreatedAt] DESC
```

By default parameter values are redacted as `'?'`. To include them, set `Database:EnableSensitiveDataLogging` to `true` (the development config does this).

### Tuning verbosity

Override any log level in `appsettings.json` → `Serilog.MinimumLevel.Override`. Examples:

```json
"Serilog": {
  "MinimumLevel": {
    "Default": "Information",
    "Override": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Warning"   // hide SQL
    }
  }
}
```

To enable parameter logging in dev (already on by default in `appsettings.Development.json`):

```json
"Database": {
  "EnableSensitiveDataLogging": true,
  "EnableDetailedErrors": true
}
```

> ⚠ Never enable `EnableSensitiveDataLogging` in production — it logs raw parameter values, which can include emails or PII.

---

## Configuration reference

All keys live in `appsettings.json` and can be overridden by `appsettings.Development.json` or environment variables (using `__` separator).

| Key | Default | Purpose |
|---|---|---|
| `ConnectionStrings:Default` | `Server=.\SQLEXPRESS;Database=StreakPlatform;Trusted_Connection=True;TrustServerCertificate=True;` | SQL Server connection |
| `Firebase:ServiceAccountPath` | `firebase-service-account.json` | Path to Firebase Admin key |
| `Database:EnableSensitiveDataLogging` | `false` (dev: `true`) | Includes raw parameter values in EF logs |
| `Database:EnableDetailedErrors` | `false` (dev: `true`) | More detailed EF exceptions |
| `Cors:AllowedOrigins` | `["http://localhost:4200"]` | Frontend origins permitted |
| `App:PublicBaseUrl` | `http://localhost:4200` | Used when generating invite URLs |
| `App:InvitePath` | `/streaks/join` | Path appended to base URL for invite links |
| `App:PointsPerCheckIn` | `10` | Reward for a successful daily check-in |
| `App:ProtectionCost` | `50` | Points cost for protecting (or restoring) a streak |
| `App:StartingPointsBalance` | `100` | Granted to brand-new users on `/api/auth/initialize` |
| `App:RestoreWindowHours` | `24` | How long after a missed day a restore is allowed |
| `App:MaxMediaSizeBytes` | `5 * 1024 * 1024` | Upload size cap (5 MB) |
| `App:AllowedMediaContentTypes` | `["image/jpeg","image/png","image/webp"]` | Upload allowlist |
| `App:MediaStorageDirectory` | `Media` | On-disk folder under content root |
| `App:MediaPublicPath` | `/media` | URL prefix where uploads are served |

---

## API surface

All `/api/*` endpoints require `Authorization: Bearer <firebase-id-token>`.

### Auth
| Method | Path | Purpose |
|---|---|---|
| POST | `/api/auth/initialize` | Idempotent — create local user row on first login |
| GET  | `/api/auth/me` | Current user profile |
| POST | `/api/auth/logout` | No-op (Firebase handles session client-side) |

### Streaks
| Method | Path | Purpose |
|---|---|---|
| POST | `/api/streaks` | Create streak (returns invite code + URL) |
| GET  | `/api/streaks` | List streaks I participate in |
| GET  | `/api/streaks/{id}` | Detail with participants, protection state, balance |
| POST | `/api/streaks/join` | Join via invite code (body: `{inviteCode}`) |
| POST | `/api/streaks/{id}/join` | Same, BRD-form |
| GET  | `/api/streaks/{id}/invite` | Get shareable code/URL |

### Check-ins
| Method | Path | Purpose |
|---|---|---|
| POST | `/api/streaks/{id}/check-ins` | Record today's check-in (body: `{note?, mediaUrl?, mediaContentType?}`). Awards points; consumes pending protection if yesterday is the gap. |
| GET  | `/api/streaks/{id}/check-ins/today` | Per-participant "checked in today?" roster |
| GET  | `/api/streaks/{id}/status` | Per-participant current count + today flag |
| GET  | `/api/streaks/{id}/feed?take=20&skip=0` | Recent check-ins **with note or media** for motivation feed |

### Media
| Method | Path | Purpose |
|---|---|---|
| POST | `/api/media/upload` | Multipart `file` upload — returns `{url, contentType, sizeBytes}`. Use the URL in a subsequent check-in body. |
| GET  | `/media/<userId>/<file>` | (Served by static-file middleware, not under `/api`.) |

### Points & protection
| Method | Path | Purpose |
|---|---|---|
| GET    | `/api/users/me/points` | Current balance |
| GET    | `/api/users/me/points/transactions?take=N` | Audit log of points changes |
| POST   | `/api/streaks/{id}/protect` | Pre-activate protection (idempotent) |
| DELETE | `/api/streaks/{id}/protect` | Cancel pending protection |
| POST   | `/api/streaks/{id}/restore` | Restore yesterday's break (within 24h) |
| GET    | `/api/users/me/protections` | List my pending protections |

Errors use `ProblemDetails` with a machine-readable `code` extension (`insufficient_points`, `restore_window_expired`, `nothing_to_restore`, `validation_error`, etc.). Duplicate check-ins return **HTTP 409**.

---

## Authentication flow

1. Frontend signs in via Firebase JS SDK → receives an ID token (JWT).
2. Frontend includes `Authorization: Bearer <token>` on every `/api/*` call.
3. `FirebaseAuthMiddleware` validates the token via Firebase Admin SDK and stashes `FirebaseUid` + `Email` + `Name` into `HttpContext.Items`.
4. `ICurrentUserAccessor` exposes those values to controllers/services.
5. Local user row (in `Users` table) is keyed on `FirebaseUid` — controllers don't trust client-supplied IDs.

`/swagger`, `/health`, and `/media/*` are excluded from auth.

---

## Project conventions

- **Controllers stay thin** — no business logic, just call a service and return.
- **Services live in `Application`** — they take primitives (e.g. `string firebaseUid`, `Guid streakId`) and DTOs, never `HttpContext`.
- **Repositories use `IUnitOfWork`** for SaveChanges — never call `SaveChangesAsync` directly inside repos.
- **Errors flow through custom exception types** (`NotFoundException`, `ConflictException`, `ForbiddenException`, `ValidationException`) handled centrally by `ExceptionHandlingMiddleware` → ProblemDetails.
- **DateOnly for calendar dates** (e.g. `CheckIn.Date`); `DateTime` UTC for timestamps.
- **Dynamic streak count** is computed by `StreakCountCalculator.Compute(checkIns, protectedDays, today)` — never stored.

---

## Adding a new endpoint (recipe)

1. Add a DTO in `StreakPlatform.Application/DTOs/`.
2. Add a method to the service interface in `Application/Interfaces/`.
3. Implement the method in `Application/Services/<X>Service.cs`.
4. If you need a new query, add a method to the repository interface and EF impl.
5. Add a controller action in `Api/Controllers/`.
6. (Optional) update Swagger annotations and tests.

---

## Troubleshooting

| Symptom | Likely cause / fix |
|---|---|
| `A network-related error... while establishing a connection to SQL Server` | Wrong server name. Use `.\SQLEXPRESS`, `(localdb)\MSSQLLocalDB`, or your actual instance. |
| `Login failed for user` | Use `Trusted_Connection=True` (Windows auth) or supply correct `User Id`/`Password`. |
| `FirebaseAuthMiddleware … Invalid Firebase token` | Token expired (1h TTL) or wrong project. Frontend must call `getIdToken(true)` to refresh. |
| `CORS error` in browser | Backend must run at `http://localhost:5000` (matches `Cors:AllowedOrigins`). |
| `Add-Migration` not recognized in Visual Studio PMC | Use external `dotnet ef` instead. The PMC needs `Microsoft.EntityFrameworkCore.Tools` installed *and* the right "Default project" selected. |
| EF migration "no project was found" | Always pass `--project` and `--startup-project` flags. |
| Logs not appearing in file | Check that `backend/StreakPlatform.Api/Logs/` exists and is writable. The app creates it on startup. |
| Migration tries to apply to wrong DB | Connection string in `appsettings.Development.json` overrides the base file in `Development` env. |

---

## Future-ready interfaces

These interfaces exist for swapping implementations without touching callers:

- `IMediaStorage` — currently `LocalMediaStorage`. Swap to Firebase Storage / Azure Blob / S3 by implementing the same interface and registering it in `DependencyInjection`.
- `INotificationService` — empty placeholder for FCM push reminders.
- `IPaymentProvider` (planned) — for the public-streaks + mock payments feature.

---

## License & contact

Internal MVP. Not licensed for redistribution.

For questions about architecture or layering, start with `Application/Services/StreakService.cs` — that's the most representative example of how the layers cooperate.
