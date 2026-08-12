# Design

## Domain Model

Mỗi union row có một trong ba class: `shared`, `pc_learned_only`, hoặc
`unity_display_only_unresolved`. Learned oracle chỉ chứa exact set từ canonical PC
progression ∪ skillbook. Direct relationship fields phải resolve bằng repo-local
vltktool slices mà không biến support target thành learned skill.

## Application Flow

1. Parse membership từ pinned `skills_table.lua` và `skillbook.lua`.
2. Observe `PcKunLunSkillOrder` chỉ như display evidence.
3. Trích union/static relationship rows bằng `vltktool`.
4. Freeze membership classification + independent oracle JSON.
5. Diff production catalog/selection state với frozen oracle.

## Interface Contract

Không đổi public API ở proof phase. Artifact contract phải deterministic,
fail-on-stale và ghi canonical paths, hashes, source lines, classification,
categories, populated static fields và direct relationships.

## Data Model

Không migration. Chỉ thêm repo-local text/JSON proof fixtures và Unity `.meta`.

## UI / Platform Impact

Không đổi panel order trong proof phase. Android/device smoke để lại epic residual.

## Observability

Harness detailed trace, generator `--check`, Python tests, Unity compile/EditMode
proof và independent Herdr audit.

## Alternatives Considered

1. Dùng 18 Unity display roots làm expected: bác bỏ vì circular và bỏ sót 11 PC-only.
2. Port cả runtime ngay: bác bỏ cho tới khi membership/static diff được freeze.
3. Parse full `skills.txt`: bác bỏ; encoded source bắt buộc đi qua `vltktool`.
