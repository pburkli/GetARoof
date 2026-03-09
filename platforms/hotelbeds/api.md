# Hotelbeds APItude API

This document covers the Hotelbeds APItude API endpoints relevant to GetARoof's use case:
searching real hotels with images and booking via the Hotelbeds wholesale network (~173,000 hotels worldwide).

- **Test base URL:** `https://api.test.hotelbeds.com`
- **Production base URL:** `https://api.hotelbeds.com`
- **Image base URL:** `https://photos.hotelbeds.com/giata/`

---

## Environments

### Test (Evaluation)

- Same servers and data as production — returns **real hotels, real images, real availability**
- Bookings do **not** create actual reservations or credit card charges
- **Quota:** 50 requests/day (403 error if exceeded)
- Upgrade to Certification environment via your dashboard for higher quotas
- Signup: https://developer.hotelbeds.com/register/ (instant, individual developer, no company needed)

### Production

- Requires completing a 5-step certification process
- Must contact apitude@hotelbeds.com for a commercial agreement
- Commission-based pricing (Net or Commissionable model, negotiated per partner)
- No public pricing — discuss with Hotelbeds sales

---

## Authentication

All API calls require two headers computed from your API key and secret.

### Headers

| Header | Value |
|--------|-------|
| `Api-key` | Your API key (from developer dashboard) |
| `X-Signature` | `SHA256(apiKey + secret + timestampInSeconds)` — hex-encoded |
| `Content-Type` | `application/json` |
| `Accept` | `application/json` |

### Computing X-Signature

```bash
echo -n "${apiKey}${secret}$(date +%s)" | sha256sum
```

The signature changes every second. Generate it immediately before each request.

---

## Part 1 — Hotel Content API

Use the Content API to retrieve hotel metadata: photos, descriptions, facilities, room info. This data changes infrequently — cache it locally.

### 1.1 Get Hotels

```
GET /hotel-content-api/1.0/hotels?destinationCode={code}&from=1&to=100
```

Returns hotel metadata including codes, names, descriptions, facilities, images, and contact info.

### 1.2 Hotel Images

Images are returned as relative paths in the Content API response. Construct full URLs using the base URL.

**Base URL:** `https://photos.hotelbeds.com/giata/`

**Available sizes:**

| Size | Width | URL pattern |
|------|-------|-------------|
| Thumbnail | 74px | `/giata/small/{path}` |
| Medium | 117px | `/giata/medium/{path}` |
| Standard | 320px | `/giata/{path}` |
| Large | 800px | `/giata/bigger/{path}` |
| XL | 1024px | `/giata/xl/{path}` |
| XXL | 2048px | `/giata/xxl/{path}` |
| Original | varies | `/giata/original/{path}` |

**Example:** Path `29/290725/290725a_hb_ro_037.jpg` becomes:
- Standard: `https://photos.hotelbeds.com/giata/29/290725/290725a_hb_ro_037.jpg`
- Large: `https://photos.hotelbeds.com/giata/bigger/29/290725/290725a_hb_ro_037.jpg`

**Image categories:**

| Code | Meaning |
|------|---------|
| `GEN` | General hotel views |
| `HAB` | Guest rooms |
| `RES` | Restaurant / dining |

**Ordering:**
- `visualOrder = 0` → primary hotel image
- `order` → rank within same category

---

## Part 2 — Hotel Booking API

Covers the complete booking process: search availability, validate rates, confirm booking.

### 2.1 Availability Search

```
POST /hotel-api/1.0/hotels
```

**Request body:**

```json
{
  "stay": {
    "checkIn": "2026-04-15",
    "checkOut": "2026-04-17"
  },
  "occupancies": [
    {
      "rooms": 1,
      "adults": 2,
      "children": 0
    }
  ],
  "hotels": {
    "hotel": [3424, 168]
  }
}
```

You can search by:
- **Hotel codes:** `"hotels": { "hotel": [3424, 168] }` (up to 2,000 per request)
- **Destination code:** `"destination": { "code": "PMI" }` (IATA or Hotelbeds destination code)
- **Geolocation:** `"geolocation": { "latitude": 48.87, "longitude": 2.33, "radius": 20, "unit": "km" }`
- **Keywords/filters:** amenities, board types, price range, flexible dates

**Response includes per hotel:**
- Hotel code, name, category
- Available rooms with `rateKey` (unique identifier for each rate)
- `rateType`: either `"BOOKABLE"` (ready to book) or `"RECHECK"` (must validate first)
- Board type (e.g., `BB` = Bed & Breakfast, `RO` = Room Only)
- Cancellation policies with fees and dates
- Net price, selling rate, daily breakdown

---

### 2.2 CheckRate (Conditional)

Only required when `rateType === "RECHECK"` in the availability response. Re-validates pricing and availability.

```
POST /hotel-api/1.0/checkrates
```

```json
{
  "rooms": [
    {
      "rateKey": "rateKey-from-availability-response"
    }
  ]
}
```

- Groups up to 10 rates per request
- Returns updated pricing and expanded `rateComments` (facility closures, check-in info, parking, taxes)
- After this call, `rateType` should be `"BOOKABLE"`

**Skip this step** if the availability response already returned `rateType === "BOOKABLE"`.

---

### 2.3 Confirm Booking

```
POST /hotel-api/1.0/bookings
```

```json
{
  "holder": {
    "name": "John",
    "surname": "Smith"
  },
  "rooms": [
    {
      "rateKey": "confirmed-rateKey",
      "paxes": [
        {
          "roomId": 1,
          "type": "AD",
          "name": "John",
          "surname": "Smith"
        }
      ]
    }
  ],
  "clientReference": "GETAROOF-12345",
  "remark": "Late arrival after 22:00",
  "tolerance": 2.00
}
```

