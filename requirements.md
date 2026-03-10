# Requirements

## 1. Overview / Purpose

### Problem Being Solved
Planning group or family accommodation for holidays is time-consuming and frustrating. Users must manually search multiple platforms, compare availability, mentally map room configurations against group needs (e.g., grandparents need their own room, families need specific bed counts), check location constraints, and manage budget — all at once. For groups this complexity multiplies significantly.

GetARoof solves this by allowing users to describe their requirements in plain natural language, then autonomously searching accommodation platforms, evaluating results against all stated requirements using AI, and presenting the best matches with photos, pricing, and an external booking link.

### Business Objectives and Success Metrics
- **Primary objective**: Become the go-to starting point for group and family holiday accommodation search in Europe.
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
- Search accommodation platforms via their APIs and present the best-matching results with photos, descriptions, exact location, and pricing
- Use AI to evaluate each result's conformance to all stated requirements
- Point the user to the easiest booking option for their selected accommodation (external link)
- Be straightforwardly extensible to additional accommodation platforms via a platform adapter interface

### Non-Goals
- In-app booking or payment processing (user always books externally via link)
- Revenue generation, monetization, or affiliate programs
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
- As any user, I want to be directed to the best external booking option for my selected accommodation so that I can complete the reservation easily.
- As any user, I want to see results from multiple platforms side by side so that I can compare across providers. *[Phase 2]*
- As a returning user, I want to optionally create an account and save my searches so that I can revisit results later without re-searching.

---

## 5. Functional Requirements

### Phase 1 (MVP — Hotelbeds)

- The system shall accept a free-text natural language input field where users describe their accommodation requirements including but not limited to: destination, date range, number and configuration of guests/rooms, location constraints, and budget.
- The system shall hand the user's raw input to an **AI intake agent** that owns the entire pre-search conversation: parsing, completeness assessment, refinement, and the decision to proceed.
- The AI intake agent shall parse the input and check whether the minimum fields needed to produce a usefully scoped search are present: **destination/location**, **time range**, and **room configuration** (at least one room with number of adults and optional children ages — see `ARCHITECTURE.md` Section 2). If any are missing or too ambiguous, the agent shall ask the user for them conversationally before continuing.
- Beyond minimum completeness, the AI intake agent shall assess whether the stated criteria are specific enough to avoid an unmanageably broad result set. If the agent judges the query to be too vague, it shall politely ask one or more targeted refinement questions to narrow the search — for example: *"Would you like breakfast included?"*, *"Do you need a parking space?"*, *"Is a pool important to you?"*, *"Are pets coming along?"*. The agent selects relevant questions based on context; it does not ask a fixed script.
- The AI intake agent decides — using its own judgment — when the accumulated requirements are specific enough to yield a well-focused search. Only then does it proceed; it does not rely on hard-coded rules or field-count thresholds. The agent operates within a maximum turn limit to avoid over-questioning (see `ARCHITECTURE.md` Section 3).
- Once the AI intake agent is satisfied, it presents the user with a structured confirmation card summarising all extracted requirements (destination, dates, group composition, budget, room configuration, selected preferences). The search is initiated only after the user explicitly confirms this summary.
- The system shall search Hotelbeds for available properties matching the parsed requirements (availability search + content API for photos and descriptions).
- When a location constraint is present, the system shall enrich each candidate property with nearby points of interest (POI) and their distances, using the hotel's coordinates and a POI service (see `ARCHITECTURE.md` Section 4).
- The system shall use AI to evaluate each candidate property for conformance to all stated requirements (room configuration, bed types, location proximity, preferences, price), using concrete POI distance data where available.
- The system shall rank results and present the best-matching properties to the user, each including: photos, property description, exact map location, total price for the stay, and the source platform. If fewer results match than expected, show what is available — do not silently relax constraints or pad with poor matches (see `ARCHITECTURE.md` Section 4).
- The system shall verify availability of the user's selected accommodation (CheckRate if required by the platform).
- The system shall provide a clear path to booking the selected accommodation externally via a Google Hotels deep link (see `ARCHITECTURE.md` Section 5).
- The system shall implement a platform adapter interface that allows new accommodation platforms to be integrated with minimal changes to core application logic.
- The system shall provide optional user registration and login with secure authentication (email/password minimum; extensible to OAuth providers). Sign-up shall not be required to search.
- Registered users shall be able to save completed searches (query parameters and results) for later reference.

### Phase 2 (Multi-Platform — Add Amadeus)

- The system shall search Amadeus (production Self-Service API) in addition to Hotelbeds for available properties.
- The system shall integrate a separate image API (e.g., Google Places) to provide hotel photos for Amadeus results, which do not include images.
- The system shall merge, deduplicate, and rank results across platforms.
- The system shall handle individual platform failures gracefully — if one platform's search fails, results from the remaining platforms shall still be returned with a user-facing notice.

---

## 6. Non-Functional Requirements

