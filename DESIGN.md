# Design decisions and tradeoffs

RTEvents is a proof of concept for event management, ticket reservations, and asynchronous payment processing. Much of the implementation prioritizes a readable workflow, simple local setup, and a small number of moving parts. It was written with an understanding that production requirements around scalability, performance, reliability, and operations would require further work.

This document describes the current implementation and its limitations. It does not claim measured throughput or production readiness. Known correctness gaps are called out separately from architectural tradeoffs; accepting a simpler architecture does not make those gaps intended behavior.

## Application structure

The solution separates HTTP concerns (`RTEvents.API`), application services (`RTEvents.Core`), and persistence and entities (`RTEvents.Database`). Controllers map requests and responses, services coordinate workflows, and entities contain operations such as holding tickets and changing payment status.

Services use EF Core's `RTEventsDbContext` directly. This keeps queries and transaction boundaries visible without adding a repository abstraction. The tradeoff is that application services depend on EF Core and the persistence model. For this scope, that coupling keeps the implementation straightforward; additional abstractions should follow an actual need rather than being a prerequisite for growth.

## Relational persistence and purchase transactions

SQL Server is the shared source of truth for events, inventory, purchases, payments, and messaging records. A purchase uses a database transaction to persist the ticket hold, purchase, payment, idempotency record, and outgoing payment request together.

There are two saves within that transaction: the first obtains the generated payment ID, and the second persists the outbox payload that references it. This makes the sequence easy to follow and keeps the outgoing request consistent with the purchase, at the cost of another database round trip and a longer transaction.

Each requested ticket becomes an entity and a database row. That is convenient for ticket-level state, but large orders increase allocations, tracking overhead, and transaction size. The API currently has no practical per-order quantity limit beyond available inventory and the integer validation range.

## Inventory and concurrency

`Event.AvailableTicketCount` provides a stored inventory counter. Holding tickets updates that counter, and the event's SQL Server row version provides optimistic concurrency protection against competing updates.

This is a reasonable starting point for a POC, but every purchase for the same event competes to update the same row. A popular event can therefore become a contention point even when the API has multiple replicas. Concurrency exceptions currently have no specific recovery or conflict response and fall through to generic error handling.

## Asynchronous payments and the outbox

The purchase response represents a pending purchase. A payment request is stored in the outbox inside the purchase transaction, allowing processing to happen later instead of requiring an external payment operation during the HTTP request.

`MessageBus` simulates a messaging integration with local database tables. The payment-response endpoint supplies simulated results. This demonstrates the separation between accepting a purchase and receiving its payment outcome without requiring a broker or payment provider for local development.

`OutboxHandler` runs inside the API process and waits 90 seconds between processing passes. This keeps deployment simple, but couples worker resources and lifecycle to the API. The polling delay also adds latency, and each pass currently loads all eligible messages.

Moving processing into a separately deployed worker would allow independent scaling. Safe concurrent workers would additionally need coordinated message claiming, bounded batches, and idempotent handling. Multiple instances can read and process the same pending records. The attempt counter and dead-letter status are illustrative, not a complete delivery and retry policy. Malformed responses remain eligible for later polling, and processing exceptions propagate out of the worker loop.

A real integration would need defined delivery guarantees, duplicate and out-of-order response handling, retry backoff, and recovery after partial failures.

## Queries and reporting

Ticket availability currently projects held and sold counts through EF Core so the database performs the aggregation. It does not load the event's entire ticket collection into application memory. Reporting and querying does not implement caching so each request requires database work, which likely would be improved with proper indexing.

A very simple implementation is used to illustrate reporting: select a page of events, retrieve their ticket details, and calculate totals in memory. Paging events does not bound the number of tickets retrieved. Large events or a large `take` can produce substantial database traffic.

The first improvements would be database-side report aggregation and a validated maximum page size. Current offset pagination also becomes less attractive for deep pages, and ordering only by date lacks a unique tie-breaker.

## Data growth and indexes

The model includes primary keys and relationship indexes, but no dedicated indexes for the pending-outbox and unprocessed-response polling predicates. There is no retention or archival process for messages, outbox records, or idempotency keys.

These choices keep the schema and background work small for a demo. Over time, growing history increases storage and can make polling more expensive. Production work would include query-plan review, targeted indexes, and explicit retention periods. Idempotency-key retention must preserve the supported retry window.

## Cancellation

Async business endpoints pass request cancellation through services to applicable EF Core queries, saves, and transaction operations. The worker similarly passes its shutdown token through message processing. This lets work respond to disconnected clients or application shutdown.

Purchase rollback deliberately uses an uncancelled token so cleanup can finish after request cancellation. Cancellation is cooperative and does not guarantee that a commit did not occur; reliable replay remains necessary when a client does not receive a response.

## Known functional gaps

The following are incomplete behaviors to resolve before relying on the workflow, rather than performance optimizations to defer indefinitely:

- **Idempotency:** A new key is not assigned the resulting purchase ID, so the initial purchase cannot reliably be replayed. Concurrent requests with the same key can race between lookup and insertion; the unique key alone does not provide a successful replay response.
- **Reservation lifecycle:** Failed payments do not release held tickets or restore inventory, and abandoned holds have no expiration mechanism.
- **Purchase state:** Payment processing changes payment and ticket states but does not transition the purchase from its pending status.
- **Inventory consistency:** Updating event capacity does not recalculate the stored available count. Reporting uses that stored count, while the availability endpoint derives its answer from capacity and ticket statuses.
- **Pricing and sales:** Pricing is a placeholder. Reporting currently includes held ticket costs in sales, and its totals cover the selected page of events rather than all events.
