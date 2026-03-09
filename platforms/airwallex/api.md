# Airwallex API

This document covers the Airwallex API endpoints relevant to GetARoof's use case:
accepting payments from users and issuing single-use virtual cards (VCCs) to pay hotels via Amadeus.

- **Sandbox base URL:** `https://api-demo.airwallex.com`
- **Production base URL:** `https://api.airwallex.com`
- **API version:** `2024-03-31` and later

---

## Authentication

All API calls require a short-lived Bearer token obtained by exchanging your API credentials.

### Obtain an Access Token

```
POST /api/v1/authentication/login
```

**Headers:**

| Header | Value |
|--------|-------|
| `x-client-id` | Your client ID (from Dashboard → Settings → Developer → API keys) |
| `x-api-key` | Your API key |

**Response:**

```json
{
  "token": "eyJhbGciOiJSUzI1NiJ9...",
  "expires_at": "2026-03-05T10:30:00Z"
}
```

Tokens are valid for **30 minutes**. Reuse the token across requests within that window rather than generating a new one per request.

All subsequent requests include:
```
Authorization: Bearer <token>
```

---

## Part 1 — Accepting User Payments

Use Airwallex's Payment Intent flow. Card data never touches your server — it is submitted directly to Airwallex via their client-side JS Elements (hosted fields).

### 1.1 Create a Payment Intent

Create a PaymentIntent server-side for each booking attempt.

```
POST /api/v1/pa/payment_intents/create
```

**Request body:**

```json
{
  "amount": 90.00,
  "currency": "EUR",
  "merchant_order_id": "GETAROOF-ORDER-12345",
  "descriptor": "GetARoof Hotel Booking",
  "customer_id": "cus_abc123",
  "return_url": "https://yourapp.com/booking/confirm",
  "payment_method_options": {
    "card": {
      "three_ds_action": "FORCE_3DS"
    }
  },
  "request_id": "unique-idempotency-key-001"
}
```

| Field | Required | Description |
|-------|----------|-------------|
| `amount` | Yes | Booking amount |
| `currency` | Yes | ISO 4217 code (e.g., `EUR`, `CHF`) |
| `merchant_order_id` | Yes | Your internal order reference |
| `customer_id` | No | Improves frictionless 3DS authentication |
| `return_url` | Yes | Where to redirect after 3DS |
| `request_id` | Yes | Unique key for idempotency |

**Response:**

```json
{
  "id": "int_abc123xyz",
  "client_secret": "cs_live_abc123...",
  "status": "REQUIRES_PAYMENT_METHOD",
  "amount": 90.00,
  "currency": "EUR"
}
```

Pass `id` and `client_secret` to your frontend.

---

### 1.2 Confirm the Payment (Client-Side)

On the frontend, use **Airwallex.js Drop-in Element** or **Embedded Card Element** — the guest enters card details directly into Airwallex-hosted iframes (PCI-safe):

```js
import { loadAirwallex, createElement } from 'airwallex-payment-elements';

await loadAirwallex({ env: 'demo', origin: window.location.origin });

const cardElement = createElement('card');
cardElement.mount('#card-container');

// On form submit:
const { paymentIntent, error } = await confirmPaymentIntent({
  element: cardElement,
  id: intentId,         // from server
  client_secret: secret // from server
});
```

If 3DS is required, Airwallex handles the redirect automatically and returns to your `return_url` with:
- `?payment_intent_id=int_abc123xyz&succeeded=true`

---

### 1.3 Verify Payment Success

After the redirect (or via webhook), confirm the final status server-side:

```
GET /api/v1/pa/payment_intents/{id}
```

Check that `status === "SUCCEEDED"` before proceeding to issue a VCC.

**Webhook event to listen for:** `payment_intent.succeeded`

---

### 1.4 Refund a Payment (if booking cancelled)

```
POST /api/v1/pa/refunds/create
```

```json
{
  "payment_intent_id": "int_abc123xyz",
  "amount": 90.00,
  "currency": "EUR",
  "reason": "CUSTOMER_REQUESTED",
  "request_id": "unique-refund-key-001"
}
```

---

## Part 2 — Issuing Virtual Cards (VCC)

