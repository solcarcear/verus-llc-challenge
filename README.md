# Company Management

Implementation of the Verus LLC coding challenge: a small application for creating and searching companies.

The solution consists of:

- An ASP.NET Core Web API backend
- An Angular frontend
- In-memory persistence (no database)

## Features

**Backend**

- Create a company (name + website URL)
- Structural validation of company name and website URL
- Deterministic company/website relevance validation
- Retrieve all companies
- Retrieve a company by ID
- Search companies by name or website, with relevance-based result ordering

**Frontend**

- Reactive form for creating a company, with client-side validation and backend error display
- Company list with loading, error, and empty states
- Search UI that delegates matching/ordering to the backend
- Responsive layout with plain CSS
- Automated tests for services and components

## Tech Stack

**Backend**
- .NET 8 / ASP.NET Core Web API (`net8.0`)
- C#
- xUnit, `Microsoft.AspNetCore.Mvc.Testing` (API integration tests)
- Swashbuckle (Swagger/OpenAPI, development only)

**Frontend**
- Angular 22 (standalone components, application builder)
- TypeScript
- Reactive Forms
- Signals (local component state)
- `HttpClient`
- Vitest (`@angular/build:unit-test`) for tests

## Project Structure

```
verus-llc-challenge/
├── backend/
│   ├── src/
│   │   ├── CompanyManagement.Api/            # Controllers, DI wiring, error handling
│   │   ├── CompanyManagement.Application/    # Services, validation, relevance, search
│   │   ├── CompanyManagement.Domain/         # Company entity
│   │   └── CompanyManagement.Infrastructure/ # In-memory repository
│   └── tests/
│       ├── CompanyManagement.UnitTests/
│       └── CompanyManagement.ApiTests/
├── frontend/
│   └── src/app/
│       ├── core/                             # API config, models, CompanyService
│       └── features/companies/               # CompanyForm, CompanyList, CompanySearch
└── README.md
```

## Architecture

**Backend** follows a layered structure with a strict dependency direction: `Api → Application → Domain`, with `Infrastructure` implementing abstractions defined in `Application`.

- **Domain** — the `Company` entity. No dependencies on other layers.
- **Application** — `CompanyService` (orchestration), `CompanyValidator`, `CompanyRelevanceEvaluator`, and the `ICompanyRepository` abstraction. Contains all business logic.
- **Infrastructure** — `InMemoryCompanyRepository`, the concrete implementation of `ICompanyRepository`.
- **Api** — `CompaniesController`, DTOs (`Contracts/`), DI configuration, CORS, and centralized exception handling (`GlobalExceptionHandler`). Controllers only translate between HTTP and `ICompanyService`; they contain no business rules.

```
Controller → ICompanyService → ICompanyValidator
                              → ICompanyRelevanceEvaluator
                              → ICompanyRepository → InMemoryCompanyRepository
```

**Frontend** splits responsibility across the root component and three feature components, all communicating through `CompanyService`:

- **App** — owns the company list, loading/searching/error state (signals), and coordinates between search and list.
- **CompanyForm** — reactive form, client-side validation, calls `CompanyService.create`, emits the created company.
- **CompanySearch** — emits the trimmed search query or a clear event; no HTTP calls.
- **CompanyList** — pure presentation component (`input()`), renders companies or an empty message.
- **CompanyService** — the only class that talks to the backend via `HttpClient`.

## Running the Application

Prerequisites:
- .NET 8 SDK
- Node.js with npm (Angular CLI 22 requirements)

### Backend

```bash
cd backend
dotnet restore
dotnet run --project src/CompanyManagement.Api
```

The API is available at:

- `http://localhost:5000`
- `https://localhost:7000`

In the `Development` environment, Swagger UI is served at `/swagger` on either URL.

### Frontend

```bash
cd frontend
npm install
npm start
```

The Angular app runs at `http://localhost:4200` and is configured to call the backend at `http://localhost:5000/api` (see `src/app/core/config/api.config.ts`). The backend's development CORS policy explicitly allows `http://localhost:4200`.

## API Endpoints

| Method | Endpoint | Description | Success |
|--------|----------|-------------|---------|
| POST | `/api/companies` | Create a company | `201 Created` |
| GET | `/api/companies` | List all companies | `200 OK` |
| GET | `/api/companies?search={query}` | Search companies by name or website, ordered by relevance | `200 OK` |
| GET | `/api/companies/{id}` | Retrieve a company by ID | `200 OK` |

- `POST /api/companies` returns `400 Bad Request` with an `ApiErrorResponse` (`message` + `errors[]`) if structural validation fails or if the company name is not relevant to the website.
- `GET /api/companies/{id}` returns `404 Not Found` if the ID does not exist.

Example create request:

```json
{
  "name": "Microsoft Corporation",
  "websiteUrl": "https://www.microsoft.com"
}
```

## Validation Strategy

### Structural validation

Implemented in `CompanyValidator`:

- **Company name** — required, and must be at least 3 characters (after trimming).
- **Website URL** — required, and must parse as an absolute, well-formed URL with an `http` or `https` scheme.

### Company / Website Relevance

Implemented in `CompanyRelevanceEvaluator`. This is a deterministic, offline check — no HTTP requests are made to the submitted website.

The evaluator normalizes both the company name and the website host into lowercase, alphanumeric tokens (stripping common corporate suffixes such as "Inc", "LLC", "Corp", "Group", and the `www` subdomain), then compares them:

| Match | Condition | Score |
|-------|-----------|-------|
| Exact | Normalized name equals normalized domain | 100 |
| Containment | One normalized identity contains the other (min. 3 characters) | 80 |
| Token | At least one shared token of 3+ characters | 60 |
| None | No overlap | rejected |

