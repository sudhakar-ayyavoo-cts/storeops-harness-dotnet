# Deployment

Deployment target: **Local Docker** Deployed and verified on
Windows 11 with Docker Desktop 29.5.3 (WSL2 backend, Linux containers).

## Target

Local Docker, via the `Dockerfile` and `docker-compose.yml` in this repository.

## Steps taken

1. `docker compose build`
2. `docker compose up -d`
3. Verified the container is healthy: `docker compose ps`
4. Verified the harness-generated feature responds:
   ```
   curl -i http://localhost:5000/api/tasks
   ```
   See Evidence below for the real response.
5. Verified the new SLA-breach-alerting behaviour specifically: triggered the Sprint 1
   sweep endpoint, then the Sprint 2 escalation sweep endpoint, and confirmed via
   `GET /api/alerts` that the resulting notification was created.
   ```
   curl -X POST http://localhost:5000/api/tasks/sla-sweep
   curl "http://localhost:5000/api/alerts?userId=a1000000-0000-0000-0000-000000000003"
   curl -X POST http://localhost:5000/api/alerts/sla-escalation-sweep
   ```
   See Evidence below for the real responses.

Two defects in the pre-existing `Dockerfile`/`docker-compose.yml` scaffold had to be fixed
before step 1 would succeed at all — see **Notes / trade-offs** below, and the full
diagnosis in `DEPLOYMENT_NOTES.md`.

## Evidence

```
$ docker compose build
...
 Image storeops-harness-dotnet-storeops-api Built

$ docker compose up -d
 Network storeops-harness-dotnet_default  Created
 Container storeops-harness-dotnet-storeops-api-1  Started

$ docker compose ps
NAME                                      IMAGE                                   STATUS                    PORTS
storeops-harness-dotnet-storeops-api-1    storeops-harness-dotnet-storeops-api    Up 44 seconds (healthy)   0.0.0.0:5000->8080/tcp
```

```
$ curl -i http://localhost:5000/api/tasks
HTTP/1.1 200 OK
Content-Type: application/json; charset=utf-8
Date: Mon, 24 Aug 2026 14:19:18 GMT
Server: Kestrel
Transfer-Encoding: chunked

[{"id":"5cf2ee01-...","title":"Weekly planogram compliance audit","status":2,"priority":0,...},
 {"id":"d0eb5d86-...","title":"Cold case compressor failure — Aisle 12","status":1,"priority":3,
  "dueDate":"2026-08-22T14:17:41...","storeId":"11111111-1111-1111-1111-111111111111",...},
 ... 3 more demo-seeded tasks]
```

SLA-breach-alerting, exercised end to end against the running container:

```
$ curl -X POST http://localhost:5000/api/tasks/sla-sweep
{"breachesDetected":1}

$ curl "http://localhost:5000/api/alerts?userId=a1000000-0000-0000-0000-000000000003"
[
  {"alertType":1,"message":"CRITICAL task \"Cold case compressor failure — Aisle 12\" is 2 days past its due date.",
   "relatedEntityId":"d0eb5d86-43fc-466d-a7e3-9da95c0ea3ff", ...},
  {"alertType":1,"message":"Task d0eb5d86-43fc-466d-a7e3-9da95c0ea3ff breached its SLA at 2026-08-24T14:18:19Z and needs attention.",
   "relatedEntityId":"d0eb5d86-43fc-466d-a7e3-9da95c0ea3ff", ...}
]

$ curl -X POST http://localhost:5000/api/alerts/sla-escalation-sweep
{"escalationsCreated":0}
```

The second `alerts` entry is the one created by this run's sweep — it's the `SlaBreachEvent`
(Sprint 1) reaching the `alerts` module's subscriber (Sprint 2) and becoming a `Notification`
for the task's Department Lead (`aisha.bello@storeops.demo`, `userId`
`a1000000-0000-0000-0000-000000000003`), exactly as `sprint-2-contract.md`'s AC-1 specifies.
The `escalationsCreated: 0` result is correct, not a failure: the default grace period
(`Alerts:SlaEscalationGraceHours = 4`) hadn't elapsed yet at the moment of this run.

### docker compose ps:

![docker compose ps](/images/docker-compose-ps.png "docker compose ps")

### api/tasks:

![http://localhost:5000/api/tasks](/images/api-tasks.png "api/tasks")

### api/alerts:

![http://localhost:5000/api/alerts?userId=a1000000-0000-0000-0000-000000000003](/images/api-alerts.png "api/alerts")

### api/alerts/sla-escalation-sweep:

![http://localhost:5000/api/alerts/sla-escalation-sweep](/images/api-alerts-sla-escalation-sweep.png "api/alerts/sla-escalation-sweep")

## Notes / trade-offs

- **Environment**: `docker-compose.yml` sets `ASPNETCORE_ENVIRONMENT=Development`, which
  means `DemoDataSeeder` (`src/StoreOps.Infrastructure/Seed/DemoDataSeeder.cs`) runs on
  container startup and seeds the demo dataset (2 stores, 6 staff, 5 tasks including one
  overdue CRITICAL task, 2 programmes, notifications, reports) — this is what step 4/5's
  responses above are showing, not manually-created test data. Storage is in-memory, so this
  reseeds identically on every `docker compose up`.
- **Port**: container port `8080` mapped to host port `5000` (`docker-compose.yml`), matching
  the port this file's `curl` commands already assumed.
- **Two pre-existing scaffold defects fixed before deployment would succeed**:
  1. `docker compose build` failed during `dotnet publish` because no `.dockerignore`
     existed — the build context included every project's local Windows-built `obj/`
     folder, and `dotnet publish --no-restore` picked up a stale `obj/project.assets.json`
     referencing a Windows-only NuGet fallback path
     (`C:\Program Files (x86)\Microsoft Visual Studio\Shared\NuGetPackages`) that doesn't
     exist inside the Linux build stage. Fixed by adding a `.dockerignore` excluding
     `bin/`, `obj/`, `.git/`, and other local/IDE artifacts.
  2. `docker-compose.yml`'s healthcheck runs `curl -f http://localhost:8080/api/tasks`
     inside the container, but the base `mcr.microsoft.com/dotnet/aspnet:8.0` runtime image
     doesn't include `curl`. Fixed by installing it in the `Dockerfile`'s runtime stage.

  Full root-cause diagnosis and the exact fix for each is in `DEPLOYMENT_NOTES.md`, kept as
  a permanent troubleshooting reference alongside this file.
- **Why local Docker over cloud**: this build's scope is a governed-development harness
  demonstration against an in-memory, no-database reference app — local Docker satisfies
  minimum bar without provisioning any cloud infrastructure the app doesn't
  otherwise need. A cloud deployment would additionally need: a real persistence layer (the
  in-memory repositories reset on every restart, which is fine locally but not acceptable
  behind a load balancer with multiple instances), a real password-hashing/JWT
  implementation in `StaffService` (currently a documented `TODO`, plaintext comparison),
  and a real scheduler (`IHostedService`/timer, or an external cron trigger) calling the
  `sla-sweep`/`sla-escalation-sweep` endpoints automatically instead of the manual/test
  invocation both sprint contracts deliberately scoped this build to.
