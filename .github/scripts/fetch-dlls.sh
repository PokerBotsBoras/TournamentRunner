#!/bin/bash
set -e

echo "Starting fetch-dlls.sh script..."

ORG="PokerBotsBoras"
TOKEN="${GITHUB_TOKEN}"

if [[ -z "$TOKEN" ]]; then
  echo "GITHUB_TOKEN is not set!"
  exit 1
fi

mkdir -p CompiledBots

echo "Listing bot repos in org: $ORG..."
REPOS=$(gh repo list "$ORG" --json name --jq '.[] | select(.name | startswith("bot-")) | .name')

for REPO in $REPOS; do
  echo "Processing $REPO..."

  RELEASE_API="https://api.github.com/repos/$ORG/$REPO/releases/latest"
  ASSETS=$(curl -s -H "Authorization: token $TOKEN" "$RELEASE_API" | jq -r '.assets[] | {name: .name, url: .browser_download_url}')

  if [[ -z "$ASSETS" ]]; then
    echo "  ❌ No release assets found for $REPO"
    continue
  fi

  BOT_DIR="CompiledBots/$REPO"
  mkdir -p "$BOT_DIR"

  echo "$ASSETS" | jq -c '.' | while read -r asset; do
    NAME=$(echo "$asset" | jq -r '.name')
    URL=$(echo "$asset" | jq -r '.url')
    OUT_PATH="$BOT_DIR/$NAME"

    echo "  Downloading $NAME..."
    curl -L -H "Authorization: token $TOKEN" "$URL" -o "$OUT_PATH"
  done

  echo "  ✅ Fetched all assets for $REPO into $BOT_DIR"
done

echo "fetch-dlls.sh script finished."
