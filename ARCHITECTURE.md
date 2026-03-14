# GetARoof — Architecture

This document defines the architecture for GetARoof. It follows a reasoning-first structure: quality attributes, constraints, and stakeholders (Section 1), logical components with context and scope (Section 2), architectural style (Section 3), concrete design decisions (Section 4), architectural decision records (Section 5), architecture diagrams (Section 6), cross-cutting concepts (Section 7), risks and technical debt (Section 8), and glossary (Section 9).

**Phase references:** Phase 1 (MVP, Hotelbeds only) and Phase 2 (multi-platform, adds Amadeus) are defined in [requirements.md](requirements.md).

---

## 1. Architectural Characteristics

This section defines the quality attributes that drive architectural decisions, ranked by importance, with measurable definitions and concrete scenarios.

### 1.1 Ranked Characteristics

| Priority | Characteristic | Description |
|---|---|---|
| **Critical** | Maintainability / Extensibility | The system must support adding new platform adapters and swapping AI providers without changes to core logic. This is the primary structural driver. |
| **Critical** | Interoperability | The system is an integration layer over multiple external APIs with different auth models, data formats, and rate limits. Each integration must be isolated. |
| **Critical** | Reliability / Resilience | External APIs will fail. Failures must not cascade, hang silently, or block results from other sources. |
| **Important** | Performance | The full search pipeline (platform API + POI enrichment + AI ranking) must complete within 30 seconds. Requires parallel execution. |
| **Important** | Security | Real user credentials and API keys must be protected. Modest attack surface (no payments), but GDPR applies. |
| **Important** | Deployability | Containerized, cloud-agnostic deployment. Must run locally with Docker and a single config file. |
| **Important** | Scalability | MVP targets 50 concurrent search sessions. Architecture must not block future commercial scaling — stateless backend, no in-process shared state. |
| **Desirable** | Accessibility | WCAG 2.1 Level AA. Required by the spec but primarily a frontend implementation concern, not an architecture shaper. |

### 1.2 Definitions and Measures

**Maintainability / Extensibility**
- Adding a new platform adapter requires no modifications to search orchestration, ranking, or UI code. New adapter = implement interface + register it.
- Changing AI provider requires only configuration and potentially a new Semantic Kernel connector — no changes to business logic code.

**Interoperability**
- Each external API integration is isolated behind its own adapter. A breaking change in one platform's API requires changes only within that platform's adapter.

**Reliability / Resilience**
- If a platform API times out or errors, the system returns results from remaining platforms (Phase 2) or a clear error message (Phase 1) within 5 seconds of detecting the failure.
- Malformed AI response triggers one retry; if retry fails, user sees results (price-sorted fallback) or error within 3 seconds. No silent hangs.

**Performance**
- End-to-end search results returned within 30 seconds of query submission.
- AI intake agent responds within 3 seconds per conversational turn.
- CheckRate verification completes within 5 seconds of user selecting a property.

**Security**
- All communication over HTTPS.
- API keys never present in client-side code, bundles, or network responses.
- Passwords stored as salted hashes. Auth tokens short-lived with refresh rotation.
- User can request deletion of all personal data, fulfilled within 72 hours (GDPR).

**Deployability**
- Application runs via `docker compose up` with no cloud-specific SDK calls in application code.
- A new developer can run the full system locally with Docker and a single configuration file for API keys, within 5 minutes.

**Scalability**
- Backend supports running multiple instances behind a load balancer with no shared in-process state (stateless API).
- MVP target: 50 concurrent search sessions without dropped requests.

**Accessibility**
- WCAG 2.1 Level AA conformance, verifiable via automated audit tools (axe, Lighthouse) plus manual review.

### 1.3 Constraints

**Technology**

| Layer | Technology |
|-------|-----------|
| Frontend | Blazor WebAssembly |
| Backend | ASP.NET Core Web API |
| AI Orchestration | Microsoft Semantic Kernel |
| Database | PostgreSQL (production); SQLite acceptable for local development |
| Containerization | Docker + Docker Compose |
| Hosting | Cloud-agnostic; any container-capable host |

Key architectural constraint: no vendor-specific cloud services in application code.

**Organizational**
- Solo developer — limits parallelism of work, operational capacity, and on-call coverage
- AI-assisted development — increases individual throughput but does not eliminate the single-person bottleneck for design decisions and debugging
- No existing infrastructure or DevOps team — deployment and operations must be simple enough for one person to manage

**Legal**
- GDPR compliance required for European users (data minimization, right to deletion, consent)
- No payment processing (PCI-DSS scope is zero)

**Schedule**
- No hard deadline, but solo-dev context favors incremental delivery — Phase 1 (MVP) should be shippable before adding multi-platform complexity

**Cost**
- Hotelbeds evaluation environment: 50 requests/day limit
- External API calls (AI provider, platform APIs) scale linearly with usage — cost grows with users
- No budget for paid infrastructure during development (favors free-tier and open-source: Overpass API, Nominatim, SQLite locally)

### 1.4 Trade-offs

**Maintainability vs. time-to-market** — Clean adapter abstractions take more upfront effort than hardcoding integrations directly. Since Phase 2 explicitly adds Amadeus, cutting corners means rewriting later. *Resolution: invest in abstractions from the start. Accept slower initial delivery.*

**Performance vs. reliability** — The 30-second budget must cover platform API + POI enrichment + AI ranking. Aggressive timeouts improve responsiveness but risk discarding valid-but-slow results. *Tension remains: fast timeouts vs. completeness.*

**Performance vs. maintainability** — Parallel execution (platform calls, POI queries) is needed to fit within 30 seconds but adds orchestration complexity. Sequential processing would be simpler. *Resolution: parallel execution is necessary. Keep complexity contained in the orchestration layer.*

**Security vs. deployability** — Proper secrets management conflicts with simple `docker compose up`. *Resolution: environment variables for local dev, document production secrets management as a deployment concern without baking in a specific vault provider.*

**Scalability vs. cost** — Each search session triggers multiple paid API calls. Scaling users linearly scales external API costs. Caching helps but availability data is time-sensitive. *Tension remains: no easy architectural fix — this is a business model concern.*

