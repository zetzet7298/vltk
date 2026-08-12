# Audit hoàn tất theo yêu cầu nguồn

## Nguồn và quy tắc kết luận

- Nguồn yêu cầu: `/home/zet/.codex/attachments/60f4a53b-009c-4170-993a-6fcd2c10b460/pasted-text-1.txt`.
- Ngày audit: `2026-07-16`; package: `SPEC-JX-PC-MOBILE-001`.
- `ĐẠT` chỉ dùng khi artifact hiện tại và kiểm tra tương ứng cùng chứng minh yêu cầu.
  `BLOCKED` là blocker external/runtime có owner và exit criteria; không được đổi thành
  `ĐẠT` nhờ static prose. Trạng thái trung gian không phải acceptance.
- PC docs Lab 1-8 là secondary audited evidence. Source/config/PAK winner và runtime
  golden vẫn theo `governance/source-authority.md`.

## Ma trận phạm vi và CNPM

| Ref nguồn | Điều kiện hoàn tất | Artifact/chứng minh | Trạng thái |
| --- | --- | --- | --- |
| Dòng 5-8 | Chỉ sửa specs/validator; không sửa Unity/Go/dhcd | `delivery/spec-scope-manifest.yaml`; cần immutable baseline/diff evidence từng wave | ĐẠT về policy; `BLOCKED` kiểm chứng compliance wave vì worktree có dirty path ngoài scope không thể quy attribution; không nhận hoặc hoàn nguyên chúng |
| Dòng 9-10 | Tiếng Việt, domain-first, client + server; P0-P1 sâu | Bốn tài liệu CNPM + `domains/`, fixture/matrix P0 | ĐẠT ở spec; runtime parity còn blocked |
| Dòng 12-13 | jx-pc read-only; vltktool cho PAK/hash/encoding | Source policy + snapshot/package/UID catalog | ĐẠT ở spec; G1 `BLOCKED` vì 4 PAK thiếu và winner chưa resolve |
| Dòng 17-30 | Đủ cấu trúc package bắt buộc | Required-layout validator | ĐẠT |
| Dòng 32-33 | Giữ heading/thứ tự/cột bốn template CNPM | CNPM signature validator | ĐẠT |
| Dòng 35-36 | Pha 1 có >=40 nhu cầu, actor/BM/QĐ/FR/NFR/trách nhiệm/AC | `01-yeu-cau.md` | ĐẠT |
| Dòng 38-39 | Pha 2 có cây, quyền, state, D1-D6, seam, deployment | `02-mo-hinh-yeu-cau.md` | ĐẠT |
| Dòng 41-42 | Pha 3 có ERD/field/key/invariant/index/transaction/migration/backup/catalog | `03-du-lieu.md`, SQL, data dictionary 48 bảng/528 cột, 15 catalog file | ĐẠT ở spec |
| Dòng 44-47 | Pha 4 và đủ shard domain đã liệt kê | `04-giao-dien.md`, `domains/` | ĐẠT ở spec |
| Dòng 96-109 | Reconciler, thứ tự pha, file ownership, cross-review và QA merge/gate | `governance/orchestration.md`; required-layout validator | ĐẠT |

## Ma trận gameplay, visual và UX

| Ref nguồn | Điều kiện hoàn tất | Artifact/chứng minh | Trạng thái |
| --- | --- | --- | --- |
| Dòng 62-64 | Ghi đúng gap brownfield đã nêu | `as-is/`, debts/migrations | ĐẠT |
| Dòng 66-68 | P0 lab 5 NPC/all skill; P1 vertical slice; P2-P4 wave | Fixture 5 NPC, static skill selection `1.554` row + novice/faction joins, matrix shared/novice/10 phái, progression source table and requirements/domain/delivery | ĐẠT ở spec; formula/effect/visual/audio runtime oracle và unresolved rows blocked |
| Dòng 70-72 | 18 Hz, fixed-point, deterministic RNG, target/aim/pending/leash | Combat/skill/auto shard + typed Proto target/aim | ĐẠT ở spec; formula/RNG golden blocked |
| Dòng 74-75 | Túi 60, mỗi item một ô, no-drag, mutation lock | UI/domain/SQL/Proto tests | ĐẠT ở contract; runtime chưa triển khai |
| Dòng 77 | HUD mobile hiện tại freeze `1280x720`; panel song song flag | UI shard/golden/migration | BLOCKED: thiếu rect/anchor/hash baseline |
| Dòng 77-79 | Ưu tiên SPR Việt; fallback chrome/frame PC + text Việt + debt | UI/content policy + manifest/SQL fallback contract | ĐẠT ở spec; winner/visual golden blocked |
| Dòng 119-120 | Skill logic 100%; từng case SSIM >=0,99; gate UI/map/avatar/audio | Test registry/golden manifest | BLOCKED: live PC golden chưa có |

## Ma trận backend và contracts