| Field | Required | Notes |
|-------|----------|-------|
| `holder.name` | Yes | Booking holder first name |
| `holder.surname` | Yes | Booking holder surname |
| `rooms[].rateKey` | Yes | From availability or checkrate response |
| `rooms[].paxes[]` | Yes | Guest details per room |
| `paxes[].type` | Yes | `AD` = adult, `CH` = child |
| `clientReference` | Yes | Your internal booking reference |
| `remark` | No | Free-text note for the hotel |
| `tolerance` | No | Accepted price variation in currency units (e.g., 2.00 = accept up to €2 price change) |

**Response includes:**
- Hotelbeds booking reference
- Hotel confirmation details
- Final pricing
- Cancellation policy

---

### 2.4 Manage Bookings

**List bookings:**
```
GET /hotel-api/1.0/bookings?from=2026-04-01&to=2026-04-30
```

**Get booking details:**
```
GET /hotel-api/1.0/bookings/{bookingId}
```

**Cancel booking:**
```
DELETE /hotel-api/1.0/bookings/{bookingId}
```

**Simulate cancellation (check fees without cancelling):**
```
DELETE /hotel-api/1.0/bookings/{bookingId}?cancellationFlag=SIMULATION
```

---

## Part 3 — Booking Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. Content API: Fetch hotel photos, descriptions, facilities    │
│    GET /hotel-content-api/1.0/hotels                            │
│    → Cache locally (changes infrequently)                       │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│ 2. Availability Search                                          │
│    POST /hotel-api/1.0/hotels                                   │
│    → dates, occupancy, destination/hotels/geolocation           │
│    ← returns hotels with rateKey and rateType per room          │
└──────────────────────────────┬──────────────────────────────────┘
                               │
                    ┌──────────▼──────────┐
                    │ rateType = RECHECK? │
                    └──────────┬──────────┘
                       yes │        │ no
                    ┌──────▼──────┐  │
                    │ 3. CheckRate│  │
                    │ POST        │  │
                    │ /checkrates │  │
                    └──────┬──────┘  │
                           │         │
                    ┌──────▼─────────▼────────────────────────────┐
                    │ 4. Confirm Booking                           │
                    │    POST /hotel-api/1.0/bookings              │
                    │    → rateKey + holder + paxes                │
                    │    ← booking reference + confirmation        │
                    └─────────────────────────────────────────────┘
```

---

## Part 4 — Payment

Hotelbeds operates as a **wholesaler/intermediary**. Payment is handled entirely by Hotelbeds — you do **not** process payments yourself.

### How it works

1. **Guest pays you** (or your platform) — you collect payment however you choose
2. **Hotelbeds pays the hotel** — via VCC or bank transfer, depending on their contract with the hotel
3. **You pay Hotelbeds** — for the net rate, per your commercial agreement (invoiced)

### Payment methods (Hotelbeds → Hotel)

| Method | Description |
|--------|-------------|
| **VCC (Virtual Credit Card)** | Hotelbeds issues a single-use VCC per booking. The hotel charges it around check-in. This is the default for most bookings. |
| **Bank transfer** | Wire transfer for some properties, depending on contract |

> **Important:** Hotelbeds issues the VCC to the hotel — you do not need to issue your own VCC. This is different from the Amadeus model where you must supply a credit card in the booking request. With Hotelbeds, payment is abstracted away from the API consumer.

### Do you need Airwallex VCC?

**No.** Unlike Amadeus, Hotelbeds handles hotel payment internally. You only need to settle with Hotelbeds per your commercial agreement (typically monthly invoicing). If your app collects payment from end users, you handle that separately (Stripe, Airwallex payments, etc.) — but the hotel gets paid by Hotelbeds, not by you directly.

---

## Pricing Models

Your commercial agreement with Hotelbeds determines which model you use.

### Net Model

- You receive **net rates** (Hotelbeds' cost)
- If `hotelMandatory = false` or `null`: you can add your own markup
- If `hotelMandatory = true`: you must use the `sellingRate` provided
- Relevant fields: `net`, `dailyNet`, `totalNet`

### Commissionable Model

- Prices already include Hotelbeds' commission in `sellingRate`
- You must always use `sellingRate` — no custom markup
- Relevant fields: `commission`, `sellingRate`, `totalSellingRate`

---

## Error Handling

Standard HTTP status codes apply. Hotelbeds-specific:

- **403** — Quota exceeded (50 req/day on evaluation) or authentication failure
- **400** — Invalid request parameters
- **500** — Internal server error — retry with same parameters

Always check `rateType` before booking — attempting to book a `RECHECK` rate without calling CheckRate first may fail.

---

## Notes & Limitations

- **Test environment uses real data** — real hotel names, real photos, real availability. Bookings are simulated (no charges).
- **50 requests/day** on evaluation keys — sufficient for POC, upgrade to certification for more.
- **Content API is separate** — availability search does not return images. Fetch and cache hotel content separately.
- **Certification required for production** — 5-step technical review including a live test booking (6+ months out, then cancelled).
- **No public pricing** — production costs are negotiated with Hotelbeds sales (apitude@hotelbeds.com).
- **Rate comments** — contain important info (facility closures, COVID notices, tax details). Parse and display these to users.
- **Currency** — configured per commercial agreement. Either single currency or up to 3 (EUR, USD, GBP).
