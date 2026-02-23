# Requirements

## 1. Overview / Purpose

### Problem Being Solved
Planning group or family accommodation for holidays is time-consuming and frustrating. Users must manually search multiple platforms, compare availability, mentally map room configurations against group needs (e.g., grandparents need their own room, families need specific bed counts), check location constraints, and manage budget — all at once. For groups this complexity multiplies significantly.

GetARoof solves this by allowing users to describe their requirements in plain natural language, then autonomously searching multiple accommodation platforms, evaluating results against all stated requirements using AI, and guiding the user through booking with minimal friction — the user's only manual task is entering payment information.

### Business Objectives and Success Metrics
- **Primary objective**: Become the go-to starting point for group and family holiday accommodation search in Europe.
- **Revenue**: Generate affiliate commission revenue from booking.com, homeToGo, and Interhome on completed bookings.
- **Success metrics**:
  - Search-to-booking conversion rate ≥ 10%
  - User returns for a second search within 12 months
  - Average time from search submission to booking completion < 5 minutes
  - Affiliate commission earned per completed booking tracked and reported

---

## 2. Background / Context

### Why This Is Being Built Now
Large language models have made natural language understanding of complex, multi-constraint queries practical and affordable. Browser automation tools and affiliate APIs make cross-platform booking orchestration feasible for a small team. The combination enables a product that would have required a large engineering team just a few years ago.

### Relevant History or Prior Work
No prior version of GetARoof exists. This is a greenfield project built by a solo developer using AI-assisted coding tools. Existing solutions (booking.com, Google Hotels, Airbnb) require users to manually search each platform and do not support natural language group configuration or automated booking.

---

## 3. Goals & Non-Goals

### Goals
- Accept natural language accommodation requests including group composition, room requirements, dates, location constraints, and budget
- Search booking.com, homeToGo, and Interhome simultaneously via their affiliate APIs (with browser automation fallback)
- Use AI to evaluate availability and requirement matching for each result
- Present the 5 best-matching results with photos, description, exact location, and price
- Autonomously complete the booking process on the chosen platform up to — but not including — payment entry
- Support user accounts for booking history and saved preferences
- Generate affiliate commission revenue; support switching to transaction fees without significant rework
- Be straightforwardly extensible to additional accommodation platforms

### Non-Goals
- Processing or storing payment information (user always enters payment data directly)
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
- As any user, I want to see the top 5 matching results from multiple platforms side by side so that I can compare options without visiting each site.
- As any user, I want to click "Book Now" and have the app complete all booking form steps so that I only need to enter my payment details.
- As a returning user, I want to see my past bookings so that I can reference confirmation details or plan a return trip.
- As any user, I want to register and log in securely so that my searches and bookings are saved to my account.

---

## 5. Functional Requirements

