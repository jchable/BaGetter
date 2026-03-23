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
AllowedCorsOrigins__0: "https://packages.dev.coderise.fr"
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
dotnet nuget add source "https://packages.dev.coderise.fr/v3/index.json" \
  --name "internal" \
  --username "<BAGETTER_USER>" \
  --password "<BAGETTER_PASSWORD>"
```

Or in `nuget.config`:

```xml
<configuration>
  <packageSources>
    <add key="internal" value="https://packages.dev.coderise.fr/v3/index.json" />
  </packageSources>
  <packageSourceCredentials>
    <internal>
      <add key="Username" value="<BAGETTER_USER>" />
      <add key="ClearTextPassword" value="<BAGETTER_PASSWORD>" />
    </internal>
  </packageSourceCredentials>
</configuration>
```

## Reverse proxy (Caddy)

[Caddy](https://caddyserver.com) is the recommended option: it handles TLS automatically via Let's Encrypt and requires no manual certificate management.

### Install Caddy

```shell
# Debian / Ubuntu
sudo apt install -y debian-keyring debian-archive-keyring apt-transport-https
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/gpg.key' | sudo gpg --dearmor -o /usr/share/keyrings/caddy-stable-archive-keyring.gpg
curl -1sLf 'https://dl.cloudsmith.io/public/caddy/stable/debian.deb.txt' | sudo tee /etc/apt/sources.list.d/caddy-stable.list
sudo apt update && sudo apt install caddy
```

### Caddyfile

Create `/etc/caddy/Caddyfile`:

```caddyfile
packages.dev.coderise.fr {
    # Block direct access to the health endpoint
    respond /health 404

    # Large package uploads (8 GiB)
    request_body {
        max_size 8GB
    }

    reverse_proxy 127.0.0.1:8082
}
```

Caddy automatically provisions a TLS certificate via Let's Encrypt. Make sure your DNS `A` record for `packages.dev.coderise.fr` points to the VPS public IP, and that ports **80** and **443** are open in the firewall.

Reload Caddy after saving:

```shell
sudo systemctl reload caddy
```

### Configure TrustedProxies

Add the Caddy host IP to your `docker-compose.yml` environment (or `.env`):

```yaml
TrustedProxies__0: "127.0.0.1"
```

If Caddy runs on the same host as Docker, this is `127.0.0.1`. If Caddy runs in a separate container or VM, use its actual IP.

:::tip Caddy with Docker

To run Caddy as a Docker container instead of a system service, add it to `docker-compose.yml` and mount the Caddyfile. The `bagetter` service port (`8082`) can then be changed to an internal-only binding (`127.0.0.1:8082:8080`).

:::

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

The MinIO console port is bound to `127.0.0.1` only — it is not reachable from the internet. Access it via an SSH tunnel from your workstation:

```shell
ssh -L 9002:127.0.0.1:9002 user@your-vps
```

Then open [http://localhost:9002](http://localhost:9002) in your browser. Log in with `MINIO_ROOT_USER` / `MINIO_ROOT_PASSWORD`.

## Database administration

There is no pgAdmin in the production stack. To access PostgreSQL directly:

```shell
# Connect via docker exec
docker exec -it bagetter-postgres-1 psql -U bagetter -d bagetter

# Or via SSH tunnel from your workstation
ssh -L 5432:localhost:5432 user@your-vps
psql "host=localhost port=5432 dbname=bagetter user=bagetter password=<POSTGRES_PASSWORD>"
```

## Backup

:::info

No automated backup is configured in the default stack. If this instance holds packages that cannot be lost, implement a backup strategy before going to production.

:::

### PostgreSQL

```shell
# Dump the database (run from the host)
docker exec bagetter-postgres-1 pg_dump -U bagetter bagetter | gzip > bagetter-$(date +%F).sql.gz

# Restore
gunzip -c bagetter-2026-03-17.sql.gz | docker exec -i bagetter-postgres-1 psql -U bagetter bagetter
```

### MinIO (package files)

Use the MinIO Client (`mc`) to mirror the bucket to another S3-compatible destination or to a local directory:

```shell
# Mirror to a local path
docker run --rm --network host \
  -v /backup/minio:/backup \
  minio/mc:latest \
  mirror local/nuget-packages /backup/nuget-packages
```

Or schedule both commands with `cron` / `systemd` timers and copy the archives off-server (e.g. with `rclone` to S3, B2, or SFTP).

## Updates

```shell
# Pull latest code
git pull

# Rebuild and restart BaGetter only (zero-downtime for DB/MinIO)
docker compose up -d --build bagetter
```

Database migrations run automatically at startup (`RunMigrationsAtStartup: true`).
