# Validation

## Proof Strategy

Completion cần chứng minh hai chiều: mọi source unit server-relevant được disposition
và mọi requirement/spec/gap truy ngược được về source unit exact. Proof phải chạy
lại trên bytes hiện hành, kiểm tra revision drift và không chấp nhận placeholder
không có trạng thái rõ ràng.

## Test Plan

| Layer | Cases |
| --- | --- |
| Unit | Parser/inventory/validator xác định path, digest, classification, stable ID và schema records. |
| Integration | Cross-reference CNPM/domain/gap/coverage; OpenAPI/router và SQLAlchemy schema inventory read-only. |
| E2E | Một số parity journeys đại diện từ PC evidence → spec → backend state → executable proof/gap. |
| Platform | Unity `IGameBackend`/DTO/proto mapping và server-authority seams. |
| Performance | Inventory/validator hoàn tất trên corpus hiện hành với kết quả deterministic. |
| Logs/Audit | Báo orphan, hash drift, variant chưa rõ, status/priority/proof thiếu. |

Không chạy `tests/integration/` hoặc `tests/e2e/` backend trước khi chứng minh PostgreSQL
test là disposable/isolated.

## Fixtures

- Snapshot manifest của PC Core/server scripts/config/data tại commit + SHA-256 worktree.
- Snapshot manifest mobile contracts và backend code/tests.
- Stable requirement/domain/status/priority vocabularies.

## Commands

Gate hiện hành gồm:

```text
srcwalk review --scope specs
python specs/scripts/validate.py --strict
ruff check specs/scripts
black --check specs/scripts
```

## Acceptance Evidence

- Herdr `orch-46fc75190d57cfa7`: ba lane read-only đã collect sạch, không boundary
  violation; transcript/boundary evidence nằm trong run state.
- Herdr re-audit `orch-5c58d1c2131bc464`: proof-auditor/reviewer lanes `wG:p5N` và
  `wG:p5P` collect sạch; ba coverage override skill/combat được hạ từ
  `runtime-wired` xuống `stub/TODO`, và P2/reverse-link/coverage-state gaps đã
  được tích hợp.
- Root đã đối chứng target OpenAPI, current FastAPI router/startup/database và
  Unity `IGameBackend`/DTO/`useMock`; các mâu thuẫn được ghi GAP-API-002,
  GAP-AUT-003, GAP-DAT-003..005, GAP-RUN-002.
- Structural strict baseline: `inventory=106183`, `coverage=104655`; đây không
  phải proof runtime parity.
- Focused unit proof: `pytest tests/unit/modules/account tests/unit/modules/skill
  tests/unit/modules/combat tests/unit/modules/map` → `1177 passed`.
- Fresh root gate sau re-audit `orch-5c58d1c2131bc464` (2026-07-20):
  `srcwalk review --scope specs`, `python specs/scripts/validate.py --strict`
  (`inventory=106183`, `coverage=104655`), `ruff check specs/scripts` và
  `black --check specs/scripts` đều pass; `harness-cli story verify
  BE-PC-PARITY-SPECS-001` cũng pass.
- Replan audit `orch-5efb771f2403ffff` phát hiện và root đã sửa ba artifact
  mismatch: policy count `41`, checklist lint/format và exact focused-unit
  command/output. Thay đổi ngoài scope ở submodule `backend/cores` được giữ
  nguyên và run đó bị supersede, không dùng làm clean boundary proof.
- Closure run `orch-1e5200ef0bf1eddd` đã `finish --verified` với current Harness
  binding; trace `#4` đạt detailed 3/3. `harness-cli story complete
  BE-PC-PARITY-SPECS-001 --json` chạy fresh proof pass và chuyển story sang
  `implemented`.
