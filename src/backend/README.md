# Track Management backend

Install the .NET 10 SDK and allow NuGet to restore packages from nuget.org or your configured mirror. The backend targets `net10.0`, matching the existing project. SQLite runs in-process.

From the repository root, run these PowerShell commands.

```powershell
dotnet restore TrackManagement.sln
dotnet build TrackManagement.sln --no-restore
dotnet test TrackManagement.sln --no-build
$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet run --project src/backend/TrackManagement.Api --no-build -- --urls http://localhost:5080
```

Open `http://localhost:5080/health` for a process health check. Development exposes the OpenAPI document at `http://localhost:5080/openapi/v1.json`. The health check does not access SQLite.

The API configures SQLite via `ConnectionStrings:TrackManagement` in `appsettings.json`. Override it with `ConnectionStrings__TrackManagement`. The default database path is relative to the API process working directory. Schema creation will be handled by migrations in the next stage.

## Project responsibilities

| Project | Responsibility |
| --- | --- |
| Domain | Entities and status enums with no project or package references |
| Application | Future use cases organized under `Artists` and `Tracks`, with focused persistence interfaces as needed |
| Infrastructure | EF Core mappings, SQLite context, and persistence registration |
| API | HTTP pipeline, controller setup, health check, and dependency composition |
| UnitTests | xUnit tests for Domain and Application, organized by feature under `tests/backend` |

SQLite generates a normalized ISRC by trimming surrounding spaces and uppercasing the value. A unique index covers that generated value. A second unique index prevents assigning the same track to the same DSP twice. Foreign keys restrict deletion of artists, tracks, and DSPs while dependent rows exist. Submission timestamps are normalized to UTC.

## Next stage

Unit tests cover track creation defaults, the initial distribution status, and UTC timestamp normalization. Add unit tests for supported and rejected status transitions and duplicate DSP assignment handling alongside those business rules. Add integration tests for EF Core mappings and SQLite constraints once migrations are in place, and for HTTP responses and JWT behavior as the API features are implemented.

Add migrations and seed data, JWT authorization, request validation, and the business use cases. Register the application services once they exist. Add controllers that call those use cases directly for the following planned routes.

| Method | Route |
| --- | --- |
| POST | `/api/artists` |
| GET | `/api/artists` |
| POST | `/api/tracks` |
| GET | `/api/tracks?artistId=&genre=&status=` |
| GET | `/api/tracks/{id}` |
| POST | `/api/tracks/{id}/distribute` |
| PATCH | `/api/tracks/{id}/status` |