| Ref nguồn | Điều kiện hoàn tất | Artifact/chứng minh | Trạng thái |
| --- | --- | --- | --- |
| Dòng 81-83 | Python 3 + FastAPI modular monolith trong `backend/`, PG16, REST và realtime seam; no fallback | ADR-0008/server shard/contracts | ĐẠT ở spec; realtime chưa hoàn tất |
| Quyết định user | PostgreSQL 16 dùng stack `/var/www/tt-docker`, không chép secret vào spec/client | ADR-0008, release plan và PostgreSQL runtime audit disposable | ĐẠT ở spec; deployment G6 chưa chạy |
| Dòng 85-87 | OpenAPI/Proto/content/error/idempotency/SQL; ACK sau commit | `contracts/`, realtime semantics và `contracts/sql/game.v1.negative.sql` | ĐẠT ở contract; Python realtime/ACK chưa triển khai |
| Dòng 89-91 | REST và WSS bao phủ toàn endpoint/message bắt buộc | OpenAPI/Proto lint + typed delta/resume/quest/combat/inventory | ĐẠT ở spec |
| Dòng 93-94 | Bundle pin version/hash/locale/provenance; no hot reload; Lua 5.1 sandbox | Manifest/SQL Lua policy + whitelist digest | ĐẠT ở spec |
| Dòng 122-124 | Đủ deterministic/proto/economy/reconnect/channel/checkpoint/Lua/migration/release tests | Test registry + test-result contract | ĐẠT ở spec; result index `BLOCKED` đến khi có implementation |
| Dòng 126-127 | 1000 CCU/SLO/AOI/device gate | NFR/test strategy | ĐẠT ở spec; runtime unverified |

## Ma trận governance, census và release

| Ref nguồn | Điều kiện hoàn tất | Artifact/chứng minh | Trạng thái |
| --- | --- | --- | --- |
| Dòng 51-53 | Authority/gap/evidence/contradiction/ADR/glossary/trace; default unverified | Governance/as-is/registry; 504 trace edge | ĐẠT |
| Dòng 55-56 | Mỗi stable ID có DRI/reviewer/phase/status/acceptance evidence | Strict schema; 252/252 governed record hợp lệ | ĐẠT |
| Dòng 58-60 | Visual/behavior authority và reference UX tách rõ | Source authority/ADR | ĐẠT |
| Dòng 112-114 | Ba mode validator kiểm đủ CNPM/ID/link/schema/lifecycle/trace/contradiction/freshness/MinIO | Validator fail-closed + 45 negative/unit test | ĐẠT |
| Dòng 116-117 | Census 100% skill/map/NPC/item/quest/script/UI/audio, không vô chủ | 17.574 record; source census/path-set độc lập; generator byte-reproducible | ĐẠT census; release `BLOCKED` cho coverage unresolved: audio 72, avatar 91, Lua 1.232, package 31, quest 8.451, SPR 1.345; các tập có thể giao nhau nên không cộng thành số entity duy nhất |
| Dòng 129-130 | Không `PARITY_DONE` trước live PC runtime golden/reviewer | Lifecycle + release validator | BLOCKED đúng chủ đích |
| Dòng 132-133 | Out-of-scope đúng; mock/local không migrate production | `spec.yaml`, migrations/source policy | ĐẠT |
| Quyết định user | Gameplay-first và client-priority, backend seam đi theo wave test được | Migration M1-M4 + P0/P1 acceptance | ĐẠT ở spec |

## Bộ lệnh chứng minh bắt buộc

```bash
python3 -m unittest discover -s harness/scripts/spec-validator/tests -v
python3 harness/scripts/spec-validator/validate.py harness/specs/jx-pc-mobile-port --mode authoring
python3 harness/scripts/spec-validator/validate.py harness/specs/jx-pc-mobile-port --mode premerge
python3 harness/scripts/spec-validator/validate.py harness/specs/jx-pc-mobile-port --mode release
npx --yes @redocly/cli lint harness/specs/jx-pc-mobile-port/contracts/openapi/game.v1.yaml
  protoc --proto_path=harness/specs/jx-pc-mobile-port/contracts/proto \
  --descriptor_set_out=/tmp/game.pb \
    harness/specs/jx-pc-mobile-port/contracts/proto/game/v1/game.proto
  DB="jx_spec_negative_${RANDOM}"; createdb "$DB" && \
    psql -X -v ON_ERROR_STOP=1 -d "$DB" \
      -f harness/specs/jx-pc-mobile-port/contracts/sql/game.v1.sql \
      -f harness/specs/jx-pc-mobile-port/contracts/sql/game.v1.negative.sql; \
    rc=$?; dropdb --if-exists "$DB"; test "$rc" -eq 0
  git diff --check -- harness/specs/jx-pc-mobile-port harness/scripts/spec-validator \
  harness/scripts/generate-jx-spec-catalog.py
```

Release chỉ được kết luận hoàn tất khi không còn trạng thái trung gian và mọi
`BLOCKED` có evidence gỡ block thật. Authoring/premerge xanh không thay thế
golden, census closure, test result hoặc reviewer sign-off.
