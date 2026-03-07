# Amadeus Self-Service Hotel APIs

This document covers the Amadeus Self-Service API endpoints relevant to hotel search and booking.
All endpoints use `https://test.api.amadeus.com` (test) or `https://api.amadeus.com` (production) as the base URL.

---

## Overview

| API | Version | Method | Path | Purpose |
|-----|---------|--------|------|---------|
| Hotel List | v1 | GET | `/v1/reference-data/locations/hotels/by-city` | Find hotels in a city |
| Hotel List | v1 | GET | `/v1/reference-data/locations/hotels/by-geocode` | Find hotels near coordinates |
| Hotel List | v1 | GET | `/v1/reference-data/locations/hotels/by-hotels` | Look up hotels by ID |
| Hotel Name Autocomplete | v1 | GET | `/v1/reference-data/locations/hotel` | Autocomplete hotel name search |
| Hotel Search | v3 | GET | `/v3/shopping/hotel-offers` | Get available rooms and prices |
| Hotel Search | v3 | GET | `/v3/shopping/hotel-offers/{offerId}` | Get pricing for a specific offer |
| Hotel Ratings | v2 | GET | `/v2/e-reputation/hotel-sentiments` | Get sentiment scores for hotels |
| Hotel Booking | v1 | POST | `/v1/booking/hotel-bookings` | Book a hotel offer (legacy) |
| Hotel Booking | v2 | POST | `/v2/booking/hotel-orders` | Book a hotel offer (current) |

---

## 1. Hotel List API (`v1`)

Finds hotels and returns their IDs, names, coordinates, and addresses. Use this as the first step to discover `hotelId` values for the Hotel Search API.

### Endpoints

#### Search by City

```
GET /v1/reference-data/locations/hotels/by-city
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `cityCode` | string | Yes | 3-letter IATA city code (e.g., `PAR`, `LON`) |
| `radius` | integer | No | Search radius, 1–300 (default: 5) |
| `radiusUnit` | string | No | `KM` or `MILE` (default: `KM`) |
| `chainCodes` | array[string] | No | Filter by 2-letter hotel chain codes |
| `amenities` | array[string] | No | Up to 3: `SWIMMING_POOL`, `SPA`, `FITNESS_CENTER`, `AIR_CONDITIONING`, `RESTAURANT`, `PARKING`, `PETS_ALLOWED`, `AIRPORT_SHUTTLE`, `BUSINESS_CENTER`, `DISABLED_FACILITIES`, `WIFI`, `MEETING_ROOMS`, `NO_KID_ALLOWED`, `TENNIS`, `GOLF`, `KITCHEN`, `ANIMAL_WATCHING`, `BABY-SITTING`, `BEACH`, `CASINO`, `JACUZZI`, `SAUNA`, `SOLARIUM`, `MASSAGE`, `VALET_PARKING`, `BAR`, `LOUNGE`, `MINIBAR`, `TELEVISION`, `WI-FI_IN_ROOM`, `ROOM_SERVICE`, `GUARDED_PARKG`, `SERV_SPEC_MENU` |
| `ratings` | array[integer] | No | Filter by star ratings, values 1–5, max 4 values |
| `hotelSource` | string | No | `BEDBANK`, `DIRECTCHAIN`, or `ALL` (default: `ALL`) |

#### Search by Geocode

```
GET /v1/reference-data/locations/hotels/by-geocode
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `latitude` | number | Yes | Decimal degrees, -90 to 90 |
| `longitude` | number | Yes | Decimal degrees, -180 to 180 |
| `radius` | integer | No | 1–300 (default: 5) |
| `radiusUnit` | string | No | `KM` or `MILE` (default: `KM`) |
| `chainCodes` | array[string] | No | 2-letter hotel chain codes |
| `amenities` | array[string] | No | Up to 3 (see above) |
| `ratings` | array[integer] | No | 1–5 stars, max 4 values |
| `hotelSource` | string | No | `BEDBANK`, `DIRECTCHAIN`, or `ALL` |

#### Search by Hotel IDs

