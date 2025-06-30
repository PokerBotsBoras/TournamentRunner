#!/bin/bash

set -e

echo "Starting fetch-images.sh script..."

ORG="pokerbotsboras"
PREFIX="dockerbot-"
IMAGES=$(gh api orgs/$ORG/packages?package_type=container | jq -r '.[] | select(.name | startswith("'$PREFIX'")) | .name')

if [[ -z "$IMAGES" ]]; then
    echo "No Docker images found for organization $ORG with prefix $PREFIX"
    exit 0
fi

for image in $IMAGES; do
    echo "Processing image: $image"
    
    echo "Pulling ghcr.io/$ORG/$image:latest"
    docker pull ghcr.io/$ORG/$image:latest
done
