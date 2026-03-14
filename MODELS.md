# GetARoof — Domain Models & AI Contracts

This document defines the field-level structure of the core domain models and AI contract schemas. For architectural context, key decisions, and how these models flow through the system, see [ARCHITECTURE.md](ARCHITECTURE.md) Section 4.

---

## SearchRequest

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

### Room

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| Adults | int | Yes | ≥1 |
| ChildrenAges | int[] | No | Empty if no children |
| Label | string? | No | Display label: "grandparents", "family with kids" |

---

## HotelResult

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

### Offer

| Field | Type | Notes |
|-------|------|-------|
| OfferId | string | Platform-specific, needed for CheckRate |
| Rooms | OfferRoom[] | Room details for this combination |
| TotalPrice | decimal | |
| Currency | string | |
| CancellationPolicy | string? | |
| IsPriceVerified | bool | True after CheckRate |

### OfferRoom

| Field | Type | Notes |
|-------|------|-------|
| RoomName | string | "Double Standard", "Family Suite" |
| BoardType | string? | "Breakfast included", "Room only" |
| Adults | int | |
| Children | int | |
| BedDescription | string? | |

### Location

| Field | Type | Notes |
|-------|------|-------|
| Address | string | Street address |
| City | string | |
| Country | string | |
| Latitude | decimal | |
| Longitude | decimal | |

### Image

| Field | Type | Notes |
|-------|------|-------|
| Url | string | |
| Caption | string? | Optional description |

### NearbyPOI

| Field | Type | Notes |
|-------|------|-------|
| Name | string | "Zürich HB", "Migros Langstrasse" |
| Category | string | "train_station", "supermarket", "restaurant", etc. |
| DistanceMeters | int | Straight-line distance from hotel |

---

## AI Contract Schemas

### Intake Agent Output

The intake agent produces a JSON object matching the `SearchRequest` model. Example:

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

### LLM Ranking Output

The ranking step returns scored hotels:

```json
[
  {
    "hotelId": "HB-12345",
    "score": 92,
    "explanation": "Excellent fit: 2 rooms match your group exactly, breakfast included, 400m from the lake, well within budget at CHF 1'650 total."
  }
]
```

The backend maps `score` and `explanation` onto `HotelResult.MatchScore` and `HotelResult.MatchExplanation`.