Examples:
- `"Microsoft Corporation"` + `https://www.microsoft.com` → exact match, 100 (the "corporation" suffix and "www" are stripped)
- `"Acme Global"` + `https://acme.com` → containment match, 80 (domain identity is a prefix of the company identity)
- `"Acme Bottling Co"` + `https://bottling-acme.com` → token match, 60 (shares the token "acme", different order)
- `"Microsoft Corporation"` + `https://apple.com` → rejected, no relevance

This approach was chosen because it is deterministic, fast, offline, easy to test, has no external API dependency, and avoids making HTTP requests to arbitrary user-supplied URLs. The trade-off is that it is intentionally heuristic: it can reject legitimate companies whose brand name and domain differ substantially (e.g. a company legally named differently from its product domain). Content inspection or an external verification service could improve accuracy, but would introduce network latency, availability concerns, and security considerations (fetching arbitrary user-supplied URLs).

The evaluator has known, documented limitations: multi-part TLDs (e.g. `.co.uk`) are not special-cased, and non-`www` subdomains are treated as part of the domain identity.

## Search

Search is performed entirely by the backend (`CompanyService.SearchCompaniesAsync`); the Angular app only sends the query string and renders the ordered results it receives.

For each stored company, the query is scored independently against the company name and the normalized domain (host with a leading `www.` stripped), and the higher of the two scores is used:

| Match type | Name score | Domain score |
|------------|-----------|--------------|
| Exact match | 100 | 90 |
| Starts with | 80 | 70 |
| Contains | 60 | 50 |

Results with a score of 0 are excluded. Remaining results are ordered by score descending, then by name alphabetically. An empty or missing query returns all companies unsorted; a query that matches nothing returns an empty list (`200 OK`, not an error).

## Persistence

Persistence is intentionally in-memory, as required by the challenge. `InMemoryCompanyRepository` implements `ICompanyRepository` using a `ConcurrentDictionary<Guid, Company>` for thread-safe access across concurrent requests, and is registered as a singleton so data survives across requests within the process lifetime.

The repository is accessed only through the `ICompanyRepository` abstraction, so it could be replaced with a database-backed implementation without changing the controller or application layer.

**Data is lost when the backend process restarts.**

## Error Handling

- `201 Created` on successful company creation (with a `Location` header via `CreatedAtAction`)
- `200 OK` on successful retrieval or search
- `400 Bad Request` for structural validation failures and relevance failures, with a structured `ApiErrorResponse`
- `404 Not Found` when a requested company ID does not exist
- Unexpected exceptions are caught centrally by `GlobalExceptionHandler`, logged via `ILogger`, and returned as a generic `500` `ProblemDetails` response with no internal exception details exposed to the client

## Frontend Design

- Standalone Angular components throughout, no NgModules
- Reactive Forms for the company creation and search forms
- Signals for local UI state (loading, searching, error message, active search query) in `App` and `CompanyForm`
- Parent/child communication via `input()` / `output()` (e.g. `CompanyList` inputs, `CompanyForm`/`CompanySearch` outputs)
- Business validation (relevance) is not duplicated on the frontend; the backend response is authoritative and its errors are surfaced to the user
- Plain, responsive CSS with no UI framework

## Testing

**Backend** (`dotnet test` from `backend/`):

- `CompanyManagement.UnitTests` — `CompanyValidator` (structural rules), `CompanyRelevanceEvaluator` (match/score cases), `CompanyService` (creation, search scoring/ordering), `InMemoryCompanyRepository`, `Company` domain equality
- `CompanyManagement.ApiTests` — end-to-end HTTP behavior via `WebApplicationFactory`: creation (`201`/`400`), relevance rejection, retrieval by ID (`200`/`404`), search (name, domain, relevance ordering, empty results), and the development CORS policy

```bash
cd backend
dotnet test
```

**Frontend** (`ng test`, using the Vitest-based `@angular/build:unit-test` builder, which runs headlessly and exits on completion):

- `CompanyService` — HTTP calls for `getAll`, `getById`, `create`, `search`
- `CompanyForm` — validation, submission, backend error display, duplicate-submission prevention
- `CompanyList` — rendering, empty state, custom empty message
- `CompanySearch` — query emission, clearing on blank submit
- `App` — orchestration of loading/search/error state and company list updates

```bash
cd frontend
npm test
```

## Engineering Decisions and Trade-offs

- Clean layer separation (Domain/Application/Infrastructure/Api) without introducing CQRS or MediatR, which would add ceremony without benefit at this scale.
- A repository abstraction (`ICompanyRepository`) is used even though the only implementation is in-memory, so persistence can be swapped later without touching controllers or business logic.
- Company/website relevance is validated with deterministic token/domain matching rather than fetching the target website, avoiding latency, availability, and security risks tied to requesting arbitrary user-supplied URLs.
- Search matching and relevance ordering live entirely in the backend; the frontend is a thin consumer of the ordered results.
- Angular signals are used for local component state instead of a global state management library, which would be disproportionate for this application's size.
- No external UI framework; plain CSS keeps the frontend dependency surface small.
- Both `http://localhost:5000` and `https://localhost:7000` are exposed, as required by the challenge.
- The development CORS policy is restricted to `http://localhost:4200`.

## Future Extensions

- Update and delete endpoints
- Pagination and sorting for company listing
- Database-backed repository implementation
- More sophisticated relevance verification
- Production environment configuration (CORS origins, HTTPS certificates, logging)

## Notes

- In-memory data resets whenever the backend process restarts.
- Default local ports: backend `5000`/`7000`, frontend `4200`.
- Swagger UI is available only in the `Development` environment, at `/swagger`.
