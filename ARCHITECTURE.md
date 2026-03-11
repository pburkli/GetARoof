# GetARoof — Architecture

This document defines the architecture for GetARoof. It follows a reasoning-first structure: quality attributes and constraints (Section 1), then logical components and architectural style (Sections 2–3, to be defined), then concrete design decisions (Section 4).

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

---

## 2. Logical Components

*To be defined.*

---

## 3. Architectural Style

*To be defined.*

---

## 4. Design Decisions

### 4.1 Domain Models

Two core models flow through the system: `SearchRequest` (input to the search layer) and `HotelResult` (output to the UI).

#### SearchRequest

Produced by the AI intake agent, consumed by platform adapters.

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Destination | string | Yes | City name or area description ("Zürich", "Lake Lucerne area") |
| CheckIn | date | Yes | |
| CheckOut | date | Yes | |
| Rooms | Room[] | Yes | At least one room with ≥1 adult |
| TotalBudget | decimal? | No | Max total stay price in user's currency |
| NightlyBudget | decimal? | No | Max per-night price |
| Currency | string | No | Defaults to EUR |
| Preferences | string[] | No | Free-text preferences: "breakfast included", "parking", "pool", "pet-friendly" |
| MinStarRating | int? | No | Minimum hotel star rating (1–5) |
| LocationConstraint | string? | No | Free-text: "near the lake", "walking distance to old town" |

**Room:**

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Adults | int | Yes | ≥1 |
| ChildrenAges | int[] | No | Empty if no children |
| Label | string? | No | Display label: "grandparents", "family with kids" |

**Key decisions:**
- Rooms are an explicit array, not a flat guest count — because group trips need specific per-room configurations. This maps directly to how Hotelbeds availability search works.
- Both budget types are nullable. The AI extracts whichever the user stated.
- Preferences are free-text strings, not an enum. Different platforms have different facility taxonomies; mapping happens in the ranking/matching layer, not the model.
- LocationConstraint is free text. It is evaluated downstream via POI enrichment and AI ranking (Section 4.3), not parsed into structured filters.

#### HotelResult

Produced by platform adapters + ranking layer, consumed by the frontend.

| Field | Type | Notes |
|-------|------|-------|
| Id | string | Platform-specific hotel ID |
| Platform | string | "Hotelbeds", "Amadeus" |
| Name | string | |
| Description | string | |
| StarRating | decimal? | 1–5 |
| Location | Location | Address, city, country, lat/lng |
| Images | Image[] | URL + optional caption |
| Facilities | string[] | "Free WiFi", "Pool", "Parking" |
| Offers | Offer[] | One or more room/rate combinations |
| MatchScore | int? | 0–100, set by ranking step |
| MatchExplanation | string? | AI-generated match summary |
| NearbyPOIs | NearbyPOI[] | Nearby points of interest with distances, populated by POI enrichment step |
| BookingUrl | string? | External booking link, constructed by the application |

**Offer:**

| Field | Type | Notes |
|-------|------|-------|
| OfferId | string | Platform-specific, needed for CheckRate |
| Rooms | OfferRoom[] | Room details for this combination |
| TotalPrice | decimal | |
| Currency | string | |
| CancellationPolicy | string? | |
| IsPriceVerified | bool | True after CheckRate |

**OfferRoom:**

| Field | Type | Notes |
|-------|------|-------|
| RoomName | string | "Double Standard", "Family Suite" |
| BoardType | string? | "Breakfast included", "Room only" |
| Adults | int | |
| Children | int | |
| BedDescription | string? | |

**NearbyPOI:**

| Field | Type | Notes |
|-------|------|-------|
| Name | string | "Zürich HB", "Migros Langstrasse" |
| Category | string | "train_station", "supermarket", "restaurant", etc. |
| DistanceMeters | int | Straight-line distance from hotel |

**Key decisions:**
- Offers are nested under Hotel, and Rooms are nested under Offer — because different offers for the same hotel have different room configurations and prices.
- MatchScore and MatchExplanation are set by the ranking step, not by the platform adapter. Adapters return raw data; the AI evaluator adds scoring.
- BookingUrl is constructed by the application (see Section 4.4), not returned by the platform API.

**What is intentionally NOT modeled:** Booking, Payment, and Revenue/Monetization are permanently out of scope. User, Account, and SavedSearch models are needed for Phase 1 but not defined here yet.

---

### 4.2 AI Intake Agent Contract

The AI intake agent owns the pre-search conversation. Its only structured output is a `SearchRequest` JSON, produced when it decides to show the confirmation card.

