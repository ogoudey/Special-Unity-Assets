#!/usr/bin/env bash
set -euo pipefail

# Usage: ./symlink_all.sh SOURCE_DIR TARGET_DIR
# Creates hard links for files in SOURCE_DIR into TARGET_DIR.

# Recommended usage:
# ./symlink_all.sh Server /path/to/Unity/Project/Assets/Scripts
# ./symlink_all.sh Client /path/to/Unity/Project/Assets/Scripts

if [[ $# -ne 2 ]]; then
    echo "Usage: $0 <source_dir> <target_dir>"
    exit 1
fi

SRC="$(realpath "$1")"
DST="$(realpath "$2")"

# Validate paths
if [[ ! -d "$SRC" ]]; then
    echo "Error: Source directory does not exist: $SRC"
    exit 1
fi

if [[ ! -d "$DST" ]]; then
    echo "Error: Target directory does not exist: $DST"
    exit 1
fi

echo "Linking files from:"
echo "  SRC = $SRC"
echo "  DST = $DST"
echo

# Loop over all entries in SRC
for file in "$SRC"/*; do
    # Skip if not a regular file
    if [[ ! -f "$file" ]]; then
        continue
    fi

    filename="$(basename "$file")"
    target="$DST/$filename"

    # Skip if the file already exists in target
    if [[ -e "$target" ]]; then
        echo "Skipping existing: $filename"
        continue
    fi

    echo "Hard linking: $filename"
    ln "$file" "$target"
done

echo
echo "Done!"

