# GetARoof — Product Roadmap

This document defines the high-level phases for building GetARoof from MVP to a multi-platform accommodation search agent.

---

## Phase 1 — MVP: Search-Only with Hotelbeds

**Goal:** Build a working accommodation search experience with real hotel data, real images, and real availability — without booking or payment.

**Platform:** Hotelbeds APItude (test environment, free evaluation keys)

**Scope:**

- AI intake agent interprets natural-language accommodation requests and extracts structured search parameters (city, dates, guests, budget, preferences)
- Search available hotels via Hotelbeds Booking API (availability endpoint)
- Enrich results with photos, descriptions, and facilities from Hotelbeds Content API
- Rank and present top matches to the user with explanation of match quality
- Verify availability of selected accommodation (CheckRate if needed)
- Point the user to the easiest external booking option (e.g., direct hotel website, Booking.com, or Expedia link) — no in-app booking

**Out of scope:**

- Booking or payment processing
- Airwallex VCC issuance
- Multi-platform search
- User accounts or persistence

**Key constraint:** 50 API requests/day on Hotelbeds evaluation keys — sufficient for demo and development.

---

## Phase 2 — Multi-Platform: Add Amadeus + Image API

**Goal:** Abstract the platform layer so multiple accommodation providers can be searched in parallel, and add Amadeus as a second source.

**Scope:**

- Define a platform-agnostic interface for accommodation search (common model for hotels, offers, images)
- Implement Hotelbeds adapter behind this interface (refactor from Phase 1)
- Implement Amadeus adapter (production Self-Service API — free tier, 72h approval, individual developer)
- Integrate a separate image API (e.g., Google Places) to supplement Amadeus results, which do not include hotel photos
- Merge, deduplicate, and rank results across platforms
- Compare pricing across providers for the same property

**Prerequisites:**

- Amadeus production API keys (apply at developers.amadeus.com, ~72h approval)
- Google Places API key or alternative image source for Amadeus hotels

**Out of scope:**

- Booking or payment processing
- Additional platforms beyond Hotelbeds and Amadeus

---

## Future Phases (not yet planned)

- **Booking integration:** In-app booking via Hotelbeds Booking API (requires commercial agreement) or redirect-based booking via Expedia Travel Redirect API
- **Payment processing:** Collect user payments via Stripe or Airwallex (requires business entity for production)
- **VCC issuance:** Issue single-use virtual cards via Airwallex to pay hotels through Amadeus (requires business entity + KYB)
- **Additional platforms:** Booking.com Demand API (pending affiliate approval), Expedia Rapid API, HomeToGo
- **User accounts:** Save search history, preferences, and past bookings
- **Speech input/output:** Natural language voice interaction (see PAT-19)