**Interoperability vs. reliability** — More external APIs improve results but increase failure surface area. *Resolution: treat platform failures as normal, not exceptional. Partial results are acceptable.*

### 1.5 Quality Scenarios

**S1 — Add a new platform adapter (Maintainability)**
A developer integrates the Amadeus API for Phase 2. They implement the platform adapter interface and register it. No files outside the new adapter and its registration are modified. Search orchestration, ranking, and UI code remain unchanged.

**S2 — Swap AI provider (Maintainability)**
The developer switches from OpenAI to Anthropic for the ranking LLM. The change requires updating Semantic Kernel configuration and adding/swapping a connector. No business logic code changes. Prompt templates may need adjustment but orchestration code does not.

**S3 — Platform API breaking change (Interoperability)**
Hotelbeds changes their availability response schema. Only the Hotelbeds adapter is updated. Domain models, orchestration, ranking, and all other adapters remain untouched. No regression in other platform results.

**S4 — Platform API timeout, single platform (Reliability)**
Hotelbeds does not respond within the timeout threshold during a Phase 1 search. The system detects the timeout and displays a clear error message suggesting the user retry. The user sees the error within 5 seconds of timeout detection. No silent hang.

**S5 — One platform fails during multi-platform search (Reliability)**
Amadeus returns HTTP 500 during a Phase 2 search. The system returns results from Hotelbeds with a notice that Amadeus results are unavailable. Total response time is not significantly affected.

**S6 — AI ranking fails (Reliability)**
The LLM returns malformed JSON for ranking. The system retries once with a corrective prompt. If the retry also fails, results are displayed sorted by price (fallback). The user sees results within 5 seconds of the fallback trigger.

**S7 — Full search pipeline (Performance)**
A user submits a confirmed search for 2 rooms in Zürich with a location constraint. The pipeline executes: Hotelbeds API call → hard-constraint filtering → POI enrichment for ~20 candidates → AI ranking. Results are displayed within 30 seconds of submission.

**S8 — AI intake response time (Performance)**
A user sends a message in the intake conversation. The AI intake agent responds with a follow-up question or confirmation card within 3 seconds.

**S9 — API key protection (Security)**
A malicious user inspects browser network traffic and client-side code. No API keys, secrets, or platform credentials are present in any client-side response, JavaScript bundle, or network call.

**S10 — User data deletion (Security / GDPR)**
A registered user requests deletion of their account. All personal data (profile, saved searches) is permanently deleted within 72 hours. No personal data remains in the database.

**S11 — Local development setup (Deployability)**
A new developer clones the repo and runs `docker compose up` with a single configuration file for API keys. The full system is running within 5 minutes. No cloud account required.

**S12 — Concurrent search load (Scalability)**
50 users submit search requests simultaneously against a horizontally scaled backend. All requests complete within 30 seconds. No requests are dropped. No shared in-process state between backend instances.

### 1.6 Stakeholders

| Role | Person | Expectations |
|---|---|---|
| Solo developer, architect, operator | Project owner | Full architecture documentation sufficient to resume work after breaks. Design decisions traceable to quality goals. Deployment simple enough to operate alone. |

---

## 2. Logical Components

This section defines the major functional building blocks of the system, their responsibilities, interactions, and data ownership. These are logical components — they represent responsibilities, not deployment units or code modules.

### 2.1 Component List

The system consists of seven components and one lightweight service:

| Component | Subdomain | Description |
|---|---|---|
| Intake Agent | Search (core) | Owns the pre-search conversation with the user |
| Platform Search | Search (core) | Integrates with accommodation platform APIs via adapters |
| Result Processing | Search (core) | Filters, enriches, and ranks search results |
| Search Orchestration | Search (core) | Coordinates the end-to-end search workflow |
| Presentation | Search (core) | All user-facing UI rendering |
| Identity & Accounts | User Management (supporting) | User registration, authentication, account lifecycle |
| Saved Searches | Persistence (supporting) | Stores and retrieves past searches for registered users |
| Location Resolution | Search (core) — lightweight service | Resolves free-text destinations to geocoded locations |

Location Resolution is a lightweight service rather than a full component — it has no domain model, no state, and no data ownership. It exists as a separate unit because of the interoperability characteristic: it wraps an external geocoding API (Nominatim) behind an interface that can be swapped.

### 2.2 Responsibility Definitions

**Intake Agent**
- **Owns:** The pre-search conversation — all messages between user and AI before search execution
- **Does:** Parse natural language input, assess completeness, ask follow-up questions, judge when criteria are specific enough, produce a confirmed `SearchRequest` JSON
- **Does not:** Execute searches, resolve locations, validate against platform capabilities
- **Interfaces:** Accepts user messages, returns AI responses or a confirmed `SearchRequest`

**Platform Search**
- **Owns:** All platform-specific API integration logic — one adapter per platform
- **Does:** Translate `SearchRequest` + resolved location into platform-specific API calls. Call availability and content APIs. Map platform-specific responses into the common `HotelResult` model. Execute CheckRate verification for a selected offer.
- **Does not:** Filter, rank, or score results. Handle POI enrichment. Decide which platforms to query.
- **Interfaces:** Accepts `SearchRequest` + resolved location, returns `HotelResult[]`. Accepts offer ID for CheckRate, returns verified availability.

**Result Processing**
- **Owns:** The pipeline that transforms raw platform results into ranked, enriched results
- **Does:** Apply hard-constraint filters (budget, star rating). Query POI service for nearby points of interest when a location constraint is present. Send candidates to AI for scoring and match explanation generation. Return ranked results with scores and explanations.
- **Does not:** Call accommodation platform APIs. Own the conversation with the user. Decide when to trigger the pipeline.
- **Interfaces:** Accepts `SearchRequest` + `HotelResult[]`, returns ranked and enriched `HotelResult[]`

