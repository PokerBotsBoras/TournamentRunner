#!/bin/bash

set -e

BOTS_DIR="../Bots"
OUTPUT_DIR="CompiledBots"

mkdir -p "$OUTPUT_DIR"

echo "Building bots in $BOTS_DIR"

for bot in "$BOTS_DIR"/*/; do
    csproj=$(find "$bot" -maxdepth 1 -name "*.csproj" | head -n 1)
    if [ -n "$csproj" ]; then
        echo "Building $bot"
        dotnet build "$bot"
        # Find the DLL in the expected output directory
        dll=$(find "$bot/bin/Debug" -type f -name "*.dll" | head -n 1)
        if [ -n "$dll" ]; then
            cp "$dll" "$OUTPUT_DIR/"
            echo "Copied $(basename "$dll") to $OUTPUT_DIR"
        else
            echo "No DLL found for $bot"
        fi
    fi
done