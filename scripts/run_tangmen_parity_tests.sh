#!/usr/bin/env bash
# Bounded TangMen canonical-oracle parity runner.
# 1. Verify the frozen oracle/slices are intact (generator --check).
# 2. Run only the TangMen canonical-oracle EditMode consumer test.
set -euo pipefail
cd "$(dirname "$0")/.."

python3 scripts/generate_tangmen_oracle.py --check

python3 scripts/run_sandbox_tests.py \
  VLTK.Tests.Sandbox.TangMenCanonicalOracleParityTests