- **Performance**: Search results shall be returned within 30 seconds of query submission under normal operating conditions. The UI shall display a progress indicator during search.
- **Scalability**: The system shall be deployable to any major cloud provider or self-hosted environment without code changes. It shall support horizontal scaling of the backend.
- **Security**: All communication shall be over HTTPS. API keys shall be stored securely and never exposed to the client. User passwords shall be stored as salted hashes. Authentication tokens shall be short-lived with refresh token rotation.
- **Data handling**: The system does not process payments or handle payment data. Users always book externally.
- **Accessibility**: The web application shall conform to WCAG 2.1 Level AA.
- **Reliability**: The system shall handle platform timeouts and errors gracefully with user-facing error messages.
- **Maintainability**: Platform integrations shall be isolated behind adapter interfaces so that changes to one platform do not affect others. Adding a new platform shall require only implementing the adapter interface and registering it — no changes to core search orchestration logic. AI provider calls shall be abstracted so the underlying model can be swapped without changes to business logic.

---

## 7. UX / Design

### User Flows

**Search Flow (Phase 1)**
1. User lands on the home screen.
2. User types a natural language description of their holiday requirements into a single prominent input field and submits.
3. The AI intake agent processes the input and responds conversationally in the same interface:
   - If minimum information (destination, dates, room configuration) is incomplete, the agent asks for what is missing.
   - If the criteria are present but too broad for a focused search, the agent politely explains this and asks targeted follow-up questions to narrow the requirements (e.g., parking, breakfast, pool, pets). The agent chooses which questions to ask based on context.
   - The user answers in free text; the agent continues refining until it judges the requirements specific enough.
4. Once the AI intake agent is satisfied, it presents a structured confirmation card summarising the full set of requirements. The user reviews and clicks "Search" to confirm.
5. A progress indicator shows that the platform is being searched.
6. The top results are displayed as cards: photo gallery, property name, platform source, total price, distance to key location, brief AI-generated match summary.
7. User browses results and selects one.
8. A detail view shows full description, all photos, map, room breakdown, and an external booking link.

**Account Flow (Optional)**
1. User may sign up with email + password at any point. Sign-up is never required to search.
2. Registered user logs in (email/password; OAuth extensible).
3. After completing a search, a registered user can save the search (query + results) for later reference.
4. User can view and revisit saved searches from their profile.

### Design References
To be defined. Reference aesthetic: clean, travel-focused, minimal friction. Inspiration: Google Flights result cards, Airbnb search results layout.

---

## 8. Technical Considerations

### Constraints
- Frontend must be Blazor WebAssembly (.NET-based SPA).
- Backend must be .NET (ASP.NET Core).
- AI integration must be provider-agnostic — no hard dependency on a single AI vendor. Microsoft Semantic Kernel is the recommended abstraction layer.
- Deployment must be cloud-agnostic: containerized (Docker), no mandatory dependency on a specific cloud service (e.g., no Azure-only services, no AWS-specific SDKs in core logic).
- No booking or payment processing — the app is search-only with external booking links.

### Dependencies

**Phase 1:**
- Hotelbeds APItude API (Booking API + Content API)
- AI model provider (OpenAI, Anthropic, or other — swappable via Semantic Kernel)
- Map/geocoding service for location display and proximity calculation (e.g., OpenStreetMap / Nominatim)
- POI service for location constraint evaluation (e.g., OpenStreetMap Overpass API)

**Phase 2 adds:**
- Amadeus Self-Service API (production)
- Image API for Amadeus hotels (e.g., Google Places API)

**Future adds:**
- Booking.com Demand API (requires partner approval)
- Expedia Rapid API (requires business entity)

### Integrations
- Authentication: ASP.NET Core Identity (extensible to OAuth/OIDC providers)

### Tech Stack

See `ARCHITECTURE.md` Section 1 for the full tech stack table.

---

## 9. Milestones / Timeline

| Phase | Focus | Key Deliverable |
|-------|-------|-----------------|
| Phase 1 — MVP | Search + accounts with Hotelbeds | AI-driven hotel search with real photos, availability, external booking links, and optional user accounts with saved searches |
| Phase 2 — Multi-Platform | Add Amadeus + image API | Cross-platform search, deduplication, and ranking |
| Future | Additional platforms | Booking.com, Expedia, and other platform integrations |

---

## 10. Risks
- **Platform API limitations**: Hotelbeds evaluation environment is limited to 50 requests/day. This is sufficient for development and demo but would require a commercial agreement for any real usage. Amadeus Self-Service API does not include hotel images, requiring a supplementary API.
- **AI parsing accuracy**: Complex natural language queries (especially group room configurations) may be misinterpreted, leading to incorrect search parameters or a search scope that is too broad. This is mitigated by the AI intake agent (see Section 5 and the Search Flow in Section 7), which conversationally fills gaps, asks refinement questions when criteria are too vague, and presents a structured confirmation card that the user must approve before any search begins. The user always has the opportunity to correct misinterpretations before they affect results. Residual risk: the AI may misjudge when criteria are "specific enough", either searching too early on broad criteria or asking unnecessarily many questions. Prompt engineering and user testing during development will be used to calibrate this judgment.
- **Search latency**: Searching multiple platforms in parallel, POI enrichment queries, and AI evaluation of results may exceed acceptable response times. Streaming/progressive results display may be needed.
- **Platform availability**: External APIs may change or become unavailable without notice, requiring ongoing maintenance.
- **Commercial agreements**: Moving beyond evaluation/test environments for Hotelbeds and other platforms requires negotiating commercial agreements. Requirements and feasibility for an individual developer are unclear.
- **Regulatory**: GDPR compliance is required for European users. User data handling, consent, and the right to deletion must be designed in from the start.