```
GET /v1/reference-data/locations/hotels/by-hotels
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `hotelIds` | array[string] | Yes | 1–99 Amadeus property codes (8 characters each) |

### Response

```json
{
  "data": [
    {
      "hotelId": "ACPARF58",
      "chainCode": "AC",
      "name": "HOTEL NAME",
      "iataCode": "PAR",
      "geoCode": { "latitude": 48.87, "longitude": 2.31 },
      "address": { "countryCode": "FR" },
      "distance": { "value": 1.2, "unit": "KM" },
      "dupeId": 700169556
    }
  ],
  "meta": {
    "count": 1,
    "links": { "self": "..." }
  }
}
```

---

## 2. Hotel Name Autocomplete API (`v1`)

Returns up to 20 hotel name suggestions matching a keyword. Useful for search-as-you-type UX.

```
GET /v1/reference-data/locations/hotel
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `keyword` | string | Yes | 4–40 chars, alphanumeric + spaces/hyphens/apostrophes |
| `subType` | array[string] | Yes | `HOTEL_LEISURE` and/or `HOTEL_GDS` |
| `countryCode` | string | No | ISO 3166-1 alpha-2 (e.g., `FR`) |
| `lang` | string | No | ISO 639-1 language code (default: `EN`) |
| `max` | integer | No | 1–20 results (default: 20) |

### Response

```json
{
  "data": [
    {
      "id": 12345,
      "type": "location",
      "subType": "HOTEL_LEISURE",
      "name": "PARIS MARRIOTT",
      "iataCode": "PAR",
      "hotelIds": ["MCPARCDT"],
      "relevance": 95,
      "address": {
        "cityName": "Paris",
        "countryCode": "FR"
      },
      "geoCode": { "latitude": 48.87, "longitude": 2.31 }
    }
  ]
}
```

---

## 3. Hotel Search API (`v3`)

Returns available room offers with pricing, policies, and cancellation rules for given hotels and dates.

### Get Offers for Multiple Hotels

```
GET /v3/shopping/hotel-offers
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `hotelIds` | array[string] | Yes | 1–20 Amadeus property codes (8 chars each) |
| `adults` | integer | No | Number of guests, 1–9 (default: 1) |
| `checkInDate` | string | No | `YYYY-MM-DD` (default: today) |
| `checkOutDate` | string | No | `YYYY-MM-DD` (default: checkIn + 1 night) |
| `roomQuantity` | integer | No | 1–9 rooms (default: 1) |
| `currency` | string | No | ISO 4217 code (e.g., `EUR`, `USD`) |
| `priceRange` | string | No | e.g., `"200-300"` |
| `boardType` | string | No | `ROOM_ONLY`, `BREAKFAST`, `HALF_BOARD`, `FULL_BOARD`, `ALL_INCLUSIVE` |
| `paymentPolicy` | string | No | `GUARANTEE`, `DEPOSIT`, or `NONE` (default) |
| `countryOfResidence` | string | No | ISO 3166-1 country code; affects taxes/pricing |
| `bestRateOnly` | boolean | No | Return only the cheapest offer per hotel (default: `true`) |
| `lang` | string | No | Language for descriptions (default: English) |

### Get Pricing for a Specific Offer

```
GET /v3/shopping/hotel-offers/{offerId}
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `offerId` | string | Yes | Offer ID from a previous search response |
| `lang` | string | No | Language preference |

> **Note:** Offer IDs are temporary and expire. Always re-fetch before booking.

### Response

```json
{
  "data": [
    {
      "type": "hotel-offers",
      "hotel": {
        "hotelId": "HLPAR266",
        "chainCode": "HL",
        "name": "HILTON PARIS",
        "cityCode": "PAR",
        "latitude": 48.87,
        "longitude": 2.31
      },
      "available": true,
      "offers": [
        {
          "id": "OFFER_ID_STRING",
          "checkInDate": "2025-06-01",
          "checkOutDate": "2025-06-03",
          "rateCode": "RAC",
          "room": {
            "type": "A1K",
            "description": { "text": "Standard King Room", "lang": "EN" }
          },
          "guests": { "adults": 2 },
          "price": {
            "currency": "EUR",
            "base": "180.00",
            "total": "198.00",
            "taxes": [
              { "code": "VALUE_ADDED_TAX", "amount": "18.00", "included": false }
            ],
            "variations": {
              "changes": [
                { "startDate": "2025-06-01", "endDate": "2025-06-02", "base": "90.00" }
              ]
            }
          },
          "policies": {
            "paymentType": "GUARANTEE",
            "cancellations": [
              { "amount": "90.00", "deadline": "2025-05-30T23:59:00" }
            ]
          }
        }
      ]
    }
  ]
}
```

