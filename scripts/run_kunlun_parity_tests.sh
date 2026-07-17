#!/usr/bin/env bash
# SKL-KL-PROOF-001: focused KunLun parity verification.
#
# 1. Verify the frozen PcKunLun oracle is byte-exact (re-derived from canonical sources).
# 2. Run the focused, non-circular Unity EditMode proof set:
#       - KunLunCanonicalOracleParityTests   (hash-pin + 24 learned + 17 targets + progression)
#       - KunLunSkillPanelTests              (canonical learned state + 18 display rows)
#       - AllFactionsCombatParityTests       (KunLun faction/count guards)
#       - CombatSkillSlotTests               (shared slot/cast regression coverage; not UI-order proof)
#       - SkillLevelUpgradeServiceTests      (upgrade eligibility)
#       - SectAllQuickWinsTests              (shared static-regression compatibility)
#
# The generator check is the independent authority gate; Unity tests are the production parity
# gate. If Unity/the MCP test runner is unavailable, the generator check still runs and the test
# step reports the gap instead of weakening assertions.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$REPO_ROOT"

echo "=== [1/2] KunLun oracle generator --check ==="
python3 scripts/generate_kunlun_oracle.py --check

echo
echo "=== [2/2] Focused KunLun parity EditMode tests ==="
TESTS=(
    "VLTK.Tests.Sandbox.KunLunCanonicalOracleParityTests"
    "VLTK.Tests.Sandbox.KunLunSkillPanelTests"
    "VLTK.Tests.Sandbox.AllFactionsCombatParityTests"
    "VLTK.Tests.Sandbox.CombatSkillSlotTests"
    "VLTK.Tests.Sandbox.SkillLevelUpgradeServiceTests"
    "VLTK.Tests.Sandbox.SectAllQuickWinsTests"
)

if ! python3 scripts/run_sandbox_tests.py "${TESTS[@]}"; then
    echo "ERROR: KunLun parity Unity tests failed or the MCP test runner was unavailable." >&2
    exit 1
fi

echo
echo "=== KunLun parity verification complete ==="