**Search Orchestration**
- **Owns:** The end-to-end search workflow and coordination between components
- **Does:** Receive confirmed `SearchRequest` from Presentation (originally produced by Intake Agent). Call Location Resolution. Dispatch to Platform Search (parallel in Phase 2). Pass results through Result Processing. Handle component failures — decide whether to return partial results or error messages. Construct external booking link.
- **Does not:** Parse natural language. Call platform APIs directly. Score or rank results. Manage user accounts.
- **Interfaces:** Accepts confirmed `SearchRequest`, returns ranked `HotelResult[]`. Accepts hotel selection, returns verified availability + booking URL.

**Presentation**
- **Owns:** All user-facing UI rendering
- **Does:** Display the chat interface for the Intake Agent conversation. Show progress indicator during search. Render ranked result cards (photos, price, match summary). Display property detail view (full description, photos, room breakdown, map, booking link). Display saved searches for logged-in users.
- **Does not:** Run business logic. Call external APIs directly. Make ranking or filtering decisions.
- **Interfaces:** Consumes backend API responses. Emits user actions (submit message, confirm search, select property, save search).

**Identity & Accounts**
- **Owns:** User credentials, authentication state, and account lifecycle
- **Does:** Register new users (email/password). Authenticate users and issue tokens. Manage token refresh and session lifecycle. Support account deletion (GDPR).
- **Does not:** Store search data. Make authorization decisions beyond authenticated/anonymous.
- **Interfaces:** Registration, login, logout, token refresh, account deletion endpoints

**Saved Searches**
- **Owns:** The association between users and their past searches
- **Does:** Persist a completed search (`SearchRequest` + `HotelResult[]` snapshot) linked to a user account. List and retrieve saved searches. Delete saved searches.
- **Does not:** Execute searches. Manage user accounts or authentication. Re-rank or refresh saved results.
- **Interfaces:** Save, list, get, delete endpoints (all require authenticated user)

**Location Resolution** (lightweight service)
- **Owns:** The mapping from free-text destinations to geocoded locations
- **Does:** Convert destination strings ("Lake Lucerne area", "Zürich") into geocoded coordinates (lat/lng, bounding box)
- **Does not:** Decide what destination to search. Query accommodation platforms. Persist any data. Produce platform-specific location codes — that mapping is the responsibility of each platform adapter.
- **Interfaces:** Accepts destination string, returns structured location data (coordinates, display name)

### 2.3 Context and Scope

#### Business Context

The system as a black box — who communicates with it and what data crosses the boundary.

| Communication Partner | Input (to system) | Output (from system) |
|---|---|---|
| User (anonymous) | Natural language accommodation requirements, search confirmation, hotel selection | AI conversation responses, ranked hotel results with photos/pricing/maps, verified availability + booking link |
| User (registered) | Registration/login credentials, save/retrieve requests | Auth tokens, saved search list |
| Hotelbeds API | Availability search results, hotel content (photos, descriptions, facilities), CheckRate verification | Search parameters (destination, dates, occupancy), content queries, CheckRate requests |
| Amadeus API *(Phase 2)* | Availability search results, hotel content | Search parameters (destination, dates, occupancy) |
| AI Provider (via Semantic Kernel) | Intake conversation responses, ranking scores + explanations, POI category extraction | Conversation messages, hotel candidate data + SearchRequest for ranking, location constraint text for POI category extraction |
| Nominatim | Geocoded coordinates (lat/lng, bounding box, display name) | Destination string (free-text geocoding query) |
| Overpass API | Nearby POI data (names, categories, distances) | OverpassQL radius queries (hotel coordinates + POI categories) |
| Google Hotels | — | *(outbound link only — no API call, constructed as URL for user)* |

All external API communication is server-side. The browser communicates only with the GetARoof backend.

#### Technical Context

| Channel | Protocol | Notes |
|---|---|---|
| Browser ↔ Backend | HTTPS (REST API) | Blazor WASM SPA calls ASP.NET Core endpoints. All client communication over this single channel. |
| Backend → Hotelbeds | HTTPS (REST) | API key + secret in headers. Rate-limited (50 req/day eval). |
| Backend → Amadeus *(Phase 2)* | HTTPS (REST + OAuth2) | Client credentials grant for token. |
| Backend → AI Provider | HTTPS (REST) | Via Semantic Kernel connector. API key auth. |
| Backend → Nominatim | HTTPS (REST) | No auth. Respect usage policy (1 req/s, User-Agent). |
| Backend → Overpass API | HTTPS (REST/POST) | No auth. OverpassQL queries. |
| Backend → PostgreSQL | TCP (PostgreSQL wire protocol) | Connection string via environment variable. |

### 2.4 Interaction Map

#### Search Flow

```
Presentation → Intake Agent
  User message (text)
  ← AI response (text) or confirmed SearchRequest (JSON)

Search Orchestration → Location Resolution
  SearchRequest.Destination (string)
  ← Resolved location (coordinates, display name)

Search Orchestration → Platform Search  [one call per platform, parallel in Phase 2]
  SearchRequest + resolved location
  ← HotelResult[]

Search Orchestration → Result Processing
  SearchRequest + HotelResult[] (combined from all platforms)
  ← Ranked, enriched HotelResult[]

Search Orchestration → Presentation
  ← Ranked HotelResult[] + progress updates during search

Presentation → Search Orchestration
  Selected hotel + offer ID (user picks a property)
  ← Verified availability + booking URL

Search Orchestration → Platform Search
  CheckRate request (offer ID)
  ← Verified price/availability
```

#### Account Flow

```
Presentation → Identity & Accounts
  Register / Login / Logout / Delete account

Presentation → Saved Searches
  Save search / List saved / View saved / Delete saved
  (all require authenticated user ID from Identity & Accounts)
```

#### External System Dependencies

```
Intake Agent        → AI Provider (via Semantic Kernel)
Result Processing   → AI Provider (via Semantic Kernel)
Result Processing   → Overpass API (POI queries)
Platform Search     → Hotelbeds API (Phase 1)
Platform Search     → Amadeus API (Phase 2)
Location Resolution → Nominatim (geocoding)
```

### 2.5 Data Ownership

