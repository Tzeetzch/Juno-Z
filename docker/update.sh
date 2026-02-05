#!/bin/bash
# Juno Bank Update Script
# Usage: sudo ./update.sh [--fresh]
#   --fresh: Remove database volume for clean install

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_DIR="$(dirname "$SCRIPT_DIR")"

echo "=== Juno Bank Update ==="

# Pull latest code
echo "Pulling latest code..."
cd "$REPO_DIR"
git pull

# Go to docker directory
cd "$SCRIPT_DIR"

# Check for --fresh flag
if [ "$1" == "--fresh" ]; then
    echo "Removing volumes for fresh install..."
    podman-compose down -v
else
    echo "Stopping containers..."
    podman-compose down
fi

# Rebuild and start
echo "Building new image..."
podman-compose build --no-cache

echo "Starting container..."
podman-compose up -d

# Show status
echo ""
echo "=== Update Complete ==="
podman-compose ps
echo ""
echo "View logs: sudo podman-compose logs -f"
