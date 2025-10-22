#!/bin/bash
# Smoke test runner script
# Usage: ./run-smoke-tests.sh
# Note: Configure target in appsettings.json before running

set -e

CONFIG_DIR="$(cd "$(dirname "$0")" && pwd)"
CONFIG_FILE="$CONFIG_DIR/appsettings.json"

# Color output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}=====================================${NC}"
echo -e "${GREEN}  Node-API Smoke Test Runner${NC}"
echo -e "${GREEN}=====================================${NC}"
echo ""

# Check if config exists
if [ ! -f "$CONFIG_FILE" ]; then
    echo -e "${RED}Error: appsettings.json not found${NC}"
    echo "Please create appsettings.json with your target configuration"
    exit 1
fi

echo -e "${YELLOW}Using configuration from appsettings.json${NC}"
echo ""
echo -e "${GREEN}Configuration:${NC}"
cat "$CONFIG_FILE"
echo ""

# Run the tests
echo -e "${GREEN}Running smoke tests...${NC}"
echo ""

if dotnet test --logger "console;verbosity=normal" --nologo; then
    echo ""
    echo -e "${GREEN}=====================================${NC}"
    echo -e "${GREEN}  ? All smoke tests PASSED${NC}"
    echo -e "${GREEN}=====================================${NC}"
    EXIT_CODE=0
else
    echo ""
    echo -e "${RED}=====================================${NC}"
    echo -e "${RED}  ? Some smoke tests FAILED${NC}"
    echo -e "${RED}=====================================${NC}"
    EXIT_CODE=1
fi

exit $EXIT_CODE
