# Track Management

The Angular app opens at [http://localhost:4200/](http://localhost:4200/). In development, Angular proxies demo and track requests under `/api` to `https://localhost:56364`.

## Start the API

Install the .NET 10 SDK. From the repository root, run these PowerShell commands.

```powershell
dotnet build TrackManagement.sln
dotnet tool install --global dotnet-ef --version 10.0.12
$env:Path += ";$env:USERPROFILE/.dotnet/tools"

$env:ConnectionStrings__TrackManagement = "Data Source=$env:TEMP/track-management-dev.db;Foreign Keys=True"
$infrastructure = 'src/backend/TrackManagement.Infrastructure'
dotnet ef database update --project $infrastructure --startup-project $infrastructure --no-build --connection "$env:ConnectionStrings__TrackManagement" -- --seed-sample-data

$env:ASPNETCORE_ENVIRONMENT = 'Development'
dotnet dev-certs https
dotnet run --project src/backend/TrackManagement.Api --no-build --launch-profile TrackManagement.Api
```

Skip the tool installation if `dotnet tool list --global` already shows `dotnet-ef` version 10.0.12. Use `dotnet tool update --global dotnet-ef --version 10.0.12` if another version is installed. See the [backend README](src/backend/README.md) for the separate Swagger and developer JWT workflow.

## Start Angular

Install Node.js 24 LTS with npm. In a second terminal, from the repository root, run:

```powershell
Set-Location src/frontend
npm ci
npm start
```

Open [http://localhost:4200/](http://localhost:4200/) and select **Start demo**. The Development-only `POST /api/auth/demo-token` issues a 15-minute read-only JWT. The API creates its demo signing key in memory at startup. Angular keeps the token in memory and attaches it to protected API calls. Reloading the page, restarting the API, or an expired session requires starting a new demo. When a protected link sends you to the landing page, **Start demo** resumes that link with its filter and page.

## Build and review

Run `npm run build` from `src/frontend` and `dotnet test TrackManagement.sln` from the repository root. The frontend bootstraps `AppModule`, and component templates live alongside their TypeScript files. The [official Takwene logo](https://www.takwene.com/Assets/img/LOGO-T.png) lives in `src/frontend/src/assets/branding`.

The production and development environment files both default to `/api`. The Angular development server uses `src/frontend/proxy.conf.json`. The demo endpoint is available only when the API runs in Development.

On the seeded grid, select **Distributed** and open **The Long Way Home** to see Pending, Live, and Rejected assignments. **Back to tracks** restores the selected filter and page. **Paper Satellites** has no assignments, and `/tracks/999999` shows the missing-track message after starting a demo session.
