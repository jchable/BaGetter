# Production Deployment (Docker Compose)

This guide covers deploying BaGetter on a VPS or dedicated server using Docker Compose, with security hardening applied for a private enterprise feed.

## Prerequisites

- Docker Engine 24+ and Docker Compose v2
- A reverse proxy (Nginx, Traefik, Caddy) for TLS termination
- A domain name or internal DNS entry

## Stack

The production stack includes:

| Service  | Purpose                        |
|----------|--------------------------------|
| BaGetter | NuGet server application       |
| PostgreSQL 17 | Package metadata database |
| MinIO    | S3-compatible package storage  |

## Setup

### 1. Clone the repository

```shell
git clone https://github.com/bagetter/BaGetter.git
cd BaGetter
```

### 2. Create the `.env` file

Copy the example file and fill in your values:

```shell
cp .env.example .env
chmod 600 .env   # Restrict to owner only
```

Edit `.env`:

```shell
# PostgreSQL
POSTGRES_PASSWORD=<strong-password>

# MinIO (S3-compatible storage)
MINIO_ROOT_USER=<minio-user>
MINIO_ROOT_PASSWORD=<strong-password>

# BaGetter application
BAGETTER_API_KEY=<random-64-char-string>
BAGETTER_USER=<admin-username>
BAGETTER_PASSWORD=<strong-password>
```

:::warning

Never commit `.env` to version control. It is already listed in `.gitignore`.
Use a password manager or secrets vault to generate and store strong passwords.

:::

### 3. Configure CORS (optional)

If you access the package browser UI from a browser, set your allowed origins in `docker-compose.yml` or as an environment variable:

```yaml
AllowedCorsOrigins__0: "https://nuget.yourcompany.internal"
```

If left empty, CORS is disabled entirely (NuGet CLI and `dotnet restore` are unaffected — they do not use CORS).

### 4. Configure Trusted Proxies

When running behind a reverse proxy, tell BaGetter which IP address to trust for `X-Forwarded-*` headers:

```yaml
TrustedProxies__0: "172.20.0.1"   # Replace with your reverse proxy container IP
```

If not configured, BaGetter defaults to trusting loopback only (safe).

### 5. Build and start

```shell
docker compose up -d --build
```

### 6. Verify

```shell
# All services should be healthy
docker compose ps

# Health endpoint
curl http://localhost:8082/health
# → {"Status":"Healthy"}

# Auth is required — unauthenticated requests return 401
curl http://localhost:8082/v3/search?q=test
# → 401 Unauthorized
```

## Publish packages

```shell
dotnet nuget push package.1.0.0.nupkg \
  -s http://localhost:8082/v3/index.json \
  -k <BAGETTER_API_KEY>
```

## Add the feed to your project

```shell
dotnet nuget add source "https://nuget.yourcompany.internal/v3/index.json" \
  --name "internal" \
  --username "<BAGETTER_USER>" \
  --password "<BAGETTER_PASSWORD>"
```

Or in `nuget.config`:

```xml
<configuration>
  <packageSources>
    <add key="internal" value="https://nuget.yourcompany.internal/v3/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <internal>
      <add key="Username" value="<BAGETTER_USER>" />
      <add key="ClearTextPassword" value="<BAGETTER_PASSWORD>" />
    </internal>
  </packageSourceCredentials>
</configuration>
```

## Reverse proxy (Nginx example)

```nginx
server {
    listen 443 ssl;
    server_name nuget.yourcompany.internal;

    ssl_certificate     /etc/ssl/certs/yourcompany.crt;
    ssl_certificate_key /etc/ssl/private/yourcompany.key;

    # Forward proxy headers so BaGetter generates correct URLs
    proxy_set_header Host              $host;
    proxy_set_header X-Forwarded-For   $proxy_add_x_forwarded_for;
    proxy_set_header X-Forwarded-Proto $scheme;
    proxy_set_header X-Forwarded-Host  $host;

    # Large package uploads
    client_max_body_size 8g;

    # Block health endpoint from external access
    location /health {
        deny all;
    }

    location / {
        proxy_pass http://127.0.0.1:8082;
    }
}
```

Configure `TrustedProxies__0` in your `.env` with the IP of this Nginx host.

## Security features

The following hardening is applied out of the box:

| Feature | Behavior |
|---|---|
| Authentication | Basic HTTP auth required on all endpoints (read + write) |
| API key | Required for package push (`X-NuGet-ApiKey` header) |
| Rate limiting | Upload: 5 pushes / 10 min per user · Search: 200 req/min |
| Security headers | `X-Frame-Options`, `X-Content-Type-Options`, `CSP`, `Referrer-Policy` |
| CORS | Disabled by default; opt-in via `AllowedCorsOrigins` |
| Trusted proxies | Loopback only by default; configure explicitly for production |
| Statistics page | Disabled (does not expose service fingerprint) |
| Docker healthcheck | Monitors `/health` every 30s |

## Structured logging

Logs are emitted as JSON (Serilog + CompactJsonFormatter) to stdout. Each request produces one line:

```json
{"@t":"2026-03-17T07:00:00Z","@mt":"HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms","RequestMethod":"GET","RequestPath":"/v3/search","StatusCode":200,"Elapsed":4.2,"UserName":"admin","RemoteIP":"172.20.0.3"}
```

Feed these logs into any aggregator via Docker log drivers:

- **Loki**: `docker run ... --log-driver=loki`
- **ELK**: Filebeat reading `/var/lib/docker/containers/*.log`
- **Seq**: `docker run ... --log-driver=gelf`

Health check requests (`/health`) are logged at `Debug` level and suppressed by default.

## MinIO console

The MinIO admin console is accessible at [http://localhost:9002](http://localhost:9002) (internal only — do not expose publicly). Log in with `MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD`.

## Database administration

There is no pgAdmin in the production stack. To access PostgreSQL directly:

```shell
# Connect via docker exec
docker exec -it bagetter-postgres-1 psql -U bagetter -d bagetter

# Or via SSH tunnel from your workstation
ssh -L 5432:localhost:5432 user@your-vps
psql "host=localhost port=5432 dbname=bagetter user=bagetter password=<POSTGRES_PASSWORD>"
```

## Updates

```shell
# Pull latest code
git pull

# Rebuild and restart BaGetter only (zero-downtime for DB/MinIO)
docker compose up -d --build bagetter
```

Database migrations run automatically at startup (`RunMigrationsAtStartup: true`).
