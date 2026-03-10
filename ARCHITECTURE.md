# GetARoof — Architecture & Design Decisions

This document records key design decisions that bridge the gap between requirements (`requirements.md`) and implementation. It defines *how* the system is built — models, contracts, strategies, and technology choices.

---

## 1. Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Blazor WebAssembly |
| Backend | ASP.NET Core Web API |
| AI Orchestration | Microsoft Semantic Kernel |
| Database | PostgreSQL (production); SQLite acceptable for local development |
| Containerization | Docker + Docker Compose |
| Hosting | Cloud-agnostic; any container-capable host |

---

## 2. Domain Models

Two core models flow through the system: `SearchRequest` (input to the search layer) and `HotelResult` (output to the UI).

### SearchRequest

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
- LocationConstraint is free text. It is evaluated downstream via POI enrichment (Section 4) and LLM ranking, not parsed into structured filters.

### HotelResult

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
- BookingUrl is constructed by the application (see Section 5), not returned by the platform API.

**What is intentionally NOT modeled:** Booking, Payment, and Revenue/Monetization are permanently out of scope. User, Account, and SavedSearch models are needed for Phase 1 but not defined here yet.

---

## 3. AI Intake Agent Contract

The AI intake agent owns the pre-search conversation. Its only structured output is a `SearchRequest` JSON, produced when it decides to show the confirmation card.

### Output schema

The agent produces a JSON object matching the `SearchRequest` model (Section 2). Example:

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

### Minimum required fields

The agent must not proceed without: `destination`, `checkIn`, `checkOut`, and at least one `room` with `adults ≥ 1`. If any of these are missing from the user's input, the agent must ask for them. It must never invent these values.

### Refinement behaviour

- The agent may ask follow-up questions to narrow preferences, budget, or location — but only when it judges this will meaningfully improve search results.
- Maximum **3 follow-up turns** before the agent must present the confirmation card with whatever information it has gathered. This prevents the conversation from feeling like an interrogation.
- The agent may combine multiple questions in one turn.

### Conversation state

No rigid state machine. The agent manages the conversation naturally. There is no intermediate structured format tracking "filled" vs "missing" fields. The only structured output is the final `SearchRequest` JSON.

### Validation

The backend validates the `SearchRequest` after receiving it from the AI:
- Required fields present, correct types (valid date range, adults ≥ 1 per room).
- If validation fails: reject with error. This indicates a code/prompt bug, not a user error.
- No re-validation of the AI's judgment (e.g., whether preferences are "specific enough") — the user confirmed the summary card.

### Error handling

If the AI returns malformed JSON: retry once with a corrective prompt. If it fails again, show a generic user-facing error. This is an edge case, not a design pillar.

---

## 4. Ranking Strategy

**Approach: filter in code, enrich with POI data, rank with LLM.**

### Step 1 — Platform search (platform adapter)

The adapter translates `SearchRequest` into platform-specific API calls. The platform already filters by destination, dates, and room occupancy — only actually available hotels are returned.

### Step 2 — Hard constraint filtering (code)

Simple pass/fail checks, no AI involved:
- **Budget**: drop offers where `totalPrice > totalBudget` or `totalPrice / nights > nightlyBudget`.
- **Star rating**: if the user specified a minimum, filter here.

Keep this list deliberately short. The more you filter in code, the more you must map free-text preferences to structured fields — which is fragile and platform-specific.

### Step 3 — POI enrichment (conditional)

Runs only when `SearchRequest.LocationConstraint` is present. For each remaining candidate (capped at ~20 after Step 2):

1. The LLM extracts relevant POI categories from the free-text constraint (e.g., "within walking distance of a grocery store" → `supermarket`; "not further than 100m from a train station" → `train_station`).
2. Query the POI service (OpenStreetMap Overpass API) with the hotel's lat/lng and a reasonable search radius (default 1km) for each category.
3. Populate `HotelResult.NearbyPOIs` with the nearest matches and their distances.

This step converts vague location constraints into concrete distance data that the LLM ranking step can reason about. The LLM evaluates both fuzzy constraints ("walking distance", "nearby") and exact ones ("100m") naturally when given actual distances.

**Why Overpass API?** Free, no API key, good European POI coverage (train stations, supermarkets, pharmacies, etc.), supports radius queries. For ~20 hotels this is ~20 queries — manageable latency.

**Why not a hard filter?** Parsing "walking distance" into a meter threshold is fragile. The LLM handles the interpretation given concrete data.

### Step 4 — AI ranking (LLM)

Send remaining candidates (capped at ~20) to the LLM along with the original `SearchRequest`. The LLM:
1. Scores each hotel 0–100 for overall fit.
2. Writes a one-sentence match explanation.
3. Returns the top results ordered by score.

The prompt includes: hotel name, facilities, room/board descriptions, price, location, and nearby POI distances (when available). It does **not** include images (the LLM cannot evaluate those).

**Why not pure rules-based ranking?** User preferences are free-text and varied. Mapping "quiet area away from nightlife" to structured filters is a never-ending enum game. The LLM handles this naturally.

**Why not pure LLM for everything?** Cost and latency. Letting the platform API and code filters narrow the set to ~20 keeps the LLM call fast and cheap.

### LLM ranking output

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

### Edge cases

- **0 results after filtering**: tell the user no exact matches were found, suggest relaxing budget or dates. Do not silently relax constraints.
- **Fewer than 5 results**: show what you have. Do not pad with poor matches.
- **LLM timeout or error**: fall back to price-sorting. Results are still valid, just unranked.

---

## 5. External Booking Link Strategy

**Approach: Google Hotels deep link.**

### Why Google Hotels

- Works for any hotel with no affiliate agreement.
- Pre-fills hotel name and dates in the URL.
- Shows prices across multiple booking platforms (Booking.com, Expedia, hotel direct) — the user picks their preferred channel.
- Zero integration effort.

### URL construction

```
https://www.google.com/travel/hotels?q={hotel_name}+{city}&dates={checkIn},{checkOut}&guests={totalGuests}
```

Constructed entirely from data already present in `HotelResult` and `SearchRequest`.

### UI treatment

A single button on the hotel detail view: **"Book on Google Hotels →"**, opens in a new tab.

### Future evolution

The `BookingUrl` field on `HotelResult` is a plain string. Changing the link generation strategy (e.g., to a platform-specific deep link) is a one-line change.

### Risk

Google Hotels URL format is not a documented stable API. The worst case is the user lands on a Google Hotels page that doesn't perfectly pre-fill. They can still search manually. Acceptable risk.
