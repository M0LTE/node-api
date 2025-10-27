#!/bin/bash
# Script to add GitHub Actions IP ranges to UFW firewall
# Run this on your Debian server
# This fetches the latest IP ranges from GitHub's API

set -e

echo "Fetching current GitHub Actions IP ranges..."

# Fetch GitHub's IP ranges from their meta API
TEMP_FILE=$(mktemp)
curl -s https://api.github.com/meta > "$TEMP_FILE"

# Extract the 'actions' IP ranges
echo "Extracting GitHub Actions IP ranges..."
GITHUB_IPS=$(cat "$TEMP_FILE" | grep -A 100 '"actions":' | grep -oP '\d+\.\d+\.\d+\.\d+/\d+' | sort -u)

if [ -z "$GITHUB_IPS" ]; then
    echo "ERROR: Could not fetch GitHub Actions IP ranges"
    echo "You may need to manually add them from: https://api.github.com/meta"
    rm "$TEMP_FILE"
    exit 1
fi

echo "Found $(echo "$GITHUB_IPS" | wc -l) IP ranges"
echo ""

# Show user what will be added
echo "The following IP ranges will be added to UFW:"
echo "$GITHUB_IPS" | head -20
if [ $(echo "$GITHUB_IPS" | wc -l) -gt 20 ]; then
    echo "... and $(( $(echo "$GITHUB_IPS" | wc -l) - 20 )) more"
fi
echo ""

# Check if we're being run non-interactively or with -y flag
AUTO_CONFIRM=false
if [ "$1" = "-y" ] || [ "$1" = "--yes" ]; then
    AUTO_CONFIRM=true
fi

if [ "$AUTO_CONFIRM" = false ]; then
    read -rp "Continue? (y/n): " REPLY
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        echo "Aborted"
        rm "$TEMP_FILE"
        exit 1
    fi
else
    echo "Auto-confirming (non-interactive mode)"
fi

# Add rules to UFW
echo ""
echo "Adding UFW rules..."
COUNT=0
while IFS= read -r ip; do
    echo "Adding rule for $ip..."
    if sudo ufw allow from "$ip" to any port 22 proto tcp comment 'GitHub Actions' 2>&1 | grep -q "Skipping"; then
        echo "  (already exists)"
    fi
    COUNT=$((COUNT + 1))
done <<< "$GITHUB_IPS"

rm "$TEMP_FILE"

echo ""
echo "Processed $COUNT IP ranges"
echo "Reloading UFW..."
sudo ufw reload

echo ""
echo "Done! Current UFW status:"
sudo ufw status numbered | head -20
echo ""
echo "Note: Run this script periodically as GitHub may add new IP ranges"
echo "Tip: Use '$0 -y' to skip the confirmation prompt"