- The system shall provide user registration and login with secure authentication (email/password minimum; extensible to OAuth providers).
- The system shall accept a free-text natural language input field where users describe their accommodation requirements including but not limited to: destination, date range, number and configuration of guests/rooms, location constraints, and budget.
- The system shall hand the user's raw input to an **AI intake agent** that owns the entire pre-search conversation: parsing, completeness assessment, refinement, and the decision to proceed.
- The AI intake agent shall parse the input and check whether the three minimum fields needed to produce a usefully scoped search are present: **destination/location**, **time range**, and **number of guests**. If any are missing or too ambiguous, the agent shall ask the user for them conversationally before continuing.
- Beyond minimum completeness, the AI intake agent shall assess whether the stated criteria are specific enough to avoid an unmanageably broad result set. If the agent judges the query to be too vague, it shall politely ask one or more targeted refinement questions to narrow the search — for example: *"Would you like breakfast included?"*, *"Do you need a parking space?"*, *"Is a pool important to you?"*, *"Are pets coming along?"*. The agent selects relevant questions based on context; it does not ask a fixed script.
- The AI intake agent decides — using its own judgment — when the accumulated requirements are specific enough to yield a well-focused search. Only then does it proceed; it does not rely on hard-coded rules or field-count thresholds.
- Once the AI intake agent is satisfied, it presents the user with a structured confirmation card summarising all extracted requirements (destination, dates, group composition, budget, room configuration, selected preferences). The search is initiated only after the user explicitly confirms this summary.
- The system shall search booking.com, homeToGo, and Interhome simultaneously for properties matching the parsed requirements.
- The system shall use affiliate APIs as the primary integration method for each platform, falling back to browser automation where the API does not support the required functionality.
- The system shall use AI to evaluate each candidate property for availability within the requested dates and conformance to all stated requirements (room count, bed configuration, location proximity, price).
- The system shall rank results and present the top 5 best-matching properties to the user, each including: photos, property description, exact map location, total price for the stay, and the source platform.
- The system shall provide a "Book Now" button on each result that initiates the autonomous booking flow.
- The system shall autonomously complete all booking form steps on the chosen platform (guest details, room selection, extras) using information stored in the user's profile, stopping before the payment entry step.
- The system shall detect when a booking platform requires guest data not yet present in the user's profile, prompt the user to supply only the missing information, persist that data to the profile, and reuse it for subsequent bookings on any platform.
- The system shall hand off the booking session to the user at the payment step, requiring the user to enter only their payment information.
- The system shall record completed bookings in the user's booking history.
- The system shall implement a platform adapter interface that allows new accommodation platforms to be integrated with minimal changes to core application logic.
- The system shall implement a monetization abstraction layer supporting affiliate commission tracking, with the ability to switch to or combine transaction fee charging without requiring architectural changes.
- The system shall never store, transmit, or log user payment information.

---

## 6. Non-Functional Requirements

- **Performance**: Search results shall be returned within 30 seconds of query submission under normal operating conditions. The UI shall display a progress indicator during search.
- **Scalability**: The system shall be deployable to any major cloud provider or self-hosted environment without code changes. It shall support horizontal scaling of the backend.
- **Security**: User passwords shall be stored as salted hashes. All communication shall be over HTTPS. Authentication tokens shall be short-lived with refresh token rotation. Payment data shall never pass through GetARoof servers.
- **Accessibility**: The web application shall conform to WCAG 2.1 Level AA.
- **Reliability**: The system shall handle individual platform failures gracefully — if one platform's search fails, results from the remaining platforms shall still be returned with a user-facing notice.
- **Maintainability**: Platform integrations shall be isolated behind adapter interfaces so that changes to one platform do not affect others. AI provider calls shall be abstracted so the underlying model can be swapped without changes to business logic.
- **Extensibility**: Adding a new platform integration shall require only implementing the platform adapter interface and registering the new adapter — no changes to core search or booking orchestration logic.

---

## 7. UX / Design

### User Flows

**Search Flow**
1. User logs in and lands on the home screen.
2. User types a natural language description of their holiday requirements into a single prominent input field and submits.
3. The AI intake agent processes the input and responds conversationally in the same interface:
   - If minimum information (destination, dates, number of guests) is incomplete, the agent asks for what is missing.
   - If the criteria are present but too broad for a focused search, the agent politely explains this and asks targeted follow-up questions to narrow the requirements (e.g., parking, breakfast, pool, pets). The agent chooses which questions to ask based on context.
   - The user answers in free text; the agent continues refining until it judges the requirements specific enough.
4. Once the AI intake agent is satisfied, it presents a structured confirmation card summarising the full set of requirements. The user reviews and clicks "Search" to confirm.
5. A progress indicator shows that multiple platforms are being searched simultaneously.
6. The top 5 results are displayed as cards: photo gallery, property name, platform source, total price, distance to key location, brief AI-generated match summary.
7. User browses results and selects one.
8. A detail view shows full description, all photos, map, room breakdown, and a "Book Now" button.

