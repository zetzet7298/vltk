# Exec Plan

## Goal

Tạo bộ specs đủ để tiếp tục port backend Python/FastAPI mà không bỏ sót behavior
server-relevant của canonical PC source, có gap/checklist/priority và proof audit
máy đọc được.

## Scope

In scope:

- CNPM `01-yeu-cau.md`, `02-mo-hinh-yeu-cau.md`, `03-du-lieu.md`.
- Index/source authority/glossary, domain behavior specs và protocol/data seams.
- Source inventory, requirement coverage, gap/roadmap/checklist, provenance và validator.
- C++ Core/server/protocol/database, live server Lua/config/data và client sources chỉ khi cần phân định contract/variant.
- Đối chiếu mobile target contract và toàn bộ backend runtime/tests hiện hành.

Out of scope:

- Sửa runtime backend, Unity hoặc PC source.
- `04-thiet-ke-giao-dien` và behavior thuần presentation.
- Chạy integration/e2e backend vào PostgreSQL chưa chứng minh disposable.

## Risk Classification

Risk flags:

- Authorization.
- Data model.
- Audit/security.
- Public contracts.
- Cross-platform.
- Existing behavior.
- Weak proof.
- Multi-domain.

Hard gates:

- Không được đoán engine/global/scheduler/RNG/encoding/index/time semantics.
- Không được gọi một slice hoàn tất nếu chưa runtime-wired và có proof outcome.
- Không sửa canonical PC source hoặc làm yếu validation để đạt completion.

## Work Phases

1. Pin revision/digest và tạo exhaustive scope manifest.
2. Inventory PC/mobile/backend, phân loại domain và variant.
3. Viết CNPM pha 1, sau đó pha 2, rồi pha 3 theo dependency.
4. Viết domain specs và gap/roadmap/checklist.
5. Chạy validator, review coverage và audit revision drift.
6. Ghi Harness proof/trace và chỉ complete khi global gate được chứng minh.

## Execution Brief

- Authority: change, chỉ ghi `backend/specs/` và artifact Harness của story này.
- Pinned PC repo commit: `d4bfc04a3dbb8f964be1ee8cd9b6dec6fc4e1b91`; worktree dirty nên file digest là bắt buộc.
- Pinned backend commit: `bc8d4a9883a359b87fe92f159524a22d88d54f31` cộng thay đổi người dùng hiện hữu không thuộc story.
- Pinned mobile commit: `657b1d3230a58c33324701a60157cd03b258badf` cộng worktree contract hiện hành.
- Harness: protocol 1, schema 13, intake #3, story `BE-PC-PARITY-SPECS-001`.
- Mode: Herdr re-audit `orch-5c58d1c2131bc464`; lanes read-only (`wG:p5N`, `wG:p5P`), root giữ quyền tích hợp và ghi specs. Prior integration wave `orch-46fc75190d57cfa7` remains provenance context.
- Critical path: lane evidence → root source verification → coverage state/overclaim correction → CNPM/domain/gap docs → validator → global audit.
- Verification: validator specs, raw placeholder/orphan checks, srcwalk review, Markdown/JSON/YAML parse, revision/digest drift audit.
- Re-audit evidence: both read-only lanes collected clean; root fresh gates and
  `harness-cli story verify BE-PC-PARITY-SPECS-001` pass after formatting
  `backend/specs/scripts/validate.py`. No runtime backend or PostgreSQL
  integration/e2e changes were made.
- Terminal lifecycle: `orch-5c58d1c2131bc464` bị replan do work-source revision
  drift; `orch-5efb771f2403ffff` được supersede khi boundary evidence bắt thay
  đổi ngoài scope ở `backend/cores`; clean root-only closure
  `orch-1e5200ef0bf1eddd` đã `finish --verified`, rồi Harness trace `#4` và
  `story complete` pass.

## Stop Conditions

Pause for human confirmation if:

- Cần mở rộng sang implement runtime, sửa Unity contract hoặc sửa PC source.
- Hai PC variants đều có thể là runtime winner mà package/config/startup evidence không phân định được.
- Yêu cầu data migration/deletion hoặc thay architecture xuất hiện.
- Validation phải bị làm yếu hoặc source of truth phải đổi để có thể complete.
