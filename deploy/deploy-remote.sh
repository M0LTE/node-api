#!/bin/bash
# Bash script to trigger remote deployment
# Usage: ./deploy/deploy-remote.sh [host] [user] [script_path]

HOST="${1:-node-api.packet.oarc.uk}"
USER="${2:-debian}"
SCRIPT_PATH="${3:-/opt/node-api/update-service.sh}"

echo "=== Remote Deployment to node-api ==="
echo "Target: $USER@$HOST"
echo ""

# Execute the update script via SSH
echo "Triggering remote update..."
ssh "$USER@$HOST" "bash $SCRIPT_PATH"

if [ $? -eq 0 ]; then
    echo ""
    echo "? Deployment successful!"
else
    echo ""
    echo "? Deployment failed with exit code $?"
    exit 1
fi
