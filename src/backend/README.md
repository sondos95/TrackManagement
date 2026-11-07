# Track Management backend

Install the .NET 10 SDK and allow NuGet to restore packages from nuget.org or your configured mirror. The backend targets `net10.0`, matching the existing project. SQLite runs in-process.

From the repository root, run these PowerShell commands.

```powershell
dotnet restore TrackManagement.sln
dotnet build TrackManagement.sln --no-restore
dotnet test TrackManagement.sln --no-build
```

## Set up the sample database

From the repository root, install the EF Core CLI version that matches Infrastructure and apply the migrations to a local SQLite file.

```powershell
dotnet tool install --global dotnet-ef --version 10.0.12

$env:ConnectionStrings__TrackManagement = "Data Source=$env:TEMP/track-management-dev.db;Foreign Keys=True"
$infrastructure = 'src/backend/TrackManagement.Infrastructure'
dotnet ef database update --project $infrastructure --startup-project $infrastructure --connection "$env:ConnectionStrings__TrackManagement" -- --seed-sample-data
```

If a different EF CLI version is installed globally, run `dotnet tool update --global dotnet-ef --version 10.0.12`. The migration chain is `20261107044720_InitialTrackManagement` followed by `20261107120000_RemoveTrackHistory`.

The opt-in `--seed-sample-data` flag loads 3 fictional artists, 8 tracks, 3 DSPs, and 9 assignments. Run the same database update command again to keep existing sample rows and fill in any missing sample artists, DSPs, or whole tracks. IDs are fixed, with artist, track, and DSP IDs starting at 10001. If a sample ID or unique ISRC conflicts with unrelated data, use a separate development database. Omit `-- --seed-sample-data` to apply the schema alone.

## Run locally with Swagger and JWT

From the repository root, generate a development JWT valid for one hour.

```powershell
Push-Location src/backend/TrackManagement.Api
$token = dotnet user-jwts create --issuer dotnet-user-jwts --audience track-management-api --valid-for 1h --output token --appsettings-file appsettings.Development.json
Pop-Location
```

In the same PowerShell session, enter `$token` to display it or run the following command to copy it. This also works in Visual Studio's Package Manager Console (`PM>`), where `Set-Clipboard` can fail with an STA error.

```powershell
$token.Trim() | clip.exe
```

Start the API in Development mode. If it was already running when you generated the token, restart it so it can load the development signing configuration.

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/backend/TrackManagement.Api --no-build --no-launch-profile -- --urls http://localhost:5080
```

- **Swagger** — Open [Swagger UI](http://localhost:5080/swagger/index.html), select **Authorize**, and paste the raw token.
- **Postman** — Create a request for `GET http://localhost:5080/api/tracks`, select **Authorization → Bearer Token**, paste the raw token, and send the request.

Both clients add the `Bearer` prefix automatically. If the token expires or you close the session holding `$token`, run the generation command again.

The SDK stores development signing material in local user secrets. Keep the token local and use your deployment's trusted issuer and audience outside Development. The Development OpenAPI document is at `/openapi/v1.json`. The process health endpoint at `/health` requires a valid token.

Use the seeded artists and DSPs when trying track creation and distribution. The integration tests use their own disposable SQLite databases.

## Project responsibilities

| Project | Responsibility |
| --- | --- |
| Domain | Entities and status enums with no project or package references |
| Application | Artist and track MediatR handlers, response models, and persistence contracts |
| Infrastructure | EF Core context, tracked repositories, read projections, unit of work, and SQLite migrations |
| API | Track controller, JWT validation, Development Swagger UI, and dependency composition |
| UnitTests | xUnit tests for Domain and Application, organized by feature under `tests/backend` |
| IntegrationTests | xUnit tests that apply the migration to isolated SQLite databases and verify persistence behavior |

The aggregate trims and uppercases ISRC before persistence. SQLite also computes a normalized value for its unique index. A second unique index prevents assigning the same track to the same DSP twice. Foreign keys restrict deletion of artists, tracks, and DSPs while dependent rows exist. Submission timestamps use UTC.

## Assessment assumptions

- The first DSP assignment moves a track from `Draft` to `Submitted`. An operator explicitly moves a submitted track with at least one assignment to `Distributed`. This decision is independent of per-DSP delivery status, so assignment delivery statuses remain intact. Later assignments start as `Pending` and preserve the track status.

## Track routes

| Method | Route | Success |
| --- | --- | --- |
| POST | `/api/tracks` | `201 Created` with the detail URL in `Location` |
| GET | `/api/tracks?artistId=&genre=&status=` | `200 OK` with filtered track summaries |
| GET | `/api/tracks/{id}` | `200 OK` with track and DSP assignment details |
| POST | `/api/tracks/{id}/distribute` | `200 OK` with updated track and DSP assignment details |
| PATCH | `/api/tracks/{id}/status` | `200 OK` with the resulting track status |

Angular track views will need to send `Authorization: Bearer <token>` for every track request.

## Artist routes

| Method | Route | Success |
| --- | --- | --- |
| POST | `/api/artists` | `201 Created` with the artist DTO |
| GET | `/api/artists` | `200 OK` with artists ordered by name and ID |

Both routes use the same bearer token and Swagger **Authorize** flow described above.