Once a user payment succeeds, issue a single-use VCC to pay the hotel via Amadeus.

### 2.1 Create a Cardholder

Each VCC must be associated with a cardholder. For hotel payments issued on behalf of your platform, create one shared **organisation-level** cardholder (done once), or create per-user cardholders for consumer cards.

```
POST /api/v1/issuing/cardholders/create
```

```json
{
  "type": "INDIVIDUAL",
  "individual": {
    "name": {
      "first_name": "GetARoof",
      "last_name": "Platform"
    },
    "date_of_birth": "1990-01-01",
    "express_consent_obtained": "yes",
    "address": {
      "line1": "1 Test Street",
      "city": "Sydney",
      "state": "NSW",
      "postcode": "2000",
      "country": "AU"
    }
  },
  "email": "payments@getaroof.com",
  "request_id": "cardholder-setup-001"
}
```

**Response includes:** `cardholder_id` — store this, you'll reuse it for every VCC.

Wait until `status === "READY"` before issuing cards (usually near-instant in sandbox).

---

### 2.2 Create a Single-Use Virtual Card

Issue one VCC per hotel booking, locked to the exact booking amount.

```
POST /api/v1/issuing/cards/create
```

```json
{
  "program": {
    "purpose": "COMMERCIAL",
    "type": "DEBIT"
  },
  "form_factor": "VIRTUAL",
  "is_personalized": false,
  "cardholder_id": "52646a67-878f-46d6-b4b1-02601cd4c553",
  "created_by": "GetARoof Platform",
  "authorization_controls": {
    "allowed_transaction_count": "SINGLE",
    "allowed_merchant_categories": ["7011"],
    "transaction_limits": {
      "currency": "EUR",
      "limits": [
        {
          "amount": 90.00,
          "interval": "ALL_TIME"
        }
      ]
    }
  },
  "request_id": "vcc-booking-12345-001"
}
```

| Field | Value | Notes |
|-------|-------|-------|
| `form_factor` | `VIRTUAL` | No physical card |
| `allowed_transaction_count` | `SINGLE` | Single-use — auto-expires after one charge |
| `allowed_merchant_categories` | `["7011"]` | MCC 7011 = Hotels & Lodging |
| `transaction_limits.amount` | Booking total | Locks card to exact amount |
| `request_id` | Unique per booking | Use your booking ID |

**Response:**

```json
{
  "card_id": "c5caf0ab-287c-4f74-8cde-84d52352bda3",
  "card_status": "PENDING",
  "form_factor": "VIRTUAL",
  "brand": "VISA",
  "authorization_controls": { "..." : "..." }
}
```

The card transitions from `PENDING` → `ACTIVE` automatically within seconds.

---

### 2.3 Retrieve Sensitive Card Details

Once the card is `ACTIVE`, retrieve the full PAN and CVV to pass to Amadeus.

```
GET /api/v1/issuing/cards/{card_id}/details
```

**Response:**

```json
{
  "card_id": "c5caf0ab-287c-4f74-8cde-84d52352bda3",
  "card_number": "4111111111111111",
  "cvv": "123",
  "expiry_month": "08",
  "expiry_year": "2027"
}
```

> **Security note:** Only retrieve sensitive details server-side, never expose them to the client. This endpoint requires your server to be PCI DSS compliant, or use PCI Proxy to handle the forwarding to Amadeus without your server seeing raw card data.

---

### 2.4 Get Card Status

Poll or use webhooks to monitor card state:

```
GET /api/v1/issuing/cards/{card_id}
```

`card_status` values: `PENDING`, `ACTIVE`, `FROZEN`, `TERMINATED`

**Webhook event to listen for:** `issuing.card.active`

---

