# Platform Overview

Summary of accommodation and payment platforms evaluated for GetARoof. See subdirectories for detailed API documentation per platform.

## Accommodation Platforms

| Platform | Access | Real data in test | Images | Booking API | Status |
|----------|--------|-------------------|--------|-------------|--------|
| **Hotelbeds** | Instant, individual dev | Yes | Yes (Content API, 7 resolutions) | Yes (full flow) | **Primary — Phase 1** |
| **Amadeus** | Instant + 72h prod approval | Yes (prod only) | No (needs external source, e.g. Google Places) | Yes | **Phase 2** |
| Booking.com | Affiliate approval pending (via CJ) | Unknown | Yes | Yes (if approved) | Pending |
| Expedia | Approval-based, likely needs business entity | No (sandbox is fake data) | Yes (prod only) | Redirect only (Travel Redirect API) | Parked |

## Payment Platforms

| Platform | Access | Status |
|----------|--------|--------|
| Airwallex | Sandbox instant; production requires business entity (KYB) | Parked — not needed if using Hotelbeds (handles hotel payment internally) |

## Detailed Documentation

- `hotelbeds/api.md` — Hotelbeds APItude (Booking API, Content API, images, booking flow, payment model)
- `amadeus/hotel-api.md` — Amadeus Self-Service Hotel APIs (search, offers, booking)
- `airwallex/api.md` — Airwallex (payment intents, VCC issuance, card details)

## Key Findings

- **Hotelbeds** is the only platform that provides real hotel data with real images in a free test environment accessible to an individual developer without approval.
- **Amadeus** Self-Service API provides real hotel data in production but **no images** — a supplementary image API (e.g., Google Places) is required.
- **Booking.com** and **Expedia** require partner approval processes that range from weeks to months. Booking.com application is pending.
- **Airwallex** sandbox works for individual developers, but production VCC issuance requires a registered business entity. Not needed when using Hotelbeds, which handles hotel payment internally.
