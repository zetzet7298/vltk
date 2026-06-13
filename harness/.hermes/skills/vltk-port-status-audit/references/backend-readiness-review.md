# Backend readiness review for Unity integration

Use this reference when the user asks whether `/var/www/vltk-mobile/backend` is ready to connect to the Unity client or whether the project can start replacing local/mock services with the backend.

## What to separate

Always report two readiness levels separately:

1. **REST game backend / domain authority** — account, role, player state, item, skill, map, shop, task, mission, social, activity, dialog, combat helper endpoints.
2. **Realtime MMO server** — connection/session manager, server tick loop, movement input, AOI/interest management, entity snapshots, NPC/enemy ownership, realtime broadcast/reconciliation.

A backend can be strong for (1) while still weak for (2). Do not call it a complete MMO server just because REST domain tests pass.

## Fast read-only inventory

From `/var/www/vltk-mobile/backend`:

```bash
srcwalk discover '*.py' --as file --scope app/modules --budget 20000
tree -a -L 3 app/modules -I '__pycache__'
python3 - <<'PY'
from pathlib import Path
for p in sorted(Path('app/modules').glob('*/api/v1/router.py')):
    print('---', p)
    txt = p.read_text()
    import re
    for m in re.finditer(r'@router\.(get|post|put|patch|delete)\("([^"]*)"', txt):
        print(m.group(1).upper(), f"/v1/{p.parts[2]}" + m.group(2))
PY
```

## Test proof to collect

Use the repo venv if present:

```bash
.venv/bin/python -m pytest tests/unit -q
.venv/bin/python -m pytest tests/integration -q
.venv/bin/python -m pytest tests/e2e -q
```

In the 2026-06-13 review, evidence was:

```text
722 tests collected
618 unit passed
103 integration passed
1 e2e passed
```

Treat those as historical evidence only; rerun before making fresh claims.

## Smoke server health

Start uvicorn as a tracked background process, not a foreground long-lived command:

```bash
.venv/bin/uvicorn app.main:app --host 127.0.0.1 --port 8020
curl -sS http://127.0.0.1:8020/health
```

Expected shape:

```json
{"status":"ok","service":"vltk-game-server","version":"0.1.0", "timestamp":"..."}
```

## Phase decision criteria

Ready to start **Phase 1 Unity integration** if:

- health endpoint works;
- unit/integration/e2e tests pass or failures are understood and unrelated;
- REST endpoints exist for login/account, role, player state, map enter/position, skill list/cast, item inventory/equip/use;
- Unity can keep a local/mock backend path while adding a `RestBackend` path.

Not ready to claim **realtime MMO server** unless code includes and tests cover:

- persistent authenticated sessions/tokens for role-bound requests;
- websocket/UDP/TCP transport or equivalent realtime gateway;
- server-owned world/entity model with NPC/enemy/player instances;
- server tick loop;
- movement command intake and authoritative position updates;
- AOI/interest management and snapshot broadcast;
- combat results broadcast to nearby clients.

## Common finding from this review

`/var/www/vltk-mobile/backend` is suitable to connect as a REST authority for account/role/player/item/skill/map persistence and to start replacing Unity mock services. It should be described as **REST game backend ready for Phase 1**, not as a complete realtime MMO server. Realtime should be a later module such as `app/modules/realtime/` with connection manager, world tick, interest/AOI, snapshots, and input commands.

Also watch for text encoding: backend map names may come out as TCVN3 mojibake if returned directly to UI. For Phase 1, either normalize/decode server responses or let Unity keep its verified Vietnamese catalog and treat backend map names as non-display data until fixed.