| Data | Owner | Consumers | Persistence |
|---|---|---|---|
| Conversation history (pre-search messages) | Intake Agent | Presentation | Transient (per-session) |
| `SearchRequest` | Intake Agent (produces) | Search Orchestration, Platform Search, Result Processing, Saved Searches. Location Resolution receives only the `Destination` field. | Transient in search flow; snapshot in Saved Searches |
| `HotelResult[]` | Platform Search (produces), Result Processing (enriches) | Search Orchestration, Presentation, Saved Searches | Transient in search flow; snapshot in Saved Searches |
| User accounts (email, hashed password, metadata) | Identity & Accounts | Saved Searches (user ID reference), Presentation (auth state) | Persistent (database) |
| Auth tokens | Identity & Accounts | Presentation | Short-lived (token store) |
| Saved search records | Saved Searches | Presentation | Persistent (database) |
| API credentials and provider config | Configuration (cross-cutting) | Platform Search, Intake Agent, Result Processing, Location Resolution | Persistent (environment variables / config files) |

### 2.6 Boundary Issues and Risks

| Risk | Severity | Mitigation |
|---|---|---|
| `SearchRequest` touched by 6 components | Moderate | Treat as a stable contract. Changes require deliberate cross-component review. |
| `HotelResult` mutated by 3 components in sequence (Platform Search creates, Result Processing enriches, Search Orchestration adds booking URL) | Low | Pipeline order enforced by Search Orchestration. Do not allow writes outside the pipeline. |
| Saved Searches stores `SearchRequest` + `HotelResult` snapshots — schema changes could make old data unreadable | Low | Acceptable for MVP. Add schema versioning if models evolve after users have saved searches. |
| Search Orchestration is a hub (touches 4 other components: Location Resolution, Platform Search, Result Processing, Presentation) | Low | Keep it thin — workflow coordination and error handling only. No domain logic. If business rules creep in, push them to the appropriate component. |
| Result Processing has two distinct external dependencies (Overpass API, AI Provider) with different failure modes | Low | Already addressed by internal step structure (Section 4.3). Each step handles its own failures independently. |

---

## 3. Architectural Style

### 3.1 Chosen Style

**Modular Monolith** — domain-partitioned modules, monolithic deployment.

Each logical component from Section 2 becomes an internal module with explicit boundaries enforced by .NET project references and `internal` access modifiers. The system deploys as a single container.

