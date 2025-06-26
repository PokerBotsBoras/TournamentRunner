#!/bin/bash
set -e

echo "Starting fetch-dlls.sh script..."

ORG="PokerBotsBoras"
TOKEN="${GITHUB_TOKEN}"

echo "ORG: $ORG"
echo "Checking if GITHUB_TOKEN is set..."
if [[ -z "$TOKEN" ]]; then
  echo "GITHUB_TOKEN is not set!"
  exit 1
else
  echo "GITHUB_TOKEN is set."
fi

mkdir -p bots
echo "Created bots directory."

echo "Listing repos with gh..."
REPOS=$(gh repo list "$ORG" --json name --jq '.[] | select(.name | startswith("bot-")) | .name')
echo "Repos found: $REPOS"

for REPO in $REPOS; do
  echo "Fetching DLL from $REPO..."
  RELEASE_URL="https://api.github.com/repos/$ORG/$REPO/releases/latest"
  echo "Release URL: $RELEASE_URL"

  ASSET_URL=$(curl -s -H "Authorization: token $TOKEN" "$RELEASE_URL" \
    | jq -r '.assets[] | select(.name | endswith(".dll")) | .browser_download_url')
  echo "Asset URL: $ASSET_URL"

  if [[ -n "$ASSET_URL" ]]; then
    OUT_PATH="CompiledBots/${REPO}.dll"
    echo "Downloading to $OUT_PATH"
    curl -L -H "Authorization: token $TOKEN" "$ASSET_URL" -o "$OUT_PATH"
    echo "Saved DLL to: $(realpath "$OUT_PATH")"
  else
    echo "No DLL found for $REPO"
  fi
done

echo "fetch-dlls.sh script finished."