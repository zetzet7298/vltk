#!/usr/bin/env bash
set -euo pipefail

SRC="${1:-../Assets/StreamingAssets/Generated/SkillPort}"
DST="${2:-catalog/testdata/skillport}"
mkdir -p "${DST}"
find "${SRC}" -maxdepth 1 -type f ! -name '*.meta' -print0 | while IFS= read -r -d '' file; do
  cp "${file}" "${DST}/$(basename "${file}")"
done