---

## 4. Hotel Ratings API (`v2`)

Returns sentiment analysis scores derived from guest reviews. Scores are on a 0–100 scale.

```
GET /v2/e-reputation/hotel-sentiments
```

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `hotelIds` | array[string] | Yes | 1–100 Amadeus hotel IDs |

### Response

```json
{
  "data": [
    {
      "type": "hotelSentiment",
      "hotelId": "TELONMFS",
      "overallRating": 83,
      "numberOfRatings": 4712,
      "numberOfReviews": 1204,
      "sentiments": {
        "sleepQuality": 87,
        "service": 91,
        "facilities": 79,
        "roomComforts": 85,
        "valueForMoney": 74,
        "catering": 82,
        "location": 95,
        "internet": 76,
        "staff": 93
      }
    }
  ],
  "meta": { "count": 1 }
}
```

---

## 5. Hotel Booking API

Two versions exist. **v2 is the current version** and supports more payment methods and richer guest data. v1 is legacy.

### v2 — Create Hotel Order (current)

```
POST /v2/booking/hotel-orders
Content-Type: application/vnd.amadeus+json
```

#### Request Body

```json
{
  "data": {
    "type": "hotel-order",
    "guests": [
      {
        "tid": 1,
        "title": "MR",
        "firstName": "John",
        "lastName": "Smith",
        "email": "john.smith@example.com",
        "phone": "+33123456789"
      }
    ],
    "travelAgent": {
      "contact": { "email": "agent@agency.com" }
    },
    "roomAssociations": [
      {
        "hotelOfferId": "OFFER_ID_STRING",
        "guestReferences": [
          { "guestReference": "1", "hotelLoyaltyId": "LOYALTY123" }
        ],
        "specialRequest": "High floor, away from elevator"
      }
    ],
    "payment": {
      "method": "CREDIT_CARD",
      "paymentCard": {
        "paymentCardInfo": {
          "vendorCode": "VI",
          "cardNumber": "4151289722471370",
          "expiryDate": "0826",
          "holderName": "John Smith",
          "securityCode": "123"
        }
      }
    }
  }
}
```

| Field | Required | Notes |
|-------|----------|-------|
| `type` | Yes | Must be `"hotel-order"` |
| `guests[].tid` | Yes | Unique integer per guest in the request |
| `guests[].firstName` | Yes | Max 56 chars |
| `guests[].lastName` | Yes | Max 57 chars |
| `guests[].email` | Yes | Max 90 chars |
| `guests[].phone` | Yes | E.123 format, max 199 chars |
| `guests[].childAge` | Conditional | Required if guest is a child |
| `roomAssociations[].hotelOfferId` | Yes | Offer ID from Hotel Search |
| `roomAssociations[].guestReferences` | Yes | Maps guest `tid` to rooms |
| `roomAssociations[].specialRequest` | No | Max 120 chars |
| `roomAssociations[].guestReferences[].hotelLoyaltyId` | No | Loyalty program ID, max 21 chars |
| `payment.method` | Yes | Only `CREDIT_CARD` supported in test |
| `payment.paymentCard.paymentCardInfo.vendorCode` | Yes | 2-char code (e.g., `VI`, `CA`, `AX`) |
| `payment.paymentCard.paymentCardInfo.expiryDate` | Yes | `MMYY` format |
| `payment.paymentCard.paymentCardInfo.securityCode` | No | Recommended; 3–4 chars |
| `travelAgent.contact.email` | Yes | Agency contact email |
| `arrivalInformation` | No | Optional flight arrival details |

#### Response (201 Created)

