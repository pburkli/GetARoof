# Platform Overview

Summary of accommodation and payment platforms evaluated for GetARoof. See subdirectories for detailed API documentation per platform.

## Accommodation Platforms — Hotels

| Platform | Access | Real data in test | Images | Booking API | Status |
|----------|--------|-------------------|--------|-------------|--------|
| **Hotelbeds** | Instant, individual dev | Yes | Yes (Content API, 7 resolutions) | Yes (full flow) | **Primary — Phase 1** |
| **Amadeus** | Instant + 72h prod approval | Yes (prod only) | No (needs external source, e.g. Google Places) | Yes | **Phase 2** |
| Booking.com | Affiliate approval pending (via CJ) | Unknown | Yes | Yes (if approved) | Pending |
| Expedia | Approval-based, likely needs business entity | No (sandbox is fake data) | Yes (prod only) | Redirect only (Travel Redirect API) | Parked |

## Accommodation Platforms — Vacation Rentals

| Platform | Focus | Access | Sandbox | Images | Booking API | Status |
|----------|-------|--------|---------|--------|-------------|--------|
| Holidu | Aggregator (2000+ partners) | Developer hub is **supply-side only** (for property managers). Affiliate program is link-based (Awin/FlexOffers), no search API. | N/A | N/A | N/A | **No demand-side API** |
| Interhome | Chalets, apartments (Swiss-based, 40k properties) | Partner registration, welcomes indie agents | Yes (test env) | Yes | Yes (real-time + direct booking) | To evaluate |
| Novasol | Holiday homes (Scandinavia + Europe) | Contact partner@novasol.com | Unknown | Yes | Yes | To evaluate |
| Rentals United | Aggregator | Free, ~1 month onboarding + certification | Yes | Yes | Yes | To evaluate |
| Airbnb | Private homes, apartments | No public API, not accepting new developers | N/A | N/A | N/A | Not accessible |
| Vrbo/HomeAway | Vacation homes | Only through Expedia Rapid (needs business entity) | N/A | N/A | N/A | Not accessible |
| HomeToGo | Aggregator | Enterprise-focused, sales-led | Unknown | Unknown | Unknown | Not accessible |
| Atraveo/Casamundo/e-domizil | Holiday rentals | Acquired by HomeToGo, no standalone API | N/A | N/A | N/A | Not accessible |

## Payment Platforms

| Platform | Access | Status |
|----------|--------|--------|
| Airwallex | Sandbox instant; production requires business entity (KYB) | Parked — not needed if using Hotelbeds (handles hotel payment internally) |

## Detailed Documentation

- `hotelbeds/api.md` — Hotelbeds APItude (Booking API, Content API, images, booking flow, payment model)
- `amadeus/hotel-api.md` — Amadeus Self-Service Hotel APIs (search, offers, booking)
- `airwallex/api.md` — Airwallex (payment intents, VCC issuance, card details)

## Key Findings

- **Hotelbeds** is the only hotel platform that provides real hotel data with real images in a free test environment accessible to an individual developer without approval.
- **Holidu** looked promising but their developer hub (`developer.holidu.com`) is a **supply-side API only** — designed for property managers to list on Holidu, not for developers to search listings. Their affiliate program (via Awin/FlexOffers) is link-based (€0.16/click-out in DACH), not an API. No demand-side search API exists.
- **Interhome** is a strong option specifically for Swiss/Alpine vacation rentals (chalets, apartments). Their partner program welcomes independent agents. Access is partner-registration-based — apply via their partner program.
- **Amadeus** Self-Service API provides real hotel data in production but **no images** — a supplementary image API (e.g., Google Places) is required.
- **Booking.com** and **Expedia** require partner approval processes that range from weeks to months. Booking.com application is pending.
- **Airbnb** does not offer public API access and is not accepting new developer partnerships.
- **Airwallex** sandbox works for individual developers, but production VCC issuance requires a registered business entity. Not needed when using Hotelbeds, which handles hotel payment internally.
