# TrackManagement

TrackManagement is a music distribution assessment app. The Angular demo lets reviewers browse tracks, filter by lifecycle status, and inspect delivery status for each digital service provider (DSP). The ASP.NET Core API also supports creating artists and tracks, assigning DSPs, and updating track status.

The [API controllers](src/backend/TrackManagement.Api/Controllers) dispatch commands and queries through MediatR. [Application handlers](src/backend/TrackManagement.Application/Features) use focused artist and track repositories for persistence and reads. The EF Core [Unit of Work](src/backend/TrackManagement.Infrastructure/Persistence/UnitOfWork.cs) saves changes on the shared SQLite context and translates persistence conflicts. [Domain entities](src/backend/TrackManagement.Domain/Entities) hold track lifecycle rules. The [Angular app](src/frontend/src/app) contains the landing page, track list and details, session guard, and HTTP interceptor.

## Prerequisites

- .NET 10 SDK and access to NuGet for restore.
- EF Core CLI 10.0.12, matching the Infrastructure packages.
- Node.js 24 LTS with npm and access to the npm registry.
- PowerShell. SQLite runs in-process.

## Restore, migrate, seed, and start the API

Run from the repository root. Install the EF tool once. If another global version is installed, use `dotnet tool update --global dotnet-ef --version 10.0.12`.

```powershell
dotnet restore TrackManagement.sln
dotnet build TrackManagement.sln --no-restore
dotnet tool install --global dotnet-ef --version 10.0.12

$env:ConnectionStrings__TrackManagement = "Data Source=$([System.IO.Path]::GetTempPath())track-management-dev.db;Foreign Keys=True"
$infrastructure = 'src/backend/TrackManagement.Infrastructure'
dotnet ef database update --project $infrastructure --startup-project $infrastructure --no-build --connection "$env:ConnectionStrings__TrackManagement" -- --seed-sample-data

dotnet dev-certs https --trust
dotnet run --project src/backend/TrackManagement.Api --no-build --launch-profile TrackManagement.Api
```

If PowerShell cannot find `dotnet ef` after installation, reopen the terminal and set the connection string again before running the remaining commands. Approve the local development certificate trust prompt when prompted.

The [launch profile](src/backend/TrackManagement.Api/Properties/launchSettings.json) runs the API in Development at **https://localhost:56364** and also binds HTTP port 56365. The [Angular dev proxy](src/frontend/proxy.conf.json) targets the HTTPS address for `/api/**` and disables certificate verification on that local proxy hop. Browser access to Swagger uses the trusted local certificate.

The [migrations](src/backend/TrackManagement.Infrastructure/Persistence/Migrations) create the schema and then remove the historical TrackHistory table. The opt-in flag loads 3 fictional artists, 8 tracks, 3 DSPs, and 9 assignments. Use the same connection string when starting the API. Omit `-- --seed-sample-data` for schema only.

## Start Angular and try the demo

In a second PowerShell terminal, from the repository root:

```powershell
Set-Location src/frontend
npm ci
npm start
```

1. Open [http://localhost:4200/](http://localhost:4200/) and select **Start demo**.
2. Set **Track status** to **Distributed**. Open **The Long Way Home** to see Pending, Live, and Rejected DSP assignments.
3. Select **Back to tracks** to return to the previous filter and page. To see an empty assignment list, select **All** and open **Paper Satellites**.

Angular calls the anonymous Development-only `POST /api/auth/demo-token`, stores the returned JWT in memory, and attaches it to protected API calls. The token expires after 15 minutes and uses a signing key generated at API startup. Start a new demo after refresh, API restart, or expiration. The demo token grants the two track reads.

## Swagger and write access

Create a separate developer JWT from the repository root. The SDK saves signing material in local user secrets. If the API is running, restart it using the same launch profile and connection string so it loads the signing configuration.

```powershell
Push-Location src/backend/TrackManagement.Api
$token = dotnet user-jwts create --issuer dotnet-user-jwts --audience track-management-api --valid-for 1h --output token --appsettings-file appsettings.Development.json
Pop-Location
$token.Trim() | Set-Clipboard
```

Open [Swagger UI](https://localhost:56364/swagger/index.html), select **Authorize**, paste the raw token, then select **Authorize** in the dialog. Swagger adds the bearer prefix. Try **GET /api/tracks** first. Use the developer token and seeded artist and DSP IDs for write requests. The Development OpenAPI document is at `/openapi/v1.json`. Both the document and Swagger UI are accessible without a token in Development.

| Method | Route | Authorization |
| --- | --- | --- |
| GET | `/api/artists` | Developer JWT |
| POST | `/api/artists` | Developer JWT |
| GET | `/api/tracks?artistId=&genre=&status=` | Demo or developer JWT |
| GET | `/api/tracks/{id}` | Demo or developer JWT |
| POST | `/api/tracks` | Developer JWT |
| POST | `/api/tracks/{id}/distribute` | Developer JWT |
| PATCH | `/api/tracks/{id}/status` | Developer JWT |

## Build and test

From the repository root after the backend build:

```powershell
dotnet test TrackManagement.sln --no-build
```

The xUnit integration tests use isolated SQLite databases. From `src/frontend` after `npm ci`, run `npm run build`. The frontend package has no test script.

See [DECISIONS.md](DECISIONS.md) for the assessment write-up.