```json
{
  "data": {
    "type": "hotel-order",
    "id": "ORDER_ID",
    "self": "https://api.amadeus.com/v2/booking/hotel-orders/ORDER_ID",
    "hotelBookings": [
      {
        "type": "hotel-booking",
        "id": "BOOKING_ID",
        "bookingStatus": "CONFIRMED",
        "hotelProviderInformation": [
          { "hotelProviderCode": "AC", "confirmationNumber": "XYZ123" }
        ],
        "hotel": {
          "hotelId": "HLPAR266",
          "chainCode": "HL",
          "name": "HILTON PARIS"
        },
        "hotelOffer": { "...": "full offer details" },
        "payment": { "method": "CREDIT_CARD", "cardNumber": "415128XXXXXX1370" }
      }
    ],
    "guests": [
      { "tid": 1, "id": "GUEST_ID_FROM_AMADEUS" }
    ],
    "associatedRecords": [
      { "reference": "ABC123", "originSystemCode": "GDS" }
    ]
  }
}
```

`bookingStatus` values: `CONFIRMED`, `PENDING`, `CANCELLED`, `ON_HOLD`, `PAST`, `UNCONFIRMED`, `DENIED`, `GHOST`, `DELETED`

### v1 — Create Hotel Bookings (legacy)

```
POST /v1/booking/hotel-bookings
Content-Type: application/vnd.amadeus+json
```

Simpler request structure. Prefer v2 for new integrations.

```json
{
  "data": {
    "offerId": "OFFER_ID_STRING",
    "guests": [
      {
        "name": { "title": "MR", "firstName": "John", "lastName": "Smith" },
        "contact": { "phone": "+33123456789", "email": "john@example.com" }
      }
    ],
    "payments": [
      {
        "method": "creditCard",
        "card": {
          "vendorCode": "VI",
          "cardNumber": "4151289722471370",
          "expiryDate": "2026-08"
        }
      }
    ],
    "rooms": [
      { "guestIds": [1], "paymentId": 1, "specialRequest": "Non-smoking room" }
    ]
  }
}
```

#### Response (201 Created)

```json
{
  "data": [
    {
      "type": "hotel-booking",
      "id": "BOOKING_ID",
      "providerConfirmationId": "XYZ123",
      "associatedRecords": [
        { "reference": "ABC123", "originSystemCode": "GDS" }
      ]
    }
  ]
}
```

---

## Typical Flow

```
1. Hotel Name Autocomplete  →  /v1/reference-data/locations/hotel
   (optional, for search-as-you-type)

2. Hotel List               →  /v1/reference-data/locations/hotels/by-city
                               /v1/reference-data/locations/hotels/by-geocode
   Obtain: hotelId values

3. Hotel Ratings            →  /v2/e-reputation/hotel-sentiments
   (optional, enrich results with review scores)

4. Hotel Search             →  /v3/shopping/hotel-offers
   Obtain: offerId, pricing, policies

5. Confirm Offer Pricing    →  /v3/shopping/hotel-offers/{offerId}
   (recommended before booking to ensure price is still valid)

6. Hotel Booking            →  /v2/booking/hotel-orders
   Obtain: bookingStatus, confirmationNumber
```

---

## Common Error Codes

| Code | Title | Meaning |
|------|-------|---------|
| 477 | INVALID FORMAT | Missing or malformed parameter |
| 1205 / 8517 | INVALID CREDIT CARD | Card number failed validation |
| 3664 | NO ROOMS AVAILABLE | Hotel is sold out |
| 33554 / 36803 | PRICE CHANGED / OFFER EXPIRED | Re-fetch the offer and retry |
| 38420 | OFFER NOT FOUND | Offer ID is expired or invalid |
| 37200 | PRICE DISCREPANCY | Rate mismatch with provider |
| 3843 | EXCEEDS OCCUPANCY | Too many guests for room |
| 00011 | UNABLE TO PROCESS | Hotel provider error |
| 04070 | CONTACT HELPDESK | Amadeus system error |

---

## Notes & Limitations

- **Test environment:** Use large cities (LON, NYC, PAR) for best test data coverage. Do not perform excessive fake bookings — hotels may blacklist the account.
- **Offer ID expiry:** Offer IDs are short-lived. Always re-verify with `GET /v3/shopping/hotel-offers/{offerId}` immediately before booking.
- **Hotel source:** `BEDBANK` = OTA-style inventory, `DIRECTCHAIN` = hotel chain direct rates.
- **Payment:** Only credit card payments are supported in the self-service API test environment.
- **Authentication:** All endpoints require an OAuth 2.0 Bearer token. Obtain via `POST /v1/security/oauth2/token` with your API key and secret.
