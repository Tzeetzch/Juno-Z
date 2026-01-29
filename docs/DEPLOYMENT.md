# Deployment Guide

## Prerequisites

- Linux server with Docker and Docker Compose
- Reverse proxy (Traefik, Nginx Proxy Manager, Caddy, etc.)

## Quick Start

```bash
# Clone the repo
git clone https://github.com/Tzeetzch/Juno-Z.git
cd Juno-Z

# Start the container
cd docker
docker-compose up -d

# Check logs
docker-compose logs -f
```

The app runs on `http://localhost:8080` by default.

## Reverse Proxy Setup

Point your reverse proxy to `junobank:8080` (or `localhost:8080` if not using Docker networks).

### Traefik Example (docker-compose labels)

```yaml
services:
  junobank:
    # ... existing config ...
    labels:
      - "traefik.enable=true"
      - "traefik.http.routers.junobank.rule=Host(`piggybank.yourdomain.com`)"
      - "traefik.http.routers.junobank.tls.certresolver=letsencrypt"
      - "traefik.http.services.junobank.loadbalancer.server.port=8080"
```

### Nginx Proxy Manager

1. Add Proxy Host
2. Domain: `piggybank.yourdomain.com`
3. Forward to: `junobank` (container name) or server IP
4. Port: `8080`
5. Enable SSL with Let's Encrypt

## Data Persistence

SQLite database is stored in a Docker volume:

```yaml
volumes:
  - junobank-data:/app/data
```

### Backup

```bash
# Copy database from volume
docker cp junobank:/app/data/junobank.db ./backup/

# Or if using bind mount
cp /path/to/data/junobank.db ./backup/
```

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Runtime environment |
| `ConnectionStrings__DefaultConnection` | (in appsettings) | SQLite path |
| `EmailSettings__Enabled` | `false` | Enable email notifications |
| `EmailSettings__SmtpHost` | - | SMTP server |
| `EmailSettings__SmtpPort` | `587` | SMTP port |

## First Run

1. Access the app via your domain
2. Complete the setup wizard (create parent accounts, child account)
3. Configure weekly allowance

## Updates

```bash
cd Juno-Z
git pull
cd docker
docker-compose build
docker-compose up -d
```
