# Decisions

## 1. What did AI generate, and what did I write or modify myself?

I used AI to help shape the assessment design and prompts. I directed the solution name, TrackManagement, chose MediatR, and asked to remove TrackHistory while retaining focused repositories and a Unit of Work. The [solution](TrackManagement.sln), [application features](src/backend/TrackManagement.Application/Features), [persistence contracts](src/backend/TrackManagement.Application/Interfaces), and removal migration in commit `d1873a0` show those decisions in the code.

I added structure to both sides of the application. I organized the backend into Domain, Application, Infrastructure, and API projects. I organized the Angular frontend around routed pages and reusable components, with separate services, a session guard, and a JWT interceptor. I also fixed the authorization issue that blocked demo token generation. The [backend registrations](src/backend/TrackManagement.Infrastructure/DependencyInjection.cs) and [Angular routes](src/frontend/src/app/app-routing.module.ts) show how those pieces fit together.

I still need to identify the specific implementation files or patches AI generated. Git records how the work landed. It does not establish how the code was produced.

## 2. What security issues did I find or introduce, and how did I handle them?

I fixed an authorization configuration bug where `POST /api/auth/demo-token` returned 401 and blocked **Start demo** before Angular could obtain a token. The working implementation uses `AllowAnonymous()` on the token issuance route inside the Development block. The first committed version of the route, `194d3b9`, already contains this configuration. I need the failing revision or configuration to pinpoint which setting caused the 401 and which change resolved it.[Program.cs](src/backend/TrackManagement.Api/Program.cs)

I reviewed the current authorization boundary using the existing [track API tests](tests/backend/TrackManagement.IntegrationTests/Tracks/TrackApiTests.cs) and [artist API tests](tests/backend/TrackManagement.IntegrationTests/Artists/ArtistApiTests.cs). They passed. In Development, anonymous issuance succeeds and sends `Cache-Control: no-store`. Anonymous protected reads receive 401. The demo JWT reads the track list and details and receives 403 on all four write routes. A valid token receives 404 at the issuance path in Production. The anonymous Development Swagger UI and OpenAPI document are separate from the protected business routes. These are current safeguards verified by tests. The JWT setup validates signature, issuer, audience, and expiry. The [demo issuer](src/backend/TrackManagement.Api/Auth/DemoTokens.cs) uses a short-lived token and an in-memory signing key.

I also ran a clean `npm ci` from the committed frontend lockfile. npm reported 16 moderate vulnerability findings, and the production build passed. The findings still need triage to establish which dependencies and workflows are affected. This review made no dependency changes.

## 3. What is one thing AI got wrong that I had to fix, and why was it wrong?

**Prompt scope correction.** The earlier AI-assisted scope omitted both artist routes. I identified that the assessment also required creating and listing artists, so I directed the scope to include `POST /api/artists` and `GET /api/artists`. The [controller](src/backend/TrackManagement.Api/Controllers/ArtistController.cs) landed in `fd084bf`, and the existing [artist API tests](tests/backend/TrackManagement.IntegrationTests/Artists/ArtistApiTests.cs) passed in this review. The original prompt and output are outside the Git record. 
