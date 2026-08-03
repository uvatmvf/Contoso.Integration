# ADR-0003: Use queues for commands and topics for domain events

- Status: Accepted
- Date: 2026-08-03

## Context

The order workflow includes directed work requests and facts that may be consumed by multiple downstream components. Treating every message identically would blur ownership and make routing harder to understand.

Commands represent a request for one capability to perform work. Events represent facts that have already occurred and may interest multiple consumers.

## Decision

Use Service Bus queues for durable commands:

- `place-order`
- `reserve-inventory`

Use the `order-events` topic for domain events:

- `InventoryReserved`
- `PaymentAuthorized`

Use subscriptions and subject filters to route events to the appropriate Functions:

- `authorize-payment`
- `complete-order`

Functions perform units of work and publish subsequent events. Persistent order state and operation identifiers provide idempotency for at-least-once delivery.

## Consequences

### Positive

- Message intent is explicit.
- Commands have a clear consumer and ownership boundary.
- Events can support additional subscribers without changing the publisher.
- The workflow is observable as a sequence of business facts.
- Queue and subscription dead-lettering isolates poison messages.

### Tradeoffs

- Choreography requires careful event contracts and correlation.
- At-least-once delivery requires idempotent handlers.
- Eventual consistency replaces a single synchronous transaction.
- Subscription filters and event subjects must remain aligned.

## Follow-up

Add failure-path events and compensation behavior for inventory release or payment reversal.
