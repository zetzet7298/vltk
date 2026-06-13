#!/usr/bin/env bash
# FS-02A smoke test — happy path + auth contract verification
# Endpoint: http://127.0.0.1:8020

set -u

BASE_URL="${BASE_URL:-http://127.0.0.1:8020}"
ACC="fs02a_$(date +%s)"
PW="hunter2"
ROLE_NAME="Kiem_Khach_$(date +%s)"

EVIDENCE_DIR="/var/www/vltk-mobile/harness/docs/fs02-evidence-2026-06-13"
mkdir -p "$EVIDENCE_DIR"

sep() { echo "------------------------------------------------------------"; }
step() { echo "[FS-02A] $1"; }

sep
step "1. GET /health — liveness probe"
curl -sS -X GET "$BASE_URL/health" -H "Accept: application/json" > "$EVIDENCE_DIR/01_health.json"
cat "$EVIDENCE_DIR/01_health.json"; echo

sep
step "2. POST /v1/account — create new account"
echo "  accName=$ACC password=$PW"
curl -sS -X POST "$BASE_URL/v1/account" \
  -H "Content-Type: application/json" \
  -d "{\"accName\":\"$ACC\",\"password\":\"$PW\",\"serviceFlag\":0}" \
  > "$EVIDENCE_DIR/02_create_account.json"
cat "$EVIDENCE_DIR/02_create_account.json"; echo

sep
step "3. POST /v1/account/login — login with plaintext password"
echo "  accName=$ACC password=$PW (plaintext, NOT pre-hashed)"
curl -sS -X POST "$BASE_URL/v1/account/login" \
  -H "Content-Type: application/json" \
  -d "{\"accName\":\"$ACC\",\"password\":\"$PW\"}" \
  > "$EVIDENCE_DIR/03_login_success.json"
cat "$EVIDENCE_DIR/03_login_success.json"; echo

sep
step "4. POST /v1/account/login — wrong password (must be 401)"
curl -sS -o "$EVIDENCE_DIR/04_login_wrong_pw.json" -w "HTTP %{http_code}\n" \
  -X POST "$BASE_URL/v1/account/login" \
  -H "Content-Type: application/json" \
  -d "{\"accName\":\"$ACC\",\"password\":\"sai_mat_khau\"}"
cat "$EVIDENCE_DIR/04_login_wrong_pw.json"; echo

sep
step "5. POST /v1/account/login — unknown account (must be 401, no leak)"
curl -sS -o "$EVIDENCE_DIR/05_login_unknown.json" -w "HTTP %{http_code}\n" \
  -X POST "$BASE_URL/v1/account/login" \
  -H "Content-Type: application/json" \
  -d "{\"accName\":\"khong_ton_tai_$(date +%s)\",\"password\":\"x\"}"
cat "$EVIDENCE_DIR/05_login_unknown.json"; echo

sep
step "6. GET /v1/role/by-account/{account} — list roles (empty list expected)"
curl -sS -X GET "$BASE_URL/v1/role/by-account/$ACC" \
  -H "Accept: application/json" \
  > "$EVIDENCE_DIR/06_list_roles_empty.json"
cat "$EVIDENCE_DIR/06_list_roles_empty.json"; echo

sep
step "7. POST /v1/role — create role (faction=0 = Kim)"
curl -sS -X POST "$BASE_URL/v1/role" \
  -H "Content-Type: application/json" \
  -d "{\"account\":\"$ACC\",\"roleName\":\"$ROLE_NAME\",\"faction\":0}" \
  > "$EVIDENCE_DIR/07_create_role.json"
cat "$EVIDENCE_DIR/07_create_role.json"; echo
ROLE_ID=$(/var/www/vltk-mobile/backend/.venv/bin/python -c "import json,sys; d=json.load(open('$EVIDENCE_DIR/07_create_role.json')); print(d.get('data',{}).get('id',''))")
echo "  → roleId=$ROLE_ID"

sep
step "8. GET /v1/role/by-account/{account} — list roles (1 role expected)"
curl -sS -X GET "$BASE_URL/v1/role/by-account/$ACC" \
  -H "Accept: application/json" \
  > "$EVIDENCE_DIR/08_list_roles_one.json"
cat "$EVIDENCE_DIR/08_list_roles_one.json"; echo

sep
step "9. POST /v1/player — create player state for role"
curl -sS -X POST "$BASE_URL/v1/player" \
  -H "Content-Type: application/json" \
  -d "{\"roleId\":$ROLE_ID,\"level\":1,\"series\":0}" \
  > "$EVIDENCE_DIR/09_create_player_state.json"
cat "$EVIDENCE_DIR/09_create_player_state.json"; echo

sep
step "10. GET /v1/player/by-role/{role_id} — get player state"
curl -sS -X GET "$BASE_URL/v1/player/by-role/$ROLE_ID" \
  -H "Accept: application/json" \
  > "$EVIDENCE_DIR/10_get_player_state.json"
cat "$EVIDENCE_DIR/10_get_player_state.json"; echo

sep
step "11. NEGATIVE — login with MD5-uppercase hashed password (must FAIL — backend expects plaintext)"
# This proves that sending MD5-hex as 'password' is the WRONG contract; backend
# would re-hash it (MD5(MD5(password))) and that won't match stored MD5(password).
MD5_OF_PW=$(printf '%s' "$PW" | md5sum | awk '{print toupper($1)}')
echo "  sending 'password' = MD5($PW) = $MD5_OF_PW"
curl -sS -o "$EVIDENCE_DIR/11_login_double_hash_FAILS.json" -w "HTTP %{http_code}\n" \
  -X POST "$BASE_URL/v1/account/login" \
  -H "Content-Type: application/json" \
  -d "{\"accName\":\"$ACC\",\"password\":\"$MD5_OF_PW\"}"
cat "$EVIDENCE_DIR/11_login_double_hash_FAILS.json"; echo
echo "  → contract: client MUST send plaintext, server hashes to MD5-IN-HOA"

sep
step "12. NEGATIVE — login without Authorization header (no bearer/JWT, must still 200)"
curl -sS -o "$EVIDENCE_DIR/12_login_no_header.json" -w "HTTP %{http_code}\n" \
  -X POST "$BASE_URL/v1/account/login" \
  -H "Content-Type: application/json" \
  -d "{\"accName\":\"$ACC\",\"password\":\"$PW\"}"
cat "$EVIDENCE_DIR/12_login_no_header.json"; echo
echo "  → contract: NO Authorization header required for /login; session is acc_name only"

sep
step "13. POST /v1/account/logout — logout, ghi logout_date"
curl -sS -X POST "$BASE_URL/v1/account/logout" \
  -H "Content-Type: application/json" \
  -d "{\"accName\":\"$ACC\"}" \
  > "$EVIDENCE_DIR/13_logout.json"
cat "$EVIDENCE_DIR/13_logout.json"; echo

sep
echo "[FS-02A] smoke test done. account=$ACC roleId=$ROLE_ID"
echo "[FS-02A] evidence: $EVIDENCE_DIR"
ls -1 "$EVIDENCE_DIR"
