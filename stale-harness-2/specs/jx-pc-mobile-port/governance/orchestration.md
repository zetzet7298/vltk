# Quy trình điều phối và quyền sở hữu specs

Tài liệu này là contract quy trình cho việc khảo sát, viết, review và promote bộ
specs. Nó không cấp quyền nâng lifecycle nếu thiếu evidence theo
`source-authority.md` và `lifecycle.md`.

## Thứ tự bắt buộc

| Bước | DRI | Đầu vào | Đầu ra/gate mở bước sau |
| --- | --- | --- | --- |
| 1. Khóa corpus | Reconciler | `/var/www/jx-pc` read-only, active `package.ini`, `vltktool` revision | Source snapshot, package order, corpus census và contradiction ledger; claim chưa chứng minh giữ `BLOCKED` |
| 2. Pha 1 CNPM | Product/spec-governance | Nhu cầu người dùng, quyết định đã chốt, evidence bước 1 | `01-yeu-cau.md` đúng template, requirement/AC/owner hợp lệ; validator authoring sạch |
| 3. Pha 2 CNPM | Architecture/domain owners | Pha 1 đã hợp lệ | `02-mo-hinh-yeu-cau.md`, quyền, state, D1-D6 và seam; không mở pha 3/4 nếu requirement đầu vào chưa có |
| 4. Domain shards | Owner từng miền | Requirement và source authority đã pin | Invariant, behavior, failure mode, test và blocker theo file ownership bên dưới |
| 5. Data/contracts/UI | Backend-data và Unity-UI | Domain shard cùng revision | SQL/OpenAPI/Proto/content schema và screen contract đã cross-review authority seam |
| 6. QA/evidence merge | QA/evidence | Catalog, contract, fixture, test case và golden metadata | Registry/trace/gate/test-result nhất quán; contradiction được Reconciler xử lý |
| 7. Promote wave | Reviewer của gate | Validator, test artifact, golden và sign-off cùng revision | Chỉ promote lifecycle khi exit criteria thật đạt; thiếu evidence tiếp tục `BLOCKED` |

## File ownership độc quyền

Trong một wave, chỉ owner được ghi ở bảng dưới sửa file thuộc phạm vi của mình.
Agent khác gửi finding/patch proposal cho owner; không có hai agent cùng sửa một
file. Reconciler sở hữu conflict resolution, không tự chọn claim thắng thiếu evidence.

| Owner | Phạm vi file |
| --- | --- |
| `spec-governance` | `00-index.md`, bốn file CNPM root, `governance/`, `spec.yaml` |
| `reconciler` | `as-is/claims.yaml`, `evidence.yaml`, `contradictions.yaml`, source snapshot và package/winner ledger |
| `identity-character` | `domains/account-character-session.md` |
| `world`, `combat`, `skills`, `progression`, `automation` | Shard domain cùng tên; không sửa contract transport trực tiếp |
| `item-economy`, `npc-quest`, `companion`, `social-endgame` | Shard domain cùng tên; thay đổi durable seam phải gửi backend-data review |
| `unity-ui` | `domains/ui-hud-panels/`, `04-giao-dien.md`; HUD geometry freeze không được đổi |
| `content-parity` | `domains/content-assets-avatar-audio/`, catalog generator/output và manifest provenance |
| `backend-contract` | `domains/server-runtime/`, `contracts/openapi/`, `contracts/proto/`, error/version/idempotency prose |
| `backend-data` | `03-du-lieu.md`, `contracts/sql/`; PostgreSQL 16 invariant và migration |
| `qa-evidence` | `registry/tests.yaml`, golden/test-result manifest, `delivery/` và trace edge sau owner review |
| `scope-audit` | `delivery/spec-scope-manifest.yaml`; chỉ ghi ranh giới được phép và evidence của wave, không nhận attribution ngoài phạm vi |

## Ranh giới audit thay đổi

`delivery/spec-scope-manifest.yaml` là nguồn machine-readable cho phạm vi được
phép của công việc specs. Kết luận scope chỉ được ghi là **đạt trong phạm vi
thay đổi do orchestrator sở hữu và evidence audit của wave hiện tại**. Một
worktree sạch không phải tiền đề của claim này; các file bẩn có trước hoặc ngoài
allowlist được xem là `UNATTRIBUTED_OUT_OF_SCOPE`, không được nhận là do
orchestrator tạo, không được hoàn nguyên và cũng không được dùng để khẳng định
toàn repository sạch.

Mỗi wave phải lưu danh sách path thực tế đã sửa và đối chiếu với allowlist. Nếu
có path ngoài allowlist mà không có quyết định mở scope, audit wave phải fail;
Reconciler chuyển finding tới owner thay vì tự sửa. Manifest chỉ là policy input,
không tự chứng minh compliance nếu thiếu baseline/diff evidence.

## Cross-review authority seam

Mỗi thay đổi command/event/schema phải có checklist cùng revision:

1. Backend xác nhận authorization, validation, transaction, idempotency và ACK sau commit.
2. Unity xác nhận intent/prediction chỉ là presentation; snapshot/delta/revision có thể reconcile và không tạo authority thứ hai.
3. Data xác nhận aggregate/FK/RLS/cross-realm/content-release invariant biểu diễn được.
4. QA chạy schema/negative/golden vector; ghi test result có hash/revision/reviewer.
5. Reconciler kiểm visual/behavior source authority và contradiction trước khi merge.

Thay đổi chỉ có prose hoặc test file chưa chạy không đủ mở gate. Sau mỗi wave chạy
validator `authoring`, `premerge`, rồi review source-backed; `release` chỉ chạy để
chứng minh toàn bộ blocker đã đóng, không dùng để che blocker bằng warning.
