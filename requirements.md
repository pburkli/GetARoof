# Requirements

## 1. Overview / Purpose

### Problem Being Solved
Planning group or family accommodation for holidays is time-consuming and frustrating. Users must manually search multiple platforms, compare availability, mentally map room configurations against group needs (e.g., grandparents need their own room, families need specific bed counts), check location constraints, and manage budget — all at once. For groups this complexity multiplies significantly.

GetARoof solves this by allowing users to describe their requirements in plain natural language, then autonomously searching accommodation platforms, evaluating results against all stated requirements using AI, and presenting the best matches with photos, pricing, and a clear path to booking.

### Business Objectives and Success Metrics
- **Primary objective**: Become the go-to starting point for group and family holiday accommodation search in Europe.
- **Revenue model (future)**: Generate commission revenue from accommodation platforms via affiliate or wholesale agreements. The initial MVP does not include booking or revenue generation.
- **Success metrics**:
  - User finds a satisfactory match within the first search session
  - User returns for a second search within 12 months
  - Average time from search submission to result presentation < 30 seconds

---

## 2. Background / Context

### Why This Is Being Built Now
Large language models have made natural language understanding of complex, multi-constraint queries practical and affordable. Accommodation platform APIs (Hotelbeds, Amadeus) are accessible to individual developers without requiring a company entity. The combination enables a product that would have required a large engineering team just a few years ago.

### Relevant History or Prior Work
No prior version of GetARoof exists. This is a greenfield project built by a solo developer using AI-assisted coding tools. Existing solutions (booking.com, Google Hotels, Airbnb) require users to manually search each platform and do not support natural language group configuration.

### Platform Research
API access was evaluated for multiple platforms. See `platforms/overview.md` for a summary and `platforms/` subdirectories for detailed API documentation per platform.

---

## 3. Goals & Non-Goals

### Goals
- Accept natural language accommodation requests including group composition, room requirements, dates, location constraints, and budget
- Search accommodation platforms via their APIs and present real results with photos, descriptions, and pricing
- Use AI to evaluate availability and requirement matching for each result
- Present the best-matching results with photos, description, exact location, and price
- Point the user to the easiest booking option for their selected accommodation (external link)
- Be straightforwardly extensible to additional accommodation platforms via a platform adapter interface
- *[Future]* Support in-app booking and payment processing

### Non-Goals
- Processing or storing payment information (user always books externally in MVP)
- Booking flights, transport, or activities
- Supporting non-holiday accommodation (business travel, long-term rentals)
- Native mobile apps (web only for now)
- Property management or listing features
- Real-time customer support or chat

---

## 4. User Stories / Use Cases

### User Personas

**The Group Organizer** — A person coordinating a holiday for a mixed group (family reunion, friends trip). They have complex, specific requirements: multiple rooms with different configurations, location constraints, a firm budget split across many people. They are comfortable using a web app and want the planning burden taken off their plate.

**The Couple or Solo Traveler** — An individual or pair who wants a quick, smart search across platforms without visiting each one separately. They have simpler requirements but value the time savings and quality of AI-matched results.

### Use Cases
- As a group organizer, I want to describe my group's full accommodation needs in one natural language message so that I don't have to configure complex filters manually.
- As a group organizer, I want the app to check room configurations automatically so that I don't have to mentally verify whether a property fits our sleeping arrangements.
- As any user, I want to see the top matching results with photos and pricing so that I can compare options without visiting each platform separately.
- As any user, I want to be directed to the best external booking option for my selected accommodation so that I can complete the reservation easily. *[Future: in-app booking]*
- As any user, I want to see results from multiple platforms side by side so that I can compare across providers. *[Phase 2]*

---

## 5. Functional Requirements

### Phase 1 (MVP — Search Only, Hotelbeds)

