# CI Và Promotion

| Trường | Giá trị |
|---|---|
| Mục đích | Tự động hóa quality/provenance/replay gate trên self-hosted runner |
| Trạng thái | `not_started` |
| Owner / reviewer | Ops owner / technical lead |
| Cập nhật | 2026-07-15 |

## Pipeline

1. Secret/dependency/license scan.
2. Markdown/YAML/link/status validator.
3. Go fmt/vet/unit/integration/contract/replay tests.
4. Unity compile/EditMode/PlayMode/headless golden.
5. Asset manifest/provenance/legal gate.
6. Build Android, sign outside repo, artifact hash.
7. Load/performance smoke và deploy canary.

## Promotion

`dev -> internal pilot -> staged pilot`; mỗi bước có approval, feature flags, migration/rollback and telemetry check. Self-hosted runner phải pin toolchain và không được truy cập secret ngoài job scope.

## P1 content release pipeline

`REQ-P1-004` mở rộng CI P0 thành pipeline content reproducible; ledger status của `DOC-OPS-05` vẫn áp dụng cho P0 CI baseline, còn P1 extension được trace riêng. Input bundle phải pin `content_bundle_id`, catalog/config version, selected JX asset manifests, schema compatibility, legal state và dependency graph.

Pipeline phải:

1. validate referential integrity, provenance/hash/load-order/decode và legal gate;
2. import/build bằng pinned toolchain, không sửa trực tiếp production database hoặc Unity scene;
3. chạy catalog, C#/Go vector, Unity headless và visual/audio golden liên quan;
4. tạo immutable bundle + SHA-256 + signer/approval record;
5. canary bằng feature flag, telemetry và compatibility reader;
6. promote hoặc rollback về bundle trước mà không đổi receipt/replay đã commit.

### P1 acceptance

- Cùng input/toolchain tạo cùng bundle hash; missing/ambiguous asset fail closed.
- Canary, rollback, expiry và owner được ghi; không có direct production mutation.
- Bundle chỉ promote khi provenance, legal, schema compatibility và regression gates pass.

## Acceptance

- [ ] CI chạy toàn bộ Go/Unity/provenance/contract/replay/secret gates.
- [ ] Artifact có hash, provenance và promotion approval.
- [ ] Canary/rollback pipeline được diễn tập trên self-hosted runner.