**Booking Flow**
1. User clicks "Book Now."
2. The app confirms the booking details (property, dates, guests, total price) and asks the user to confirm.
3. The app begins the autonomous booking process. If the target platform requires guest data not yet in the user's profile, the app pauses and prompts the user for only the missing fields.
4. The user supplies the missing data; the app stores it and continues autonomously.
5. The user is presented with the platform's payment screen (or a clear handoff instruction) to enter payment details.
6. Upon payment completion, the booking confirmation is stored in the user's GetARoof account.

**Account Flow**
- Registration: email + password, basic profile (name, preferred language).
- Login: email/password (MVP); OAuth extensible.
- Profile: stores guest data collected during previous bookings (e.g., names, nationalities, dates of birth) for reuse across platforms.
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
- booking.com Partner/Affiliate API
- homeToGo Affiliate API
- Interhome Affiliate API
- AI model provider (OpenAI, Anthropic, or other — swappable via Semantic Kernel)
- Browser automation library for fallback booking flows (e.g., Playwright for .NET)
- Map/geocoding service for location display and proximity calculation (e.g., OpenStreetMap / Nominatim, or provider-agnostic abstraction)

### Integrations
- Authentication: ASP.NET Core Identity (extensible to OAuth/OIDC providers)
- Email: transactional email service for registration/confirmation (provider-agnostic via abstraction)

### Tech Stack Notes
| Layer | Technology |
|-------|-----------|
| Frontend | Blazor WebAssembly |
| Backend | ASP.NET Core Web API |
| AI Orchestration | Microsoft Semantic Kernel |
| Browser Automation | Playwright for .NET |
| Database | PostgreSQL (production); SQLite acceptable for local development |
| Containerization | Docker + Docker Compose |
| Hosting | Cloud-agnostic; any container-capable host |

---

## 9. Milestones / Timeline

| Milestone | Description | Target Date |
|-----------|-------------|-------------|
| Phase 1: Foundation | Project scaffold, authentication, user accounts, database schema, CI pipeline | TBD |
| Phase 2: Search Core | NL input parsing, Semantic Kernel integration, first platform adapter (Interhome), result display | TBD |
| Phase 3: Multi-Platform Search | booking.com and homeToGo adapters, parallel search, graceful failure handling | TBD |
| Phase 4: Booking Automation | Autonomous booking flow via API/Playwright, user handoff at payment step, booking history | TBD |
| Phase 5: Polish & Launch | UI refinement, WCAG compliance, affiliate tracking validation, performance testing, public launch | TBD |

---

## 10. Open Questions / Risks

### Open Questions
- What should happen if fewer than 5 results meet the requirements — show fewer results, or relax constraints and flag relaxed ones?
- Should the app support multiple saved searches, or only the most recent?
- What specific guest data fields do the three initial platforms require for booking (to be confirmed during platform integration)?

### Risks
- **Affiliate API restrictions**: booking.com and others have strict terms of use for their affiliate programs. Automated booking via affiliate API may be restricted or require partner approval. Browser automation may violate terms of service for some platforms.
- **Browser automation fragility**: Automated form-filling is sensitive to platform UI changes. Platforms may also deploy bot detection that blocks automation.
- **AI parsing accuracy**: Complex natural language queries (especially group room configurations) may be misinterpreted, leading to incorrect search parameters or a search scope that is too broad. This is mitigated by the AI intake agent (see Section 5 and the Search Flow in Section 7), which conversationally fills gaps, asks refinement questions when criteria are too vague, and presents a structured confirmation card that the user must approve before any search begins. The user always has the opportunity to correct misinterpretations before they affect results. Residual risk: the AI may misjudge when criteria are "specific enough", either searching too early on broad criteria or asking unnecessarily many questions. Prompt engineering and user testing during development will be used to calibrate this judgment.
- **Search latency**: Searching 3+ platforms in parallel with AI evaluation of results may exceed acceptable response times. Streaming/progressive results display may be needed.
- **Platform availability**: External APIs and scraped sites may change or become unavailable without notice, requiring ongoing maintenance.
- **Regulatory**: GDPR compliance is required for European users. User data handling, consent, and the right to deletion must be designed in from the start.
