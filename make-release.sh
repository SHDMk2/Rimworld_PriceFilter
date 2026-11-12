#!/usr/bin/env bash
# Builds a clean, upload-ready copy of the mod (default: ../PriceFilter next to this folder)
# (no .git, bin, obj or .pdb). Keeps About/PublishedFileId.txt if it already exists there.
set -euo pipefail
SRC="$(cd "$(dirname "$0")" && pwd)"
DST="${1:-$SRC/../PriceFilter}"

[ -d "$HOME/.dotnet" ] && export DOTNET_ROOT="$HOME/.dotnet" PATH="$HOME/.dotnet:$PATH"
dotnet build "$SRC/Source/PriceFilter/PriceFilter.csproj" -c Release -v q -nologo

PFID=""
[ -f "$DST/About/PublishedFileId.txt" ] && PFID="$(cat "$DST/About/PublishedFileId.txt")"
[ -z "$PFID" ] && [ -f "$SRC/About/PublishedFileId.txt" ] && PFID="$(cat "$SRC/About/PublishedFileId.txt")"

rm -rf "$DST"
mkdir -p "$DST/About" "$DST/Assemblies" "$DST/Source/PriceFilter"
cp "$SRC"/About/*.xml "$DST/About/"
[ -f "$SRC/About/Preview.png" ] && cp "$SRC/About/Preview.png" "$DST/About/"
cp "$SRC/Assemblies/PriceFilter.dll" "$DST/Assemblies/"
cp "$SRC"/Source/PriceFilter/*.cs "$SRC/Source/PriceFilter/PriceFilter.csproj" "$DST/Source/PriceFilter/"
[ -n "$PFID" ] && echo -n "$PFID" > "$DST/About/PublishedFileId.txt"

echo "Release ready: $DST"
find "$DST" -type f | sort
