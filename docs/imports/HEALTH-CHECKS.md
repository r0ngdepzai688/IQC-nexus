# Import Health Checks Specification

## 1. Endpoints

| Endpoint | Type | Description | Healthy Criteria |
| :--- | :--- | :--- | :--- |
| `/health/live` | Liveness | Process is running | HTTP 200 |
| `/health/ready` | Readiness | Database reachable & migrations applied | HTTP 200, DB connected, 0 pending migrations |
| `/health/degraded` | Operational | Work queue backlog & poison tasks | HTTP 200, backlog < 100, poison count < 5 |

---

## 2. Security Boundary

Health check endpoints expose **zero secrets**, zero connection strings, and zero personal information.