- The system shall accept a free-text natural language input field where users describe their accommodation requirements including but not limited to: destination, date range, number and configuration of guests/rooms, location constraints, and budget.
- The system shall hand the user's raw input to an **AI intake agent** that owns the entire pre-search conversation: parsing, completeness assessment, refinement, and the decision to proceed.
- The AI intake agent shall parse the input and check whether the minimum fields needed to produce a usefully scoped search are present: **destination/location**, **time range**, and **room configuration** (at least one room with number of adults and optional children ages — see Section 11.1). If any are missing or too ambiguous, the agent shall ask the user for them conversationally before continuing.
- Beyond minimum completeness, the AI intake agent shall assess whether the stated criteria are specific enough to avoid an unmanageably broad result set. If the agent judges the query to be too vague, it shall politely ask one or more targeted refinement questions to narrow the search — for example: *"Would you like breakfast included?"*, *"Do you need a parking space?"*, *"Is a pool important to you?"*, *"Are pets coming along?"*. The agent selects relevant questions based on context; it does not ask a fixed script.
- The AI intake agent decides — using its own judgment — when the accumulated requirements are specific enough to yield a well-focused search. Only then does it proceed; it does not rely on hard-coded rules or field-count thresholds. The agent operates within a maximum turn limit to avoid over-questioning (see Section 11.2).
- Once the AI intake agent is satisfied, it presents the user with a structured confirmation card summarising all extracted requirements (destination, dates, group composition, budget, room configuration, selected preferences). The search is initiated only after the user explicitly confirms this summary.
- The system shall search Hotelbeds for available properties matching the parsed requirements (availability search + content API for photos and descriptions).
- The system shall use AI to evaluate each candidate property for availability within the requested dates and conformance to all stated requirements (room count, bed configuration, location proximity, price).
- The system shall rank results and present the best-matching properties to the user, each including: photos, property description, exact map location, total price for the stay, and the source platform.
- The system shall verify availability of the user's selected accommodation (CheckRate if required by the platform).
- The system shall provide a clear path to booking the selected accommodation externally via a Google Hotels deep link (see Section 11.4).
- The system shall implement a platform adapter interface that allows new accommodation platforms to be integrated with minimal changes to core application logic.

### Phase 2 (Multi-Platform — Add Amadeus)

- The system shall search Amadeus (production Self-Service API) in addition to Hotelbeds for available properties.
- The system shall integrate a separate image API (e.g., Google Places) to provide hotel photos for Amadeus results, which do not include images.
- The system shall merge, deduplicate, and rank results across platforms.
- The system shall handle individual platform failures gracefully — if one platform's search fails, results from the remaining platforms shall still be returned with a user-facing notice.

### Future Phases

- The system shall provide user registration and login with secure authentication (email/password minimum; extensible to OAuth providers).
- The system shall provide a "Book Now" button that initiates an in-app booking flow (requires commercial agreement with accommodation platform).
- The system shall record completed bookings in the user's booking history.
- The system shall detect when a booking platform requires guest data not yet present in the user's profile, prompt the user to supply only the missing information, persist that data to the profile, and reuse it for subsequent bookings on any platform.
- The system shall implement a monetization abstraction layer supporting affiliate commission tracking, with the ability to switch to or combine transaction fee charging without requiring architectural changes.
- The system shall never store, transmit, or log user payment information.

---

## 6. Non-Functional Requirements

- **Performance**: Search results shall be returned within 30 seconds of query submission under normal operating conditions. The UI shall display a progress indicator during search.
- **Scalability**: The system shall be deployable to any major cloud provider or self-hosted environment without code changes. It shall support horizontal scaling of the backend.
- **Security**: All communication shall be over HTTPS. API keys shall be stored securely and never exposed to the client. *[Future: User passwords shall be stored as salted hashes. Authentication tokens shall be short-lived with refresh token rotation.]*
- **Accessibility**: The web application shall conform to WCAG 2.1 Level AA.
- **Reliability**: The system shall handle platform timeouts and errors gracefully with user-facing error messages. In Phase 2+, if one platform's search fails, results from remaining platforms shall still be returned with a notice.
- **Maintainability**: Platform integrations shall be isolated behind adapter interfaces so that changes to one platform do not affect others. AI provider calls shall be abstracted so the underlying model can be swapped without changes to business logic.
- **Extensibility**: Adding a new platform integration shall require only implementing the platform adapter interface and registering the new adapter — no changes to core search orchestration logic.

---

## 7. UX / Design

### User Flows

