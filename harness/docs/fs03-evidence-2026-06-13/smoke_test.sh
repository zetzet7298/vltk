#!/usr/bin/env bash
# FS-03A smoke test — exercise the 8 endpoints we need to pin (skill + combat).
# All against a live uvicorn at 127.0.0.1:8020.
# Saves step-by-step response JSON into $OUT_DIR for diff/replay later.

set -euo pipefail

BASE_URL="${BASE_URL:-http://127.0.0.1:8020}"
OUT_DIR="${OUT_DIR:-/var/www/vltk-mobile/harness/docs/fs03-evidence-2026-06-13}"
mkdir -p "$OUT_DIR"

ACCNAME="fs03a_$(date +%s)"

# Helper: curl + save body to $OUT_DIR/$name.json + assert status.
# Usage: call_api POST /v1/... data.json step_name
call_api() {
  local method="$1"
  local path="$2"
  local data="${3:-}"
  local step="$4"
  local out="$OUT_DIR/${step}.json"
  local code
  if [[ -n "$data" ]]; then
    code=$(curl -sS -o "$out" -w '%{http_code}' \
      -X "$method" "$BASE_URL$path" \
      -H 'Content-Type: application/json' \
      -d "$data")
  else
    code=$(curl -sS -o "$out" -w '%{http_code}' \
      -X "$method" "$BASE_URL$path")
  fi
  printf '  %s %-50s -> %s  (%s)\n' "$method" "$path" "$code" "$step"
  if [[ ! "$code" =~ ^2 ]]; then
    echo "    FAIL body: $(head -c 300 "$out")"
    return 1
  fi
  return 0
}

echo "FS-03A smoke test — base=$BASE_URL acc=$ACCNAME"

# 1. /v1/account (create)
call_api POST /v1/account \
  "{\"accName\":\"$ACCNAME\",\"password\":\"hunter2\"}" \
  01_account_create

# 2. /v1/account/login
call_api POST /v1/account/login \
  "{\"accName\":\"$ACCNAME\",\"password\":\"hunter2\"}" \
  02_account_login

# 3. /v1/role (create role)
call_api POST /v1/role \
  "{\"account\":\"$ACCNAME\",\"roleName\":\"FS03A\",\"faction\":0}" \
  03_role_create
ROLE_ID=$(python3 -c "import json;print(json.load(open('$OUT_DIR/03_role_create.json'))['data']['id'])")
echo "  -> role_id=$ROLE_ID"

# 4. /v1/player (create player_state)
call_api POST /v1/player \
  "{\"roleId\":$ROLE_ID,\"level\":50}" \
  04_player_create

# Seed mana=300 directly (test parity against skill 210 which costs 50 mana).
# Use sqlalchemy via Python one-liner.
python3 - <<PY
import asyncio
from sqlalchemy.ext.asyncio import create_async_engine
from sqlalchemy import text
async def go():
    e = create_async_engine('postgresql+asyncpg://vltk:vltk_dev_pwd@127.0.0.1:5432/vltk_game')
    async with e.begin() as c:
        await c.execute(text('UPDATE player_states SET current_mana=300, mana_max=500, current_life=1000, life_max=1000 WHERE role_id=:r'), {'r': $ROLE_ID})
    print('  -> seeded mana=300 for role_id=$ROLE_ID')
asyncio.run(go())
PY

# 5. /v1/skill/learn
call_api POST /v1/skill/learn \
  "{\"roleId\":$ROLE_ID,\"skillId\":22,\"charLevel\":50,\"faction\":0}" \
  05_skill_learn

# 6. /v1/skill/by-role/{role_id}
call_api GET "/v1/skill/by-role/$ROLE_ID" "" 06_skill_by_role

# 7. /v1/skill/by-role/{role_id}/level-up/{skill_id}
call_api POST "/v1/skill/by-role/$ROLE_ID/level-up/22" "" 07_skill_level_up

# 8. /v1/skill/learn for skill 210 (mana cost 50) — needed for cast flow
call_api POST /v1/skill/learn \
  "{\"roleId\":$ROLE_ID,\"skillId\":210,\"charLevel\":50,\"faction\":0}" \
  08_skill_learn_210

# 9. /v1/skill/cast/check (pre-flight, stateless)
call_api POST /v1/skill/cast/check \
  "{\"roleId\":$ROLE_ID,\"skillId\":210,\"currentMana\":300,\"currentLife\":1000,\"currentStamina\":100,\"onHorse\":false,\"relation\":2,\"distance\":0,\"weaponType\":0,\"equipState\":-2,\"nowMs\":1000,\"lastCastMs\":0}" \
  09_skill_cast_check

# 10. /v1/skill/cast (server-authoritative, real deduction)
call_api POST /v1/skill/cast \
  "{\"roleId\":$ROLE_ID,\"skillId\":210,\"onHorse\":false,\"relation\":2,\"distance\":0,\"weaponType\":0,\"equipState\":-2,\"nowMs\":1000}" \
  10_skill_cast

# 11. /v1/skill/cast (cooldown reject — same nowMs=1001, last_cast=1000 → diff=1 < wait=5)
call_api POST /v1/skill/cast \
  "{\"roleId\":$ROLE_ID,\"skillId\":210,\"onHorse\":false,\"relation\":2,\"distance\":0,\"weaponType\":0,\"equipState\":-2,\"nowMs\":1001}" \
  11_skill_cast_cooldown || true
echo "  (above is EXPECTED to fail with 409 — cooldown gate)"

# 12. /v1/combat/damage/calc (parity armor+resist)
call_api POST /v1/combat/damage/calc \
  '{"atkMin":200,"atkMax":200,"damageKind":0,"target":{"life":1000,"lifeMax":1000,"physicsArmor":50,"physicsResist":20},"seed":1}' \
  12_combat_damage_calc

# 13. /v1/combat/damage/calc (mana shield break)
call_api POST /v1/combat/damage/calc \
  '{"atkMin":100,"atkMax":100,"damageKind":0,"target":{"life":1000,"lifeMax":1000,"mana":30,"manaMax":200,"manaShieldPercent":50},"seed":1}' \
  13_combat_damage_mana_shield

# 14. /v1/combat/status/tick (poison DoT applies)
call_api POST /v1/combat/status/tick \
  '{"target":{"life":1000,"lifeMax":1000},"status":{"poisonState":{"value0":15,"value1":2,"time":3}},"loopFrames":1}' \
  14_combat_status_tick_poison

# 15. /v1/combat/status/tick (regen on GAME_UPDATE_TIME)
call_api POST /v1/combat/status/tick \
  '{"target":{"life":100,"lifeMax":1000,"mana":50,"manaMax":500},"loopFrames":0,"lifeReplenish":30,"manaReplenish":20}' \
  15_combat_status_tick_regen

# 16. /v1/combat/pk/check (safe zone blocks)
call_api POST /v1/combat/pk/check \
  '{"attackerCamp":1,"targetCamp":2,"mapType":"City","inBattle":true}' \
  16_combat_pk_check_safe_zone

# 17. /v1/combat/pk/check (battlefield enemy allowed)
call_api POST /v1/combat/pk/check \
  '{"attackerCamp":1,"targetCamp":2,"mapType":"Battlefield","inBattle":true}' \
  17_combat_pk_check_battlefield

echo
echo "All smoke steps complete. Output: $OUT_DIR"
ls -1 "$OUT_DIR"/[0-9]*.json
