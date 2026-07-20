# Design

## Source Boundary

- Static rows: `/var/www/jx-source/pak_unpacked/slistcache/settings/skills.txt`.
- Exact repo slice: `Assets/StreamingAssets/Reference/PcCaiBangSkills.txt`, SHA-256 `7aa82d708a8ecdbdcdf6d7e2ce1974fde9286832d6f2ffff1d3c2d182a440973`.
- Relationship supplement: canonical client `bin/client/script/skill/gaibang.lua`, SHA-256 `56d9910a0d601ee28f40f26f257af1bb6f98757c8319a1b336926bc9d4471ed8`.
- Canonical tree là read-only; encoded/extracted assets tiếp tục dùng `vltktool`.

## Oracle Flow

```text
exact PC slice + two evidenced Lua relationships
  -> scripts/generate_caibang_oracle.py (stdlib only, no Unity imports)
  -> PcCaiBangOracle.json + .sha256
  -> CaiBangCanonicalOracleParityTests
  -> PcCombatCatalogFactory result comparison
```

Generator dùng Latin-1 byte-preserving để không tự đoán/đổi encoding; chỉ diễn giải ASCII headers, số và paths. JSON được sort key, compact, UTF-8/LF, không timestamp hay machine-dependent output.

## Root Contract

Oracle pin đúng 26 root IDs theo player panel: `115-130`, `274`, `277`, `357-360`, `714`, `720`, `1073`, `1074`. Support/event/NPC nodes không tính root. `357 -> collide 389` và `1073 -> collide 1072` lấy từ Lua relationship evidence.

## Anti-Circularity

- Generator không đọc/import `PcCombatCatalogFactory`, `CombatRuntimeService`, `SkillDefinition` hoặc test fixture.
- Unity test đọc artifact hash-pinned rồi so với production catalog.
- Source cell trống không bị biến thành expected `0`; artifact có `present[]`, test chỉ assert static field thật sự có giá trị.
- `MisslesForm=0` giữ ngoại lệ đã document: Unity có thể giữ render form fallback khi PC row vẫn sở hữu child missile.

## Alternatives Rejected

- Expected hard-code riêng trong từng test: phân tán và dễ drift.
- Test parse cùng TSV rồi so catalog: circular ở thời điểm chạy, không có reviewed frozen result.
- Thêm schema framework/dependency: không cần; version field + hash + generator check đủ cho slice hiện tại.
