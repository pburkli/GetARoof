# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

GetARoof is an AI-assisted accommodation search platform. A solo-developer project currently in the architecture/design phase — no implementation code exists yet.

**Tech stack:** ASP.NET Core Web API, Blazor WebAssembly frontend, PostgreSQL (prod) / SQLite (dev), Microsoft Semantic Kernel for AI orchestration, Docker.

## Architecture

**Style:** Modular monolith — domain-partitioned modules with compiler-enforced boundaries, single-container deployment.

**7 logical components:**
- **Intake Agent** — AI-driven pre-search conversation
- **Platform Search** — adapter layer for accommodation APIs (Hotelbeds Phase 1, Amadeus Phase 2)
- **Result Processing** — filtering, POI enrichment, LLM-based ranking
- **Search Orchestration** — workflow coordination across modules
- **Presentation** — Blazor WASM frontend
- **Identity & Accounts** — user management
- **Saved Searches** — persistence for registered users

**Plus 1 lightweight service:**
- **Location Resolution** — geocoding via Nominatim + Overpass API

## Key Design Decisions (ADRs)

All ADRs are in `ARCHITECTURE.md`. The most important:

- **ADR-003:** Platform adapter interface — add new platforms without touching orchestration
- **ADR-004:** Semantic Kernel as AI abstraction — swap AI providers via config only
- **ADR-005:** Hybrid ranking — code filters → POI enrichment → LLM ranking (within 30s budget)
- **ADR-007:** Free-text preferences, not structured enums — no cross-platform taxonomy

## Key Documentation

| File | Content |
|------|---------|
| `ARCHITECTURE.md` | Full architecture: components, ADRs, quality scenarios, constraints |
| `MODELS.md` | Domain model field definitions (SearchRequest, HotelResult, AI contracts) |
| `requirements.md` | Functional/non-functional requirements, use cases, two-phase plan |
| `platforms/overview.md` | Platform API evaluation matrix |
| `platforms/hotelbeds/api.md` | Hotelbeds API specifics |

## Constraints to Preserve

- **30-second end-to-end search budget** (quality scenario S7)
- **Stateless backend** — no shared in-process state; support 50 concurrent sessions (S12)
- **No vendor-specific cloud code** — deploy with `docker compose up`
- **Hotelbeds eval environment:** 50 requests/day limit
- **GDPR compliance** — atomic user data deletion via shared database (ADR-002)

## Diagram Rendering

```bash
# Render PlantUML diagrams to SVG (requires Docker)
docker compose -f diagrams/compose.yaml run --rm render
# Or directly:
diagrams/render.sh
```

C4 model diagrams live in `diagrams/` as `.puml` source files with rendered `.svg` output.