**Search Flow (Phase 1)**
1. User lands on the home screen.
2. User types a natural language description of their holiday requirements into a single prominent input field and submits.
3. The AI intake agent processes the input and responds conversationally in the same interface:
   - If minimum information (destination, dates, number of guests) is incomplete, the agent asks for what is missing.
   - If the criteria are present but too broad for a focused search, the agent politely explains this and asks targeted follow-up questions to narrow the requirements (e.g., parking, breakfast, pool, pets). The agent chooses which questions to ask based on context.
   - The user answers in free text; the agent continues refining until it judges the requirements specific enough.
4. Once the AI intake agent is satisfied, it presents a structured confirmation card summarising the full set of requirements. The user reviews and clicks "Search" to confirm.
5. A progress indicator shows that the platform is being searched.
6. The top results are displayed as cards: photo gallery, property name, platform source, total price, distance to key location, brief AI-generated match summary.
7. User browses results and selects one.
8. A detail view shows full description, all photos, map, room breakdown, and an external booking link.

**Booking Flow (Future)**
1. User clicks "Book Now."
2. The app confirms the booking details (property, dates, guests, total price) and asks the user to confirm.
3. The app begins the booking process via the platform API.
4. If the platform requires guest data not yet available, the app prompts the user for only the missing fields.
5. The booking is confirmed and stored in the user's account.

**Account Flow (Future)**
- Registration: email + password, basic profile (name, preferred language).
- Login: email/password (MVP); OAuth extensible.
- Profile: stores guest data collected during previous bookings for reuse across platforms.
- Booking history: list of past and upcoming bookings with confirmation references.

### Design References
To be defined. Reference aesthetic: clean, travel-focused, minimal friction. Inspiration: Google Flights result cards, Airbnb search results layout.

---

## 8. Technical Considerations

### Constraints
- Frontend must be Blazor WebAssembly (.NET-based SPA).
- Backend must be .NET (ASP.NET Core).
- AI integration must be provider-agnostic — no hard dependency on a single AI vendor. Microsoft Semantic Kernel is the recommended abstraction layer.
- Deployment must be cloud-agnostic: containerized (Docker), no mandatory dependency on a specific cloud service (e.g., no Azure-only services, no AWS-specific SDKs in core logic).
- Payment data must never be handled by GetARoof — no PCI DSS scope.

### Dependencies

**Phase 1:**
- Hotelbeds APItude API (Booking API + Content API)
- AI model provider (OpenAI, Anthropic, or other — swappable via Semantic Kernel)
- Map/geocoding service for location display and proximity calculation (e.g., OpenStreetMap / Nominatim)

**Phase 2 adds:**
- Amadeus Self-Service API (production)
- Image API for Amadeus hotels (e.g., Google Places API)

**Future adds:**
- Booking.com Demand API (pending affiliate approval)
- Expedia Rapid or Travel Redirect API (requires business entity)
- Airwallex (VCC issuance — requires business entity for production)
- Browser automation library (e.g., Playwright for .NET) for fallback booking flows on platforms without booking APIs

### Integrations
- *[Future]* Authentication: ASP.NET Core Identity (extensible to OAuth/OIDC providers)
- *[Future]* Email: transactional email service for registration/confirmation (provider-agnostic via abstraction)

### Tech Stack Notes
| Layer | Technology |
|-------|-----------|
| Frontend | Blazor WebAssembly |
| Backend | ASP.NET Core Web API |
| AI Orchestration | Microsoft Semantic Kernel |
| Database | PostgreSQL (production); SQLite acceptable for local development (Phase 2+; no persistence in Phase 1) |
| Containerization | Docker + Docker Compose |
| Hosting | Cloud-agnostic; any container-capable host |

---

## 9. Milestones / Timeline

See `ROADMAP.md` for the full phased roadmap. Summary:

| Phase | Focus | Key Deliverable |
|-------|-------|-----------------|
| Phase 1 — MVP | Search-only with Hotelbeds | AI-driven hotel search with real photos, availability, and external booking links |
| Phase 2 — Multi-Platform | Add Amadeus + image API | Cross-platform search, deduplication, and ranking |
| Future | Booking, payment, accounts | In-app booking, user accounts, additional platforms |

---

## 10. Open Questions / Risks

### Open Questions
- ~~What should happen if fewer than 5 results meet the requirements~~ — **Decided**: show fewer results. Do not silently relax constraints. See Section 11.3.
- ~~What is the best external booking link to show for each Hotelbeds result~~ — **Decided**: Google Hotels deep link. See Section 11.4.
- Should the MVP include any form of user accounts, or is anonymous search sufficient for Phase 1?

