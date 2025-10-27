#!/bin/bash
# Script to update the node-api service on the Debian host
# This is meant to be run on the remote server
# The systemd service handles pulling the latest Docker image and running compose up

set -e

echo "=== node-api Service Update ==="
echo "Starting at: $(date)"

# Restart the service via systemd (which handles pull + compose up)
echo "Restarting service (this will pull latest image and restart)..."
sudo systemctl restart node-api

# Wait a moment for the service to start
sleep 3

# Check service status
echo "Service status:"
sudo systemctl status node-api --no-pager

echo "Update completed at: $(date)"
