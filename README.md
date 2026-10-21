# Booking Workflow API

University practice project modeling booking transitions, idempotent events, failure compensation, and service observability in ASP.NET Core.

## Behavior

Bookings move through `Idle`, `Reserved`, `Confirmed`, `CheckedIn`, `Completed` and `Cancelled` states. Events carry an idempotency key and correlation ID. The service includes a simulated failure path to exercise compensation behavior.

## Run

Requires the .NET 8 SDK:

```sh
dotnet restore Frameworks4.sln
dotnet run --project Frameworks4 -- --urls http://localhost:5000
```

| Endpoint | Purpose |
|---|---|
| `POST /api/booking/{processKey}/event` | Submit a booking event |
| `GET /api/booking/{processKey}` | Read current state |
| `GET /metrics` | Read application metrics |
| `GET /health/live` | Health-check endpoint |
| `GET /health/ready` | Readiness check |

Example first event:

```sh
curl -X POST http://localhost:5000/api/booking/demo/event -H "Content-Type: application/json" -d '{"event":"Reserve","idempotencyKey":"demo-1","correlationId":"example"}'
```

## Structure and limitations

`Controllers/` exposes HTTP endpoints, `Services/` stores state and metrics, `Models/` defines transitions, and `HealthChecks/` implements readiness. These directories are under `Frameworks4/`.

All state is in memory. Completion deliberately has a failure simulation; this is an educational workflow model. The liveness endpoint currently runs the registered health checks too, so it should not be described as unconditionally healthy.
