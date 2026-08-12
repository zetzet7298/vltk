#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
python3 scripts/generate_caibang_oracle.py --check
python3 scripts/run_sandbox_tests.py \
  VLTK.Tests.Sandbox.CaiBangAddSkillDamageChainTests \
  VLTK.Tests.Sandbox.CaiBangCanonicalOracleParityTests \
  VLTK.Tests.Sandbox.CaiBangCastSoundParityTests \
  VLTK.Tests.Sandbox.CaiBangCatalogCharAnimTests \
  VLTK.Tests.Sandbox.CaiBangCombatParityTests \
  VLTK.Tests.Sandbox.CaiBangFirePoolParityTests \
  VLTK.Tests.Sandbox.CaiBangDogArrayTests \
  VLTK.Tests.Sandbox.CaiBangLuaLevelServiceTests \
  VLTK.Tests.Sandbox.CaiBangPhiLongCollisionAcceptanceTests \
  VLTK.Tests.Sandbox.CaiBangPhiLongSpreadTests \
  VLTK.Tests.Sandbox.CaiBangSkillPanelTests \
  VLTK.Tests.Sandbox.CaiBangSkillStyleTests \
  VLTK.Tests.Sandbox.CaiBangTianXiaWuGouTests \
  VLTK.Tests.Sandbox.CaiBangVisualResourceParityTests \
  VLTK.Tests.Sandbox.CaiBangWaitTimeTests \
  VLTK.Tests.Sandbox.CombatSkillSlotTests
