#!/bin/bash
set -e

echo "Starting fetch-images.sh script..."

ORG="pokerbotsboras"
PREFIX="dockerbot-"

echo "Calling GitHub API..."
RESPONSE=$(gh api orgs/$ORG/packages?package_type=container || { echo "gh API call failed"; exit 1; })
echo "API response:"
echo "$RESPONSE" | head -n 10  # Just print the start for debugging

IMAGES=$(echo "$RESPONSE" | jq -r '.[] | select(.name | startswith("'"$PREFIX"'")) | .name')

if [[ -z "$IMAGES" ]]; then
    echo "No Docker images found for organization $ORG with prefix $PREFIX"
    exit 0
fi

for image in $IMAGES; do
    echo "Pulling ghcr.io/$ORG/$image:latest"
    docker pull ghcr.io/$ORG/$image:latest
done
