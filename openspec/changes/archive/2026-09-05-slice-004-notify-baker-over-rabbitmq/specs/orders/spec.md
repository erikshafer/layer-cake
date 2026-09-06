# Delta: orders (slice-004-notify-baker-over-rabbitmq)

## MODIFIED Requirements

### Requirement: Placing an order produces exactly one baker task

Placing an order SHALL produce exactly one baker task for that order, observable via `GET /baker/tasks` returning exactly `200` with a camelCase array `[ { orderId, summary, createdAt } ]`, filterable with an optional `?orderId={id}` query parameter. The notification that creates the task SHALL be delivered through a RabbitMQ queue in both twins before the task is written; the task appears asynchronously after the `201`, so observers SHALL poll with a short timeout. Once present, the count for that order SHALL be exactly one (never zero after settling, never duplicated), including when the broker redelivers the notification. With no broker reachable, no task SHALL appear on either twin. The HTTP contract does not reveal which broker, queue, or client library either twin uses.

#### Scenario: placing_order_produces_exactly_one_baker_task

- **WHEN** a client places an order successfully and polls `GET /baker/tasks?orderId={id}` with a short timeout
- **THEN** exactly one task for that order appears, with its `orderId` matching the placed order and a non-empty `summary` and `createdAt`