For rationale, rejected alternatives, and trade-offs, see [ADR-001](#adr-001-modular-monolith-as-architectural-style).

### 3.2 Scenario Validation

All 12 quality scenarios from Section 1.5 were walked through against the modular monolith. All pass without workarounds or compromises.

| Scenario | Result | Notes |
|---|---|---|
| S1 — Add platform adapter | Pass | New project in Platform Search module, implement interface, register. No other modules touched. |
| S2 — Swap AI provider | Pass | Configuration + connector change within AI-consuming modules. No business logic changes. |
| S3 — Platform API breaking change | Pass | Only the affected adapter project changes. Common `HotelResult` contract unchanged. |
| S4 — Platform timeout (Phase 1) | Pass | In-process error handling in Search Orchestration. Clear error to user within 5 seconds. |
| S5 — One platform fails (Phase 2) | Pass | Parallel adapter calls with independent error handling. Partial results returned. |
| S6 — AI ranking fails | Pass | Retry + price-sort fallback contained within Result Processing. |
| S7 — Full pipeline under 30s | Pass | In-process orchestration adds negligible overhead. Latency budget spent on external I/O. |
| S8 — Intake response under 3s | Pass | Direct call to Intake Agent module. Latency dominated by AI provider. |
| S9 — API key protection | Pass | Single backend process. Secrets in environment variables, never sent to client. |
| S10 — GDPR data deletion | Pass | Single database, atomic transaction across Identity & Saved Searches. |
| S11 — Local dev setup | Pass | Single app container + PostgreSQL. `docker compose up` within 5 minutes. |
| S12 — 50 concurrent sessions | Pass | Stateless backend, horizontal scaling by adding container instances. |

---

## 4. Design Decisions

### 4.1 Domain Models

Two core models flow through the system: `SearchRequest` (input to the search layer) and `HotelResult` (output to the UI). Field-level definitions are in [MODELS.md](MODELS.md).

#### SearchRequest

Produced by the AI intake agent, consumed by platform adapters.

**Key decisions:**
- Rooms are an explicit array, not a flat guest count — because group trips need specific per-room configurations. This maps directly to how Hotelbeds availability search works.
- Both budget types are nullable. The AI extracts whichever the user stated.
- Preferences are free-text strings, not an enum — see [ADR-007](#adr-007-free-text-preferences-over-structured-enums).
- LocationConstraint is free text. It is evaluated downstream via POI enrichment and AI ranking (Section 4.3), not parsed into structured filters.

#### HotelResult

Produced by platform adapters + ranking layer, consumed by the frontend.

**Key decisions:**
- Offers are nested under Hotel, and Rooms are nested under Offer — because different offers for the same hotel have different room configurations and prices.
- MatchScore and MatchExplanation are set by the ranking step, not by the platform adapter. Adapters return raw data; the AI evaluator adds scoring.
- BookingUrl is constructed by the application (see Section 4.4), not returned by the platform API.

**What is intentionally NOT modeled:** Booking, Payment, and Revenue/Monetization are permanently out of scope. User, Account, and SavedSearch models are needed for Phase 1 but not defined here yet.

---

### 4.2 AI Intake Agent Contract

The AI intake agent owns the pre-search conversation. Its only structured output is a `SearchRequest` JSON, produced when it decides to show the confirmation card.

#### Output schema

The agent produces a JSON object matching the `SearchRequest` model (Section 4.1). See [MODELS.md](MODELS.md#intake-agent-output) for the full schema and example.

#### Minimum required fields

The agent must not proceed without: `destination`, `checkIn`, `checkOut`, and at least one `room` with `adults ≥ 1`. If any of these are missing from the user's input, the agent must ask for them. It must never invent these values.

#### Optional fields

The agent may extract any optional `SearchRequest` field from the conversation, including `totalBudget`, `nightlyBudget`, `currency`, `preferences`, `minStarRating`, and `locationConstraint`. These are populated when the user mentions them but never prompted for with fixed questions — the agent uses its judgment to ask relevant follow-up questions (see Refinement behaviour).

#### Refinement behaviour

- The agent may ask follow-up questions to narrow preferences, budget, or location — but only when it judges this will meaningfully improve search results.
- Maximum **3 follow-up turns** before the agent must present the confirmation card with whatever information it has gathered. This prevents the conversation from feeling like an interrogation.
- The agent may combine multiple questions in one turn.

#### Conversation state

No rigid state machine. The agent manages the conversation naturally. There is no intermediate structured format tracking "filled" vs "missing" fields. The only structured output is the final `SearchRequest` JSON.

#### Validation

The backend validates the `SearchRequest` after receiving it from the AI:
- Required fields present, correct types (valid date range, adults ≥ 1 per room).
- If validation fails: reject with error. This indicates a code/prompt bug, not a user error.
- No re-validation of the AI's judgment (e.g., whether preferences are "specific enough") — the user confirmed the summary card.

#### Error handling

If the AI returns malformed JSON: retry once with a corrective prompt. If it fails again, show a generic user-facing error. This is an edge case, not a design pillar.

---

### 4.3 Ranking Strategy

**Approach: filter in code, enrich with POI data, rank with LLM.**

#### Step 1 — Platform search (platform adapter)

The adapter translates `SearchRequest` into platform-specific API calls. The platform already filters by destination, dates, and room occupancy — only actually available hotels are returned.

#### Step 2 — Hard constraint filtering (code)

Simple pass/fail checks, no AI involved:
- **Budget**: drop offers that exceed either budget constraint — where `totalPrice > totalBudget` or `totalPrice / nights > nightlyBudget`. If both are set, an offer must satisfy both.
- **Star rating**: if the user specified a minimum, filter here.

Keep this list deliberately short. The more you filter in code, the more you must map free-text preferences to structured fields — which is fragile and platform-specific.

#### Candidate cap

If more than 20 candidates remain after Step 2, only the top 20 (by price, ascending) proceed to Steps 3 and 4. This cap applies regardless of whether POI enrichment runs — it keeps LLM ranking calls small and fast (see [ADR-005](#adr-005-hybrid-ranking-strategy-code-filters--poi-enrichment--llm)).

#### Step 3 — POI enrichment (conditional)

Runs only when `SearchRequest.LocationConstraint` is present. For each candidate (≤20):

1. The LLM extracts relevant POI categories from the free-text constraint (e.g., "within walking distance of a grocery store" → `supermarket`; "not further than 100m from a train station" → `train_station`).
2. Query the POI service (OpenStreetMap Overpass API) with the hotel's lat/lng and a reasonable search radius (default 1km) for each category.
3. Populate `HotelResult.NearbyPOIs` with the nearest matches and their distances.

This step converts vague location constraints into concrete distance data that the LLM ranking step can reason about. The LLM evaluates both fuzzy constraints ("walking distance", "nearby") and exact ones ("100m") naturally when given actual distances.

**Why Overpass API?** Free, no API key, good European POI coverage (train stations, supermarkets, pharmacies, etc.), supports radius queries. For ≤20 hotels with a few categories each, this is on the order of 20–60 Overpass queries — manageable latency if batched per hotel.

**Why not a hard filter?** Parsing "walking distance" into a meter threshold is fragile. The LLM handles the interpretation given concrete data.

#### Step 4 — AI ranking (LLM)

Send remaining candidates (≤20 after the candidate cap) to the LLM along with the original `SearchRequest`. The LLM:
1. Scores each hotel 0–100 for overall fit.
2. Writes a one-sentence match explanation.
3. Returns the top results ordered by score.

The prompt includes: hotel name, facilities, room/board descriptions, price, location, and nearby POI distances (when available). It does **not** include images (the LLM cannot evaluate those).

#### LLM ranking output

See [MODELS.md](MODELS.md#llm-ranking-output) for the response schema.

#### Edge cases

- **0 results after filtering**: tell the user no exact matches were found, suggest relaxing budget or dates. Do not silently relax constraints.
- **Fewer than 5 results**: show what you have. Do not pad with poor matches.
- **LLM timeout or error**: fall back to price-sorting. Results are still valid, just unranked.

---

### 4.4 External Booking Link Strategy

**Approach: Google Hotels deep link.** See [ADR-006](#adr-006-google-hotels-deep-link-for-external-booking) for rationale and alternatives considered.

#### URL construction

```
https://www.google.com/travel/hotels?q={hotel_name}+{city}&dates={checkIn},{checkOut}&guests={totalGuests}
```

`totalGuests` is the sum of all adults and children across all rooms in the `SearchRequest`. Constructed entirely from data already present in `HotelResult` and `SearchRequest`.

#### UI treatment

A single button on the hotel detail view: **"Book on Google Hotels →"**, opens in a new tab.

---

## 5. Architectural Decision Records

### Decision Register

| ADR | Title | Status | Drivers |
|---|---|---|---|
| 001 | Modular monolith as architectural style | Accepted | Maintainability, Interoperability, Reliability |
| 002 | Shared database across all modules | Accepted | Reliability, Deployability, GDPR |
| 003 | Platform adapter interface for extensibility | Accepted | Maintainability, Interoperability |
| 004 | Semantic Kernel as AI abstraction boundary | Accepted | Maintainability, .NET stack constraint |
| 005 | Hybrid ranking strategy | Accepted | Performance, Maintainability, Reliability |
| 006 | Google Hotels deep link for external booking | Accepted | Non-goal (no in-app booking), cost constraint |
| 007 | Free-text preferences over structured enums | Accepted | Maintainability, Interoperability |
| 008 | Inward dependency rule for project references | Accepted | Maintainability, Interoperability |

---

### ADR-001: Modular Monolith as Architectural Style

**Status:** Accepted | **Date:** 2026-03-13

**Context:**
Solo-developer search application integrating multiple external platform APIs. Critical characteristics: maintainability/extensibility, interoperability, reliability. Target: 50 concurrent sessions, Docker deployment, no cloud-specific dependencies.

**Decision:**
Modular monolith — domain-partitioned modules, monolithic deployment. Each logical component becomes a .NET project with boundaries enforced by project references and `internal` access modifiers.

**Alternatives Considered:**
- **Layered** — No structural enforcement of domain isolation. Boundary erosion risk with Phase 2 already planned.
- **Microkernel** — Only Platform Search fits the plugin model. In practice identical to interface + DI registration.
- **Microservices** — No driver for independent deployment/scaling. Operational overhead unjustified.
- **Event-driven** — Core workflow is request-response. No async workflows or fan-out.

**Consequences:**
- (+) Compiler-enforced boundaries, simple deployment, in-process communication, atomic cross-module operations
- (−) No independent scaling per component. Erosion risk mitigated by compiler, not code review.

**Drivers:** Maintainability/Extensibility, Interoperability, Reliability, solo developer constraint.

---

### ADR-002: Shared Database Across All Modules

**Status:** Accepted | **Date:** 2026-03-13

**Context:**
The modular monolith (ADR-001) needs a data strategy. Modules that persist data: Identity & Accounts (users, credentials), Saved Searches (search snapshots). GDPR requires atomic deletion of all user data.

**Decision:**
Single shared PostgreSQL database. Each module owns its tables and accesses only its own data. No cross-module joins — modules reference each other by ID only.

**Alternatives Considered:**
- **Database per module** — Stronger isolation but makes atomic GDPR deletion a distributed coordination problem. Adds operational overhead for a solo developer with only two persisting modules.
- **Shared database, shared tables** — Simpler but erases ownership boundaries. Any module could read or write another's data.

**Consequences:**
- (+) Atomic cross-module transactions (GDPR deletion in one commit). Single database to operate, back up, and migrate.
- (−) Risk of cross-module coupling through the database if table ownership discipline lapses. Harder to extract a module into a separate service later.

**Drivers:** Reliability (atomic operations), Deployability (single database), solo developer constraint, GDPR.

---

### ADR-003: Platform Adapter Interface for Extensibility

**Status:** Accepted | **Date:** 2026-03-13

**Context:**
The system must support multiple accommodation platforms (Hotelbeds Phase 1, Amadeus Phase 2, more planned). Adding a platform must not require changes to orchestration, ranking, or UI.

**Decision:**
Define an `IPlatformAdapter` interface in the shared Contracts project (see ADR-008). Each platform implements it in a separate .NET project. Adapters are registered via DI. Search Orchestration calls all registered adapters — it does not know which platforms exist.

**Alternatives Considered:**
- **Direct integration without interface** — Faster initially but every new platform requires changes to orchestration logic. Violates the top-priority maintainability characteristic.
- **Plugin loading via assembly scanning** — Stronger isolation but over-engineered. DI registration achieves the same result without runtime assembly loading complexity.

**Consequences:**
- (+) New platform = new project, implement interface, register. Zero changes elsewhere. Validated by scenario S1.
- (−) All adapters must conform to a single interface. If a platform has fundamentally different capabilities, the interface may need extension — which affects all adapters.

**Drivers:** Maintainability/Extensibility (critical), Interoperability (critical).

---

### ADR-004: Semantic Kernel as AI Abstraction Boundary

**Status:** Accepted | **Date:** 2026-03-13

**Context:**
Two components use LLM calls: Intake Agent (conversation) and Result Processing (ranking). The requirements mandate no hard dependency on a single AI vendor.

**Decision:**
Use Microsoft Semantic Kernel as the abstraction layer for all AI provider interactions. Business logic depends on Semantic Kernel abstractions, not on provider-specific SDKs.

**Alternatives Considered:**
- **Direct provider SDK calls** — Simpler initially but swapping providers means rewriting all call sites.
- **Custom abstraction layer** — Full control but duplicates what Semantic Kernel already provides. Maintenance burden for a solo developer.

**Consequences:**
- (+) Swap AI provider by changing configuration and connector. Business logic unchanged. Validated by scenario S2.
- (−) Couples to Microsoft's abstraction. If Semantic Kernel's API changes or is abandoned, all AI-consuming modules are affected. Acceptable risk given active maintenance and .NET ecosystem alignment.

**Drivers:** Maintainability/Extensibility (critical), .NET tech stack constraint.

---

### ADR-005: Hybrid Ranking Strategy (Code Filters + POI Enrichment + LLM)

**Status:** Accepted | **Date:** 2026-03-13

**Context:**
Users express preferences in free text ("near the lake", "quiet area", "breakfast included"). The system must rank results against these preferences within a 30-second budget. Platform APIs return 50–200 candidates.

**Decision:**
Three-step pipeline: (1) hard-constraint filtering in code (budget, star rating) to reduce candidates, (2) POI enrichment via Overpass API for location constraints (≤20 candidates), (3) LLM ranking with scores and explanations for the enriched set.

**Alternatives Considered:**
- **Pure LLM ranking of all candidates** — Too slow and expensive. Sending 200 hotels with full details to an LLM exceeds latency and token budgets.
- **Pure rules-based ranking** — Cannot interpret free-text preferences ("quiet area away from nightlife"). Would require mapping every preference to structured filters.
- **LLM ranking without POI enrichment** — The LLM cannot evaluate location constraints without distance data.

**Consequences:**
- (+) Keeps LLM calls small (≤20 candidates). Code filters are fast and deterministic. POI data gives the LLM concrete facts. Fallback to price-sort if LLM fails.
- (−) Three-step pipeline adds orchestration complexity. POI queries add latency. Cap of 20 candidates means some valid matches may be excluded from ranking.

**Drivers:** Performance (30s budget), Maintainability (free-text preferences), Reliability (fallback strategy).

---

### ADR-006: Google Hotels Deep Link for External Booking

**Status:** Accepted | **Date:** 2026-03-13

**Context:**
GetARoof does not process bookings. The user needs a path from a selected hotel to completing a reservation. No affiliate agreements or platform booking APIs are available.

**Decision:**
Construct a Google Hotels URL from hotel name, city, dates, and guest count. Display as a single "Book on Google Hotels" button that opens in a new tab.

**Alternatives Considered:**
- **Platform-specific booking deep links** — Hotelbeds and Amadeus are B2B APIs. Completing a booking would require becoming a reseller.
- **No booking link** — User would have to search manually on booking platforms. Defeats the purpose of the app.

**Consequences:**
- (+) Zero integration effort. Works for any hotel. Shows prices across multiple booking channels.
- (−) Google Hotels URL format is undocumented. Worst case: user lands on a page that doesn't perfectly pre-fill. Acceptable risk.

**Drivers:** Non-goal (no in-app booking), cost constraint (no affiliate agreements).

---

### ADR-007: Free-Text Preferences Over Structured Enums

**Status:** Accepted | **Date:** 2026-03-13

**Context:**
Users describe preferences in natural language: "breakfast included", "pet-friendly", "quiet area". These must be evaluated against hotel data from platforms with different facility taxonomies.

**Decision:**
Store preferences as `string[]` in `SearchRequest`. No enum mapping at the model level. The LLM ranking step (ADR-005) interprets preferences against hotel facilities and POI data.

**Alternatives Considered:**
- **Structured preference enum** — Enables deterministic filtering but requires maintaining a cross-platform facility taxonomy. Fails for subjective preferences ("quiet area", "good for families").
- **Hybrid (enum for common + free-text for rest)** — More complexity for marginal benefit. The LLM handles both categories equally well.

**Consequences:**
- (+) Supports any preference without model changes. No cross-platform taxonomy maintenance.
- (−) Preferences cannot be used for deterministic pre-filtering. A preference like "breakfast included" could have been a cheap code filter but is instead evaluated by the LLM.

**Drivers:** Maintainability/Extensibility (no enum maintenance), Interoperability (no cross-platform taxonomy).

---

### ADR-008: Inward Dependency Rule for Project References

**Status:** Accepted | **Date:** 2026-03-14

**Context:**
The modular monolith (ADR-001) enforces module boundaries via .NET project references. Without a rule governing which projects may reference which, dependency direction will be decided ad hoc during implementation. This risks infrastructure types leaking into domain logic — making it harder to swap adapters (Phase 2), test business logic in isolation, and reason about the system.

**Decision:**
One compile-enforced rule: **domain types and orchestration logic must not reference infrastructure.** Concretely:

- A shared **Contracts** project owns domain types (`SearchRequest`, `HotelResult`) and interface definitions (`IPlatformAdapter`, `ILocationResolver`, `IRankingService`, etc.).
- Orchestration and business logic projects reference Contracts, never adapter implementations.
- Adapter projects (Hotelbeds, Amadeus, Nominatim, etc.) reference Contracts and implement the interfaces defined there.
- The **composition root** (the ASP.NET Core host) references everything and wires adapters to interfaces via dependency injection.

This is not full hexagonal/onion architecture. There are no mandated concentric layers within modules, no required repository pattern for simple persistence (e.g., Saved Searches), and no abstraction for its own sake. The rule applies at the project reference level only.

**Alternatives Considered:**
- **No dependency rule** — Rely on developer discipline. Faster initially, but in a solo project there is no code review to catch violations. Wrong dependencies become visible only when Phase 2 forces a change.
- **Full hexagonal/onion architecture** — Mandates concentric layers (domain model, domain services, application services, infrastructure) within each module. Provides stronger structural guarantees but adds significant ceremony for modules that are internally simple. Over-engineered for a solo developer with 7 modules.

**Consequences:**
- (+) Compile-time enforcement: if an orchestration project references an adapter project, it won't build. No discipline required — the compiler catches it.
- (+) Adapters are swappable without touching orchestration. Validated by scenarios S1, S2, S3.
- (+) Orchestration and business logic are unit-testable by mocking interfaces from Contracts.
- (−) Interface definitions must live in Contracts, not in the module that implements them. This moves `IPlatformAdapter` out of Platform Search (updating ADR-003).
- (−) Adding a new external dependency requires defining an interface in Contracts first, then implementing it — slightly more upfront work than calling the dependency directly.

**Drivers:** Maintainability/Extensibility (critical), Interoperability (critical), solo developer constraint.

---

## 6. Architecture Diagrams

All diagrams use the [C4 model](https://c4model.com/) with PlantUML. Source files are in `diagrams/*.puml`. Regenerate SVGs with `diagrams/render.sh`.

### 6.1 Context Diagram

The system, its users, and all external dependencies.

![Context Diagram](diagrams/context.svg)

### 6.2 Component Diagram

All modules, their interactions, and external system dependencies.

![Component Diagram](diagrams/component.svg)

### 6.3 Deployment Diagram

Physical containers and network boundaries.

![Deployment Diagram](diagrams/deployment.svg)

### 6.4 Search Flow Sequence Diagram

The end-to-end search pipeline — the most complex flow in the system.

![Search Flow](diagrams/search-flow.svg)

---

## 7. Cross-cutting Concepts

This section tracks cross-cutting design topics that affect multiple components. These are open design decisions — they will be resolved during implementation and documented here as decisions are made.

### 7.1 Error Handling Strategy

**Status:** Partially defined — component-level error handling is specified in Sections 4.2 (intake agent), 4.3 (ranking), and quality scenarios S4–S6. What is not yet defined:

- Consistent error response shape for the REST API (error codes, message format, correlation IDs)
- Whether to use middleware-based exception handling or per-endpoint try/catch
- Error classification (transient vs. permanent) and retry policy beyond the AI-specific retry in Section 4.2

### 7.2 Logging and Observability

**Status:** Not decided.

Open questions:
- Structured logging library (e.g., Serilog, NLog, or built-in `ILogger` with JSON formatter)
- Log levels and what to log at each level (especially for external API calls and AI interactions)
- Health check endpoints for container orchestration
- Whether to add distributed tracing (OpenTelemetry) or defer to post-MVP
- Log storage and retention strategy for a solo-operated deployment

### 7.3 Configuration Management

**Status:** Partially defined — environment variables for API keys and database connection (§1.3 Constraints, §2.3 Technical Context). What is not yet defined:

- Configuration layering (appsettings.json → environment variables → secrets)
- Which settings are runtime-configurable vs. requiring a restart
- Feature flags for Phase 1 vs. Phase 2 functionality (if needed)

### 7.4 Authentication and Authorization Flow

**Status:** Partially defined — ASP.NET Core Identity chosen, token-based auth mentioned (§1.2 Security definition, requirements.md §5). What is not yet defined:

- JWT vs. cookie-based authentication for the Blazor WASM ↔ API boundary
- Token storage on the client side (localStorage, sessionStorage, or in-memory)
- Token lifetime and refresh rotation specifics
- Authorization model beyond authenticated/anonymous (currently no roles or permissions needed)

### 7.5 API Design Conventions

**Status:** Not decided.

Open questions:
- URL structure and versioning (e.g., `/api/v1/...`)
- Consistent response envelope or direct resource serialization
- Pagination approach for result lists (saved searches)
- Request/response content type (JSON assumed, but not formalized)

---

## 8. Risks and Technical Debt

Business and platform risks are documented in [requirements.md](requirements.md) Section 10. This section tracks **architectural risks and known technical debt**.

### 8.1 Architectural Risks

| Risk | Severity | Mitigation |
|---|---|---|
| `SearchRequest` touched by 6 components — schema change has wide blast radius | Moderate | Treat as stable contract. Changes require cross-component review. (From §2.6) |
| Google Hotels URL format is undocumented — may break without notice | Low | Worst case: user lands on a generic Google Hotels page. Monitor and adjust. (From ADR-006) |
| Semantic Kernel dependency — if abandoned or API changes significantly, all AI-consuming modules affected | Low | Active maintenance and .NET ecosystem alignment reduce risk. Acceptable. (From ADR-004) |
| 20-candidate cap may exclude valid matches from LLM ranking | Low | Cap chosen for latency budget. Revisit if users report missing relevant results. (From ADR-005) |
| Nominatim usage policy (1 req/s) may bottleneck location resolution under load | Low | Single geocode per search. Only a concern at scale beyond MVP's 50 concurrent sessions. |

### 8.2 Technical Debt

| Item | Source | Impact | When to Address |
|---|---|---|---|
| No schema versioning for saved searches | §2.6 | If `SearchRequest` or `HotelResult` models evolve after users have saved searches, old snapshots may become unreadable | Before any model change after users have saved data |
| Cross-cutting concepts (§7) are undecided | This document | Implementation will make ad hoc choices that may be inconsistent across modules | During early implementation — decide before the second module is built |
| No monitoring or alerting strategy | §7.2 | Failures in production (API outages, AI errors) may go unnoticed | Before any non-local deployment |
| User/Account/SavedSearch database models not yet defined | §4.1 | Deferred intentionally, but needed before implementing Identity & Accounts and Saved Searches modules | Phase 1 implementation |

---

## 9. Glossary

| Term | Definition |
|---|---|
| Adapter | An implementation of `IPlatformAdapter` that integrates a specific accommodation platform API (e.g., Hotelbeds, Amadeus). |
| CheckRate | A platform API call that verifies the current price and availability of a specific hotel offer before the user proceeds to booking. Required by Hotelbeds. |
| Composition root | The ASP.NET Core host project that references all modules and wires adapter implementations to interfaces via dependency injection. |
| Confirmation card | A structured summary of extracted search requirements presented to the user by the intake agent. The search only proceeds after the user explicitly confirms it. |
| Contracts project | The shared .NET project that owns domain types (`SearchRequest`, `HotelResult`) and interface definitions (`IPlatformAdapter`, etc.). All modules depend inward on Contracts. (ADR-008) |
| Hard-constraint filter | A deterministic code-based filter applied before AI ranking — currently budget and star rating. Removes candidates that objectively fail. (Section 4.3, Step 2) |
| HotelResult | The common domain model for a hotel with its offers, produced by platform adapters, enriched by Result Processing, and consumed by the frontend. (MODELS.md) |
| Intake agent | The AI-driven conversational component that owns the pre-search dialogue, extracts requirements, and produces a confirmed `SearchRequest`. (Section 2.2) |
| Location constraint | A free-text string in `SearchRequest` describing a spatial preference ("near the lake", "walking distance to old town"). Evaluated via POI enrichment + LLM ranking, not parsed into structured filters. |
| Location Resolution | Lightweight service that resolves free-text destination strings to geocoded coordinates via Nominatim. No domain model, no state, no data ownership. (Section 2.2) |
| Modular monolith | The architectural style: domain-partitioned modules with compiler-enforced boundaries, deployed as a single container. (ADR-001) |
| Nominatim | OpenStreetMap's geocoding service, used by Location Resolution to convert destination strings into coordinates and bounding boxes. |
| Offer | A specific room/rate combination within a `HotelResult`. A single hotel may have multiple offers with different room configurations and prices. |
| Overpass API | OpenStreetMap's query API, used for POI enrichment — finding nearby points of interest (train stations, supermarkets, etc.) given hotel coordinates. |
| Platform | An external accommodation API that provides hotel availability and content data (Hotelbeds, Amadeus). |
| Platform Search | The component that owns all platform-specific API integration logic. Contains one adapter per platform. (Section 2.2) |
| POI enrichment | The step that queries Overpass API for nearby points of interest around each candidate hotel, converting vague location constraints into concrete distance data for AI ranking. (Section 4.3, Step 3) |
| Presentation | The component that owns all user-facing UI rendering — chat interface, search results, property details, saved searches. Blazor WebAssembly SPA. (Section 2.2) |
| Result Processing | The component that owns the pipeline transforming raw platform results into ranked, enriched results — hard-constraint filtering, POI enrichment, and AI ranking. (Section 2.2) |
| Search Orchestration | The component that coordinates the end-to-end search workflow across Location Resolution, Platform Search, Result Processing, and Presentation. (Section 2.2) |
| SearchRequest | The structured output of the intake agent: destination, dates, rooms, budget, preferences. The input contract for the search pipeline. (MODELS.md) |
| Semantic Kernel | Microsoft's .NET SDK for AI orchestration, used as the abstraction layer between business logic and AI providers. (ADR-004) |