#### Output schema

The agent produces a JSON object matching the `SearchRequest` model (Section 4.1). Example:

```json
{
  "destination": "Zürich",
  "checkIn": "2026-07-15",
  "checkOut": "2026-07-20",
  "rooms": [
    { "adults": 2, "childrenAges": [], "label": "grandparents" },
    { "adults": 2, "childrenAges": [8, 5], "label": "family with kids" }
  ],
  "totalBudget": 2000,
  "nightlyBudget": null,
  "currency": "CHF",
  "preferences": ["breakfast included", "parking", "pool"],
  "locationConstraint": "near the lake"
}
```

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
- **Budget**: drop offers where `totalPrice > totalBudget` or `totalPrice / nights > nightlyBudget`.
- **Star rating**: if the user specified a minimum, filter here.

Keep this list deliberately short. The more you filter in code, the more you must map free-text preferences to structured fields — which is fragile and platform-specific.

#### Step 3 — POI enrichment (conditional)

Runs only when `SearchRequest.LocationConstraint` is present. For each remaining candidate (capped at ~20 after Step 2):

1. The LLM extracts relevant POI categories from the free-text constraint (e.g., "within walking distance of a grocery store" → `supermarket`; "not further than 100m from a train station" → `train_station`).
2. Query the POI service (OpenStreetMap Overpass API) with the hotel's lat/lng and a reasonable search radius (default 1km) for each category.
3. Populate `HotelResult.NearbyPOIs` with the nearest matches and their distances.

This step converts vague location constraints into concrete distance data that the LLM ranking step can reason about. The LLM evaluates both fuzzy constraints ("walking distance", "nearby") and exact ones ("100m") naturally when given actual distances.

**Why Overpass API?** Free, no API key, good European POI coverage (train stations, supermarkets, pharmacies, etc.), supports radius queries. For ~20 hotels this is ~20 queries — manageable latency.

**Why not a hard filter?** Parsing "walking distance" into a meter threshold is fragile. The LLM handles the interpretation given concrete data.

#### Step 4 — AI ranking (LLM)

Send remaining candidates (capped at ~20) to the LLM along with the original `SearchRequest`. The LLM:
1. Scores each hotel 0–100 for overall fit.
2. Writes a one-sentence match explanation.
3. Returns the top results ordered by score.

The prompt includes: hotel name, facilities, room/board descriptions, price, location, and nearby POI distances (when available). It does **not** include images (the LLM cannot evaluate those).

**Why not pure rules-based ranking?** User preferences are free-text and varied. Mapping "quiet area away from nightlife" to structured filters is a never-ending enum game. The LLM handles this naturally.

**Why not pure LLM for everything?** Cost and latency. Letting the platform API and code filters narrow the set to ~20 keeps the LLM call fast and cheap.

#### LLM ranking output

```json
[
  {
    "hotelId": "HB-12345",
    "score": 92,
    "explanation": "Excellent fit: 2 rooms match your group exactly, breakfast included, 400m from the lake, well within budget at CHF 1'650 total."
  }
]
```

The backend maps scores and explanations onto `HotelResult.MatchScore` and `HotelResult.MatchExplanation`.

#### Edge cases

- **0 results after filtering**: tell the user no exact matches were found, suggest relaxing budget or dates. Do not silently relax constraints.
- **Fewer than 5 results**: show what you have. Do not pad with poor matches.
- **LLM timeout or error**: fall back to price-sorting. Results are still valid, just unranked.

---

### 4.4 External Booking Link Strategy

**Approach: Google Hotels deep link.**

#### Why Google Hotels

- Works for any hotel with no affiliate agreement.
- Pre-fills hotel name and dates in the URL.
- Shows prices across multiple booking platforms (Booking.com, Expedia, hotel direct) — the user picks their preferred channel.
- Zero integration effort.

#### URL construction

```
https://www.google.com/travel/hotels?q={hotel_name}+{city}&dates={checkIn},{checkOut}&guests={totalGuests}
```

Constructed entirely from data already present in `HotelResult` and `SearchRequest`.

#### UI treatment

A single button on the hotel detail view: **"Book on Google Hotels →"**, opens in a new tab.

#### Future evolution

The `BookingUrl` field on `HotelResult` is a plain string. Changing the link generation strategy (e.g., to a platform-specific deep link) is a one-line change.

#### Risk

Google Hotels URL format is not a documented stable API. The worst case is the user lands on a Google Hotels page that doesn't perfectly pre-fill. They can still search manually. Acceptable risk.
