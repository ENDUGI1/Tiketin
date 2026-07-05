# Tiketin — Build Plan

IT helpdesk ticketing system. ASP.NET Core 8 (Web API + Razor Pages, one project),
PostgreSQL 16 + EF Core 8, Identity (JWT + Cookie), role-based: Employee / Technician / Admin.
Full spec in `.agents/conventions.md`.

## Technical decisions
- **Single web project** (`Tiketin.Web`) + one test project. Clean-ish layering by folder
  (Domain / Data / Services / Contracts / Infrastructure), not by assembly. Avoids
  over-engineering while keeping the service layer API/UI-agnostic.
- **API-first**: Razor Pages call the same `Services/` the API controllers use. No page
  talks to `DbContext` directly.
- **Auth duality**: Identity cookie scheme for Razor Pages, JWT bearer for `/api/v1/*`.
  Policy scheme selects per-request based on path/Authorization header.
- **Ticket numbers**: one Postgres sequence per month, created on demand inside a
  transaction (`CREATE SEQUENCE IF NOT EXISTS seq_ticket_yyyymm`) — atomic, no row locks.
- **SLA on-read**: computed by `SlaService` from `created_at` + category SLA minutes;
  nothing stored, nothing to invalidate. Trade-off documented in README.
- **Validation**: DataAnnotations on request records (no FluentValidation dependency).
- **KB search**: Postgres `tsvector` GIN index, `indonesian` config, via
  `EF.Functions.ToTsVector`.
- **Charts**: Chart.js (self-hosted, no CDN) on the admin dashboard only.
- **Markdown**: Markdig for KB article rendering (sanitized pipeline).

## Milestones
1. **M1 — Scaffold**: solution + projects, docker-compose (Postgres 16 + Mailpit),
   Domain entities + enums, DbContext + snake_case + initial migration (incl. custom
   indexes + GIN), Identity setup (cookie + JWT), role/user/category seeder, base layout
   + tokens.css + sidebar, login page + `/auth/login|refresh` API. Commit(s), build green.
2. **M2 — Ticket core**: TicketService (create/get/list with role scoping, comments,
   attachments, events), TicketNumberGenerator, IFileStorage local disk, API endpoints,
   pages: Tiket Saya / Buat Tiket / Detail Tiket (timeline + comments + rating).
3. **M3 — Technician flow**: status transition validator, assign/self-assign, priority
   change, internal notes, first_response_at logic, SlaService + Antrian page with live
   SLA countdown column, reopen (≤7 days), email notifications (assigned/resolved),
   AutoCloseResolvedTicketsJob.
4. **M4 — KB & Admin**: KbService + full-text search + suggestion-on-create-ticket,
   KB pages (Markdig), ReportService + 4 report endpoints, admin dashboard (stats,
   Chart.js trend + category breakdown, technician table), user management CRUD.
5. **M5 — Hardening**: unit tests (SlaService, transition validator, number generator),
   integration tests (create ticket 201+event, cross-user 403, illegal transition 400),
   GitHub Actions CI, Swagger polish + openapi.json export, seed ~40 tickets + 8 KB
   articles, README (pitch, ERD mermaid, trade-offs), prod Dockerfile + compose.

## Environment notes
- .NET 8 SDK: installed user-local at `%LOCALAPPDATA%\Microsoft\dotnet` (not on system PATH).
- Docker: NOT installed on this machine — required for running Postgres locally and
  Testcontainers integration tests. Build/unit tests work without it.
