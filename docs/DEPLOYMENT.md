# Deployment Guide

Complete guide for deploying Juno Bank on a Linux server with Podman/Docker.

## Prerequisites

- Linux server (amd64)
- Podman with podman-compose (or Docker with docker-compose)
- Nginx as reverse proxy
- Domain with Cloudflare DNS (optional but recommended)

## Quick Start

```bash
# Clone the repo
git clone https://github.com/Tzeetzch/Juno-Z.git
cd Juno-Z

# Build and start with Podman
cd docker
podman-compose up -d --build

# Or with Docker
docker-compose up -d --build

# Check logs
podman-compose logs -f
# or
docker-compose logs -f
```

The app runs on `http://localhost:5050`.

## Directory Structure

```
docker/
├── Dockerfile           # Multi-stage build
├── docker-compose.yml   # Container orchestration
├── .dockerignore        # Build exclusions
├── nginx-example.conf   # Reverse proxy config
└── update.sh            # Update script
```

## Reverse Proxy Setup (Nginx)

### 1. Copy the example config

```bash
sudo cp docker/nginx-example.conf /etc/nginx/sites-available/junobank
sudo ln -s /etc/nginx/sites-available/junobank /etc/nginx/sites-enabled/
```

### 2. Edit the config

```bash
sudo nano /etc/nginx/sites-available/junobank
```

Replace `juno.yourdomain.com` with your actual subdomain.

### 3. SSL Options

**Option A: Cloudflare Full (Strict) SSL** (recommended)
1. In Cloudflare Dashboard → SSL/TLS → Origin Server
2. Create Origin Certificate
3. Save the certificate and key to your server
4. Uncomment and update the ssl_certificate lines in nginx config

**Option B: Let's Encrypt**
```bash
sudo certbot --nginx -d juno.yourdomain.com
```

### 4. Test and reload Nginx

```bash
sudo nginx -t
sudo systemctl reload nginx
```

## Cloudflare DNS Setup

1. Add an A record pointing to your server IP:
   - Type: A
   - Name: juno (or your preferred subdomain)
   - Content: Your server's public IP
   - Proxy status: Proxied (orange cloud)

2. SSL/TLS settings:
   - Encryption mode: Full (strict) if using origin certificate
   - Encryption mode: Full if using Let's Encrypt

## Data Persistence

Two volumes are created for persistent data:

| Volume | Purpose |
|--------|---------|
| `junobank-data` | SQLite database (`junobank.db`) + `email-config.json` |
| `junobank-keys` | ASP.NET Data Protection keys (auth cookies) |

### Backup

```bash
# Find the volume location
podman volume inspect junobank-data

# Copy the database file
sudo cp /path/to/volume/junobank.db ~/backups/junobank-$(date +%Y%m%d).db
```

### Restore

```bash
# Stop the container
podman-compose down

# Copy backup to volume
sudo cp ~/backups/junobank-backup.db /path/to/volume/junobank.db

# Start the container
podman-compose up -d
```

## Environment Variables

Override settings via environment variables in `docker-compose.yml`:

| Variable | Default | Description |
|----------|---------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | Runtime environment |
| `ASPNETCORE_URLS` | `http://+:5050` | Listen URL |
| `ConnectionStrings__DefaultConnection` | `Data Source=/app/data/junobank.db` | Database path |
| `JUNO_SEED_DEMO` | *(unset)* | Set to `true` to seed demo accounts |

### Email Configuration

Email enables password reset links. Configure via the **Setup Wizard** (Step 4) or environment variables:

```yaml
environment:
  - Email__Host=smtp.gmail.com
  - Email__Port=587
  - Email__Username=your-email@gmail.com
  - Email__Password=your-app-password
  - Email__FromAddress=noreply@yourdomain.com
  - Email__FromName=Juno Bank
```

**Priority:** Environment variables override `email-config.json` (written by Setup Wizard).

**Gmail setup:** Enable 2-Step Verification → create App Password (Google Account → Security → App Passwords → Mail). Use the 16-character app password, not your Gmail password.

## First Run

1. Access the app via your domain: `https://juno.yourdomain.com`
2. The **Setup Wizard** will launch automatically (no users exist yet)
3. Create your admin parent account (Step 1)
4. Optionally add a partner/second parent (Step 2)
5. Add your children with picture passwords (Step 3)
6. Configure email for password recovery, or skip (Step 4)
7. Review and confirm (Step 5)
8. You'll be logged in automatically as the admin parent

### Demo Mode (for testing)

Set `JUNO_SEED_DEMO=true` to seed demo accounts instead of using the wizard:
- **Parent 1:** dad@junobank.local / parent123 (admin)
- **Parent 2:** mom@junobank.local / parent123
- **Child (Junior):** Tap cat → dog → star → moon
- **Child (Sophie):** Tap star → moon → cat → dog

## Updates

```bash
cd Juno-Z
git pull

cd docker
podman-compose down
podman-compose up -d --build
```

## Troubleshooting

### Container won't start

```bash
# Check logs
podman-compose logs junobank

# Common issues:
# - Port 5050 already in use: change the port mapping in docker-compose.yml
# - Permission denied on volumes: check volume ownership
```

### Blazor SignalR connection fails

Ensure Nginx has WebSocket support configured:
- `proxy_set_header Upgrade $http_upgrade;`
- `proxy_set_header Connection "upgrade";`

Check Cloudflare WebSockets setting (Network → WebSockets → On)

### Database locked errors

SQLite allows only one writer at a time. If you see "database is locked":
1. Ensure only one container instance is running
2. Restart the container: `podman-compose restart`

### Password Recovery

Three options for resetting a forgotten password:

1. **Email reset** — If email is configured, use the "Forgot Password?" link on the login page. A reset link is sent to the parent's email.

2. **Admin resets another parent** — An admin parent can reset another parent's password from Settings → Admin Panel → "Reset Password" button.

3. **CLI emergency reset** — If all else fails, use Docker exec:
```bash
docker exec junobank dotnet JunoBank.Web.dll reset-password user@email.com newpassword
# or with Podman:
podman exec junobank dotnet JunoBank.Web.dll reset-password user@email.com newpassword
```
This resets the password and clears any lockout state.

### Auth cookies don't persist after restart

The Data Protection keys volume must persist. Check:
```bash
podman volume inspect junobank-keys
```

## Security Notes

- The container runs as a non-root user (`junobank`)
- Production starts with Setup Wizard (no default passwords)
- Database file permissions are restricted
- HTTPS is handled by the reverse proxy, not the container
- Login rate limiting: 5 attempts → 5-minute lockout

## Health Check

The container includes a health check. View status:

```bash
podman ps
# or
docker ps
```

Healthy containers show `(healthy)` in the STATUS column.

## Resource Usage

Juno Bank is lightweight:
- **Memory:** ~100-150 MB
- **CPU:** Minimal (spikes during requests)
- **Disk:** ~200 MB image + database size