## Part 3 — Sample Workflow: Hotel Booking

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. User selects a hotel offer on GetARoof                       │
│    → offerId from Amadeus Hotel Search API                      │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│ 2. Server: Create Payment Intent                                │
│    POST /api/v1/pa/payment_intents/create                       │
│    → amount = hotel total, currency = EUR                       │
│    ← returns intent id + client_secret                          │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│ 3. Client: Guest enters card details                            │
│    Airwallex.js Drop-in Element (PCI-safe hosted fields)        │
│    → 3DS authentication handled automatically                   │
│    ← redirect to return_url with ?succeeded=true                │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│ 4. Server: Verify payment                                       │
│    GET /api/v1/pa/payment_intents/{id}                          │
│    → assert status === "SUCCEEDED"                              │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│ 5. Server: Issue single-use VCC                                 │
│    POST /api/v1/issuing/cards/create                            │
│    → SINGLE use, MCC 7011, locked to booking amount            │
│    ← returns card_id                                            │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│ 6. Server: Retrieve VCC details                                 │
│    GET /api/v1/issuing/cards/{card_id}/details                  │
│    ← card_number, cvv, expiry                                   │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│ 7. Server: Book hotel via Amadeus                               │
│    POST /v2/booking/hotel-orders                                │
│    → payment.method = CREDIT_CARD                               │
│    → paymentCard = VCC details from step 6                      │
│    ← bookingStatus: CONFIRMED + confirmationNumber              │
└──────────────────────────────┬──────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────┐
│ 8. VCC auto-expires after hotel charges it at check-in          │
│    Guest's real card details were never shared with hotel       │
└─────────────────────────────────────────────────────────────────┘
```

---

## Error Handling

| HTTP Status | Meaning | Common Cause |
|-------------|---------|--------------|
| 400 | Bad request | Missing or invalid fields |
| 401 | Unauthorized | Expired or invalid Bearer token |
| 404 | Not found | Invalid `card_id` or `payment_intent_id` |
| 422 | Unprocessable | Business rule violation (e.g. insufficient funds) |
| 429 | Rate limited | Too many requests — implement exponential backoff |
| 500 | Server error | Airwallex internal error — retry with same `request_id` |

Always include a unique `request_id` on write operations — Airwallex uses it for idempotency, so retrying with the same `request_id` is safe and will not create duplicate cards or charges.

---

## Notes & Limitations

- **Switzerland:** Card Issuing availability for CH-registered businesses is not explicitly confirmed in public docs — verify with Airwallex sales before building.
- **Card brand:** Issued VCCs are Visa by default.
- **Funds:** VCC spend draws from your Airwallex Wallet balance — ensure it is funded before issuing cards.
- **MCC restriction:** Setting `allowed_merchant_categories: ["7011"]` prevents the VCC from being used anywhere except hotels — recommended for security.
- **Single-use:** Once the hotel charges the VCC, it is automatically terminated.
- **PCI scope:** Retrieving raw card details via `/details` means your server is technically in PCI scope for that moment. Consider routing through PCI Proxy to remain fully out of scope.
- **PCI access must be enabled:** The `/details` endpoint requires explicit PCI access activation by your Airwallex Account Manager — request this during onboarding.
- **Onboarding:** KYB (Know Your Business) verification required before going live — typically 1–2 weeks.
- **Production requires a business entity:** Individual developers cannot activate a production Airwallex account. You must have a registered company (sole proprietorship may not suffice — verify with Airwallex). The sandbox works fine for individual developers, but production onboarding requires KYB with a legal business entity.

## Account Manager Configuration (Required Before Use)

Contact your Airwallex Account Manager to enable the following — none of these are self-serve:

| Setting | Notes |
|---------|-------|
| Issuing APIs | Must be explicitly enabled for your account |
| Program types | Specify which you need: `PREPAID`, `DEBIT`, `CREDIT`, or `DEFERRED_DEBIT`. Only enabled types work — others return 422. |
| PCI access | Required to call `GET /issuing/cards/{id}/details` for raw card numbers |
| Automatic Conversions | Needed if billing currency differs from wallet currency |
| Card creation limits | Default limits may be low — request higher limits for production |
| Spending transaction limits by currency | Configure per-card spend caps |

## Sandbox Gotchas

- **Individual cardholder `PENDING` status:** Individual cardholders may require manual approval in sandbox and stay `PENDING` until an Airwallex operator approves them. If stuck, use `COMPANY` type or contact support.
- **Program type:** In sandbox, test which `program.type` values are enabled for your account — only account-configured types will succeed.
- **Simulating transactions:** Airwallex sandbox supports simulated card charges to test webhook events (`issuing.transaction.created`, etc.).
