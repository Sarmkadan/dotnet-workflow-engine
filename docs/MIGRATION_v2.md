# Migration Guide: v1.x to v2.0

This document covers all breaking changes and required steps to upgrade from v1.x to v2.0.

## Breaking Changes

### Docker Port Change (80 -> 8080)

The default container port has changed from `80` to `8080` to support running as a non-root user.

**Before (v1.x):**
```yaml
ports:
  - "5000:80"
```

**After (v2.0):**
```yaml
ports:
  - "8080:8080"
```

If you reference the container port in Kubernetes manifests, reverse proxies, or health check configurations, update all occurrences of port `80` to `8080`.

#### Kubernetes

Update `containerPort`, liveness/readiness probes, and Service `targetPort`:

```yaml
containers:
  - name: api
    ports:
      - containerPort: 8080
    livenessProbe:
      httpGet:
        path: /health
        port: 8080
    readinessProbe:
      httpGet:
        path: /health
        port: 8080
---
apiVersion: v1
kind: Service
spec:
  ports:
    - port: 80
      targetPort: 8080
```

#### Reverse Proxy (nginx)

```nginx
upstream workflow_backend {
    server app1:8080;
    server app2:8080;
}
```

### Non-root Container User

The Docker image now runs as a dedicated `appuser` (UID 1001) instead of root. If you mount volumes that require write access, ensure the directory is owned by UID 1001:

```bash
chown -R 1001:1001 /path/to/mounted/volume
```

### Removed `version` Key from docker-compose.yml

The top-level `version` key has been removed from `docker-compose.yml` per the Compose Specification. Docker Compose v2.x ignores this field; if you use Compose v1, upgrade to v2 before migrating.

### Restart Policy

All services now include `restart: unless-stopped`. This is non-breaking but changes the default behavior from no restart policy to automatic restarts. Remove the directive if you manage restarts externally (e.g., via Kubernetes or systemd).

## Migration Steps

1. **Update port mappings** in all deployment configs (docker-compose, Kubernetes, CI/CD, reverse proxy).
2. **Update health check URLs** - replace `http://localhost/health` or `http://localhost:80/health` with `http://localhost:8080/health`.
3. **Update `ASPNETCORE_URLS`** if set explicitly in environment variables: change to `http://+:8080`.
4. **Fix volume permissions** if mounting writable directories into the container.
5. **Remove `version` key** from custom docker-compose overrides if present.
6. **Update NuGet package** to `Zaiets.dotnet.workflow.engine` v2.0.0.
7. **Test health checks** - run `docker compose up` and verify `curl http://localhost:8080/health` returns 200.

## Environment Variable Changes

| Variable | v1.x Default | v2.0 Default |
|----------|-------------|-------------|
| `ASPNETCORE_URLS` | `http://+:80` | `http://+:8080` |

No other environment variables have changed names or semantics.

## Rollback

To roll back to v1.x, revert the Docker image tag and restore the previous port mappings. No database schema changes were introduced in v2.0, so data is fully compatible in both directions.