### Risks
- **Platform API limitations**: Hotelbeds evaluation environment is limited to 50 requests/day. This is sufficient for development and demo but would require a commercial agreement for any real usage. Amadeus Self-Service API does not include hotel images, requiring a supplementary API.
- **AI parsing accuracy**: Complex natural language queries (especially group room configurations) may be misinterpreted, leading to incorrect search parameters or a search scope that is too broad. This is mitigated by the AI intake agent (see Section 5 and the Search Flow in Section 7), which conversationally fills gaps, asks refinement questions when criteria are too vague, and presents a structured confirmation card that the user must approve before any search begins. The user always has the opportunity to correct misinterpretations before they affect results. Residual risk: the AI may misjudge when criteria are "specific enough", either searching too early on broad criteria or asking unnecessarily many questions. Prompt engineering and user testing during development will be used to calibrate this judgment.
- **Search latency**: Searching multiple platforms in parallel with AI evaluation of results may exceed acceptable response times. Streaming/progressive results display may be needed.
- **Platform availability**: External APIs may change or become unavailable without notice, requiring ongoing maintenance.
- **Commercial agreements**: Moving beyond evaluation/test environments for Hotelbeds and other platforms requires negotiating commercial agreements. Requirements and feasibility for an individual developer are unclear.
- **Regulatory**: GDPR compliance is required for European users. User data handling, consent, and the right to deletion must be designed in from the start.

---

## 11. Design Decisions

This section records key design decisions made before architecture. These bridge the gap between requirements (what to build) and architecture (how to build it).

### 11.1 Domain Models

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
- LocationConstraint is free text. Geocoding or semantic interpretation happens downstream.

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

**Key decisions:**
- Offers are nested under Hotel, and Rooms are nested under Offer — because different offers for the same hotel have different room configurations and prices.
- MatchScore and MatchExplanation are set by the ranking step, not by the platform adapter. Adapters return raw data; the AI evaluator adds scoring.
- BookingUrl is constructed by the application (see Section 11.4), not returned by the platform API.

**What is intentionally NOT modeled in Phase 1:** User, Account, Booking, Payment, SearchSession (all ephemeral or out of scope).

---

### 11.2 AI Intake Agent Contract

The AI intake agent owns the pre-search conversation. Its only structured output is a `SearchRequest` JSON, produced when it decides to show the confirmation card.

#### Output schema

The agent produces a JSON object matching the `SearchRequest` model (Section 11.1). Example:

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

### 11.3 Ranking Strategy

**Approach: filter in code, rank with LLM.**

#### Step 1 — Platform search (platform adapter)

The adapter translates `SearchRequest` into platform-specific API calls. The platform already filters by destination, dates, and room occupancy — only actually available hotels are returned.

#### Step 2 — Hard constraint filtering (code)

Simple pass/fail checks, no AI involved:
- **Budget**: drop offers where `totalPrice > totalBudget` or `totalPrice / nights > nightlyBudget`.
- **Star rating**: if the user specified a minimum, filter here.

Keep this list deliberately short. The more you filter in code, the more you must map free-text preferences to structured fields — which is fragile and platform-specific.

#### Step 3 — AI ranking (LLM)

Send remaining candidates (capped at ~20) to the LLM along with the original `SearchRequest`. The LLM:
1. Scores each hotel 0–100 for overall fit.
2. Writes a one-sentence match explanation.
3. Returns the top results ordered by score.

The prompt includes: hotel name, facilities, room/board descriptions, price, location. It does **not** include images (the LLM cannot evaluate those).

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

### 11.4 External Booking Link Strategy

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

The `BookingUrl` field on `HotelResult` is a plain string. Changing the link generation strategy is a one-line change:
- **Booking.com affiliate** approval (pending) → swap to Booking.com deep link with affiliate tag.
- **Hotelbeds commercial agreement** → replace external link with in-app booking via their API.

#### Risk

Google Hotels URL format is not a documented stable API. The worst case is the user lands on a Google Hotels page that doesn't perfectly pre-fill. They can still search manually. Acceptable for MVP.
