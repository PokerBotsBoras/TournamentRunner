#!/bin/bash
set -e

ORG="PokerBotsBoras"
TOKEN="${GITHUB_TOKEN}"

mkdir -p bots

REPOS=$(gh repo list "$ORG" --json name --jq '.[] | select(.name | startswith("bot-")) | .name')

for REPO in $REPOS; do
  echo "Fetching DLL from $REPO..."
  RELEASE_URL="https://api.github.com/repos/$ORG/$REPO/releases/latest"

  ASSET_URL=$(curl -s -H "Authorization: token $TOKEN" "$RELEASE_URL" \
    | jq -r '.assets[] | select(.name | endswith(".dll")) | .browser_download_url')

  if [[ -n "$ASSET_URL" ]]; then
    curl -L -H "Authorization: token $TOKEN" "$ASSET_URL" -o "CompiledBots/${REPO}.dll"
  else
    echo "No DLL found for $REPO"
  fi
done
