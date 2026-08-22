# Deployment

> Template — fill in after you build and run the container. Deployment target chosen:
> **Local Docker** (minimum accepted option per Section 3.4 of the brief).

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
   [Paste the real response once you've run it.]
5. Verified the new SLA-breach-alerting behaviour specifically — e.g. trigger the sweep
   endpoint/mechanism your Generator built, then check `.harness` demonstration evidence
   or a follow-up `GET /api/alerts` call shows the resulting notification.
   [Describe the exact call(s) and response(s).]

## Evidence

[Attach a screenshot of `docker compose ps` showing the running container, and a
screenshot or pasted output of the successful `curl`/Postman call against the new
endpoint, per Section 5.5 / 6.3 of the brief.]

## Notes / trade-offs

[Anything worth recording — e.g. environment variables used, why local Docker was chosen
over cloud deployment for this submission, what would change for a cloud deployment.]
