# Either.SampleApi

`Either.SampleApi` is a medium-complexity minimal API sample that demonstrates practical `WTorricos.Either` usage with vertical slices.

## Scope and boundaries

- Exactly 3 endpoints: `POST /orders`, `GET /orders/{id}`, `POST /payments/refund`
- Architecture: vertical slices under `Features/*` with shared `Infrastructure/Persistence/AppDbContext`
- Persistence: EF Core InMemory only (illustrative)
- Explicit non-goals: authentication, pagination/search, retries/polly, background jobs, and production hardening

## Either usage by endpoint

- `POST /orders`: `FlatMap` + `FlatMapAsync` + `ToCreatedResult`
- `GET /orders/{id}`: `FromNullable` + `MapFailure` + `ToOkResult`
- `POST /payments/refund`: `LINQ Syntax` + `MapAsync` + `ToOkResult` with cancellation-aware flow

## Failure mapping

All `Failure` values are translated to RFC7807 `ProblemDetails` by `Infrastructure/Http/FailureProblemDetailsMapper.cs`.

- Code-driven mapping: numeric error codes are used directly, then `VALIDATION_*` -> `400`, `*NOT_FOUND*` -> `404`, `CONFLICT_*` -> `409`
- Severity fallback mapping: `Info/Warning` -> `400`, `Error/Critical` -> `500`

## Cancellation behavior

`POST /payments/refund` catches `OperationCanceledException` and returns HTTP `499` intentionally to make cancellation explicit in this sample.

## Running the sample

```powershell
dotnet run --project samples\Either.SampleApi\Either.SampleApi.csproj
```

## curl examples

### POST /orders success

```bash
curl -i -X POST http://localhost:5000/orders \
  -H "Content-Type: application/json" \
  -d '{"customerName":"Ada Lovelace","amount":120.00,"currency":"USD"}'
```

### POST /orders failure (validation)

```bash
curl -i -X POST http://localhost:5000/orders \
  -H "Content-Type: application/json" \
  -d '{"customerName":"","amount":0,"currency":"USD"}'
```

### GET /orders/{id} success

```bash
curl -i http://localhost:5000/orders/{id}
```

### GET /orders/{id} failure (not found)

```bash
curl -i http://localhost:5000/orders/00000000-0000-0000-0000-000000000000
```

### POST /payments/refund success

```bash
curl -i -X POST http://localhost:5000/payments/refund \
  -H "Content-Type: application/json" \
  -d '{"orderId":"{id}","amount":25.00,"reason":"Partial reimbursement"}'
```

### POST /payments/refund failure (amount exceeds order)

```bash
curl -i -X POST http://localhost:5000/payments/refund \
  -H "Content-Type: application/json" \
  -d '{"orderId":"{id}","amount":9999.00,"reason":"Too much"}'
```
