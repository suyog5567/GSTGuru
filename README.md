# GST Guru — End-to-End GST Management System

GST Guru is a portfolio-ready, end-to-end demonstration of building a small business GST suite. It covers invoice creation with GST calculation (intra/inter-state, discount, cess), reports/returns-style summaries (GSTR-1, GSTR-3B, HSN summary), and an AI assistant that demonstrates system prompts, few-shot-like behaviour, structured JSON output and an LLM-API-from-code workflow. The frontend is a no-Node, vanilla JavaScript SPA served by ASP.NET Core. Data is stored in SQL Server LocalDB via Entity Framework Core.

> Note: This is an educational/demo project, not a substitute for compliance software. All AI suggestions and HSN rate hints are indicative.

## Highlights
- **GST engine**: correct intra (CGST+SGST) vs inter (IGST) split, discount pre-tax, cess, rupee-roundoff, zero-rated/nil/exempt handling.
- **APIs**: Products, Parties, Invoices, Tax calculator, Reports (dashboard, summary, HSN, GSTR-1, GSTR-3B), Business profile, Chat (offline + pluggable OpenAI/Anthropic/Gemini).
- **AI from code**: provider abstraction (IChatProvider), HttpClient-based integrations, system prompt + structured JSON parsing; offline fallback works without keys.
- **Data**: EF Core + SQL Server LocalDB, migrations, seeding (demo business, products, parties, issued invoices).
- **Reporting**: GSTR-1 B2B/B2C, HSN summary, rate-wise, monthly trend, CSV export for GSTR-1.
- **Testing**: xUnit tests for the GST calculation core.
- **No Node required**: static UI (HTML/CSS/JS) served from wwwroot.

## Tech Stack
- Backend: ASP.NET Core (.NET 10) Web API, EF Core 10, SQL Server LocalDB
- Frontend: Vanilla JS, HTML5, CSS3
- Testing: xUnit
- AI: OpenAI/Anthropic/Gemini HTTP APIs + deterministic offline provider

## Project Structure
- `src/GSTGuru.Web/Domain` — Entities (Product, Party, Invoice, InvoiceLine, BusinessProfile) and enums
- `src/GSTGuru.Web/Gst` — Calculation engine, HSN catalogue, rates, state codes
- `src/GSTGuru.Web/Data` — DbContext, seeder, design-time factory
- `src/GSTGuru.Web/Features` — Controllers/DTOs/services by feature (Invoices, Products, Parties, Tax, Reports, Business, Chat)
- `src/GSTGuru.Web/wwwroot` — Static SPA (HTML/CSS/JS)
- `tests/GSTGuru.Tests` — Core logic tests

## Prerequisites
- .NET 10 SDK (you have 10.0.401)
- SQL Server Express LocalDB (usually present with VS; instance `MSSQLLocalDB`)
- (Optional) API keys: `Ai:ApiKey` for OpenAI/Anthropic/Gemini

## Getting Started
1. Open the solution: `GSTGuru.slnx` (or `dotnet build` from repo root)
2. Run the API: `dotnet run --project src\GSTGuru.Web\GSTGuru.Web.csproj`
3. Open the app: http://localhost:5258 (the default `http` profile). If you use the `https` profile, open https://localhost:7288 — accept the ASP.NET dev certificate first with `dotnet dev-certs https --trust`
4. On first run, EF Core applies migrations and seeds demo data
5. Browse the UI: Dashboard, Invoices (create draft/issue), Products/Parties, Quick Calculator, Reports, AI Assistant

## AI Configuration
Edit `src/GSTGuru.Web/appsettings.json`:
```json
{
  "Ai": {
    "Provider": "Offline",  // Offline | OpenAI | Anthropic | Gemini
    "Model": "gst-guru-offline",
    "ApiKey": "",
    "BaseUrl": "",
    "MaxTokens": 1024,
    "Temperature": 0.2
  }
}
```
- `Offline`: deterministic, works immediately. Demonstrates intent detection + calculation + knowledge base.
- `OpenAI`: set `BaseUrl` (or use default https://api.openai.com/v1), `Model`, `ApiKey`. `response_format=json_object` is requested.
- `Anthropic`: headers `x-api-key`, `anthropic-version: 2023-06-01`. Base https://api.anthropic.com/v1/messages.
- `Gemini`: uses `key` query param. Base https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent.

## Testing
```bash
dotnet test GSTGuru.slnx
```

## Sample Endpoints (curl)
```bash
# Quick intra-state calc
curl -X POST http://localhost:5258/api/tax/calculate -H "Content-Type: application/json" -d '{"sellerStateCode":"27","placeOfSupplyStateCode":"27","lines":[{"description":"Tea","quantity":10,"unitPrice":210,"gstRate":5}]}'

# AI chat
curl -X POST http://localhost:5258/api/chat -H "Content-Type: application/json" -d '{"messages":[{"role":"user","content":"GST on 5000 at 18% intra-state"}]}'

# GSTR-1 CSV
curl -L http://localhost:5258/api/reports/gstr1/export.csv -o GSTR-1.csv
```

## Design Notes (for interviews)
- Clean separation: Domain (entities), Gst (domain rules), Features (use-cases), Data (persistence).
- "LLM API from code, not just chat window": provider interface, HttpClient, system prompt, few-shot, structured JSON output (parse + fallback).
- SQL skills demonstrated: EF Core LINQ (joins/grouping), indexes (unique filtered GSTIN, composite), migrations.
- GST correctness: intra/inter, discount pre-tax, roundoff to nearest rupee.
- Zero-setup DB via LocalDB; seeded data makes demos concrete.

## Known Limitations
- State list is indicative; place-of-supply edge cases (export, SEZ) kept minimal for scope.
- Chat providers require working keys and outbound internet.
- UI is vanilla JS (no SPA framework) — simple and no build step.

## Deployment
Deployment is intentionally **not** included per your request. The app is meant to run locally end-to-end.
