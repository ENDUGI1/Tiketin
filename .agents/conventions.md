# Tiketin — Project Conventions

Authoritative source: the build spec (root prompt). This file distills it plus the
applicable rules from `.agents/skills/`. When a skill conflicts with the spec, the spec wins.

## Language
- UI copy: Bahasa Indonesia.
- Code, comments, commit messages, technical docs (README, XML docs): English.

## Stack (fixed, non-negotiable)
- .NET 8 LTS, ASP.NET Core: Web API controllers + Razor Pages in ONE project (`Tiketin.Web`).
- EF Core 8 + Npgsql, code-first migrations, `UseSnakeCaseNamingConvention()`.
- PostgreSQL 16 via Docker Compose (no Supabase/BaaS).
- ASP.NET Core Identity (`IdentityUser<Guid>`), JWT for API + Cookie for Razor Pages.
  Roles: Employee, Technician, Admin.
- Swashbuckle Swagger, XML doc comments, `[ProducesResponseType]` on every endpoint.
  Swagger UI Development-only; `openapi.json` exported to repo.
- xUnit + FluentAssertions + Testcontainers (Postgres) for integration tests.
- Razor Pages + vanilla CSS (custom tokens) + vanilla JS. No Bootstrap, no Tailwind.
- MailKit + SMTP (Mailtrap dev). Notifications: ticket assigned, ticket resolved.
- GitHub Actions CI (build + test). Multi-stage Dockerfile + docker-compose.prod.yml.

## Architecture
- Thin controllers/pages; ALL business logic in `Services/`.
- DTOs are C# `record` types in `Contracts/` (requests & responses).
- Validation: DataAnnotations only (single approach, consistent everywhere).
- List responses wrap as `{ data, meta }`; errors are RFC 7807 ProblemDetails.
- Domain enums: `TicketStatus` (Open=1, InProgress=2, Resolved=3, Closed=4, Reopened=5),
  `TicketPriority` (Low=1..Critical=4), `TicketEventType`.
- Infrastructure abstractions: `IFileStorage` (local disk `/storage`), `IEmailSender`,
  `ITicketNumberGenerator` (atomic per-month sequence, format `TKT-YYYYMM-0001`).
- Background job: `AutoCloseResolvedTicketsJob` (IHostedService, hourly) closes tickets
  resolved > 7 days ago.
- SLA breach is computed on-read, never stored.

## Business rules (enforced in services, covered by tests)
- Status transitions: Open→InProgress|Resolved; InProgress→Resolved|Open; Resolved→Closed|Reopened;
  Reopened→InProgress|Resolved; Closed is terminal. Validated by `TicketStatusTransitionValidator`.
- `first_response_at` set on first technician non-internal comment or status change.
- Reopen: reporter only, within 7 days of `resolved_at`.
- Rating: reporter only, status Resolved/Closed, write-once.
- `is_internal` comments: Technician/Admin only; hidden from reporters.
- Attachments: max 5 MB, whitelist `image/*` + `application/pdf`.
- Employee sees only own tickets; Technician/Admin see all.

## Design (spec §7 wins over skill defaults — Inter and Lucide are explicitly mandated)
- Internal ops tool ala Linear/Height: dense, fast, quiet. Dark warm-graphite theme only.
- Tokens live in `wwwroot/css/tokens.css` exactly as specified. Single accent (#E8842C),
  used ONLY for primary actions and SLA warnings.
- `--font-mono` (JetBrains Mono) ALWAYS for ticket numbers, timestamps, SLA timers.
- Status = 8px dot + text label, never large colored pills.
- Tables: 40px rows, 13px font, hover `--bg-2`, no heavy zebra striping.
- Sidebar fixed 220px, Lucide icons as static inline SVG (no CDN).
- Empty state = one instruction sentence + one action button. No illustrations.
- Only animations: 120ms hover transitions + subtle pulse on SLA breach.
  Respect `prefers-reduced-motion`.
- Responsive down to 768px; mobile not a priority.

## Applicable skill rules (from .agents/skills/)
- No em-dash (—/–) anywhere in visible UI copy; use commas, periods, or hyphens.
- WCAG AA contrast: body text 4.5:1, large text 3:1 — including buttons, forms,
  placeholders, focus rings, error text.
- Forms: label ABOVE input; helper text present; error text BELOW input;
  never placeholder-as-label; validate on blur; focus first invalid field.
- Implement full UI state cycles: loading, empty, error — not just success.
- One radius scale (6px) everywhere; one icon family (Lucide) with uniform stroke width.
- Seed data: realistic Indonesian names, organic numbers (no 50%/99.99% fakes),
  no "John Doe" equivalents.
- Tabular/monospace figures for data columns and timers (prevents layout shift).
- No emoji as icons. No hand-rolled decorative SVGs beyond simple marks.
- Visible focus states; keyboard navigable; `aria-label` on icon-only buttons.
- Nav: current location highlighted; navigation placement identical on every page.

## Git
- Conventional commits (`feat:`, `fix:`, `chore:`, `test:`, `docs:`, `refactor:`).
- NO AI attribution anywhere: not in commits, co-authors, code comments, or README.
- Small atomic commits per feature; history must tell the build story.
- Work directly on `main`.

## Milestones (each ends buildable + green tests)
M1 scaffold/auth → M2 ticket core → M3 technician flow/SLA → M4 KB & admin → M5 hardening.
