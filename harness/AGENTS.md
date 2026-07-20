# Agent Instructions

## PC Source Of Truth

- PC docs chuẩn: `/var/www/jx-source/01_tinh_kiem_source/tai-lieu-game`.
- Canonical PC source duy nhất là `/var/www/jx-source`; coi toàn bộ cây này là read-only.
- Canonical runtime/PAK đã unpack là `/var/www/jx-source/pak_unpacked/`.
- Index/audit hiện hành: `/var/www/jx-source/docs/SOURCE_INDEX.md` và `/var/www/jx-source/docs/SCAN_REPORT_TINH_KIEM.md`.
- C++/source tree cần tra trước khi port: `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/`.

## Canonical PC Rules

- Với PAK, SPR, DAT, Hash_UID hoặc encoded config, bắt buộc dùng `~/Projects/vltktool`; không tự hash/decode hoặc đoán encoding.
- Với SPR thì nên dùng hash/disk path không nên dùng logical path. logical path Chỉ để hiểu logic,behavior code của Pc
- SPR có text/UI: luôn kiểm tra `bin/client/package.ini` để chọn **winner theo package priority** (ví dụ Vietnamese override `update01.pak` có thể ghi đè `spr.pak`); không dùng fallback tiếng Trung chỉ vì logical path trùng.
- Resolve logical path → UID bằng `vltktool resolve_uid.py`, extract đúng frame winner bằng `vltktool extract_item_spr.py`, rồi `cmp` với PNG Unity và lưu UID/package/frame + SHA-256 vào provenance trước khi dùng.
- Không copy candidate chỉ để làm evidence. Chỉ vendor exact bytes vào repo-local slice khi asset/config đã được chọn và thực sự dùng.
- Không sửa bất kỳ file nào dưới `/var/www/jx-source`.

## Backend Game Server Rules

- Backend nằm tại `/var/www/vltk-mobile/backend` và là **Git repo riêng** với repo
  `/var/www/vltk-mobile`; chạy `git`, cài dependency, lint và test từ đúng repo,
  đồng thời giữ nguyên mọi thay đổi không thuộc task.
- Trước khi sửa backend, đọc `/var/www/vltk-mobile/backend/AGENTS.md`, `README.md`
  và `pyproject.toml` trong repo đó, rồi dùng `srcwalk` khảo sát đúng module,
  caller, dependency và test liên quan. Không dùng số file, số commit `Pxxx`,
  docstring hay test chỉ kiểm tra constant để kết luận tiến độ/runtime parity.
- Thứ tự source of truth: hành vi PC từ **đúng file đang tồn tại** dưới
  `/var/www/jx-source` -> target contract tại
  `/var/www/vltk-mobile/domains/server-runtime/README.md`, `contracts/` và
  `Assets/Scripts/Backend/` trong repo mobile -> implementation cùng executable
  tests trong backend. Nếu các tầng mâu thuẫn, nêu rõ và không tự chọn theo
  comment/commit cũ.
- Mỗi slice port phải xác minh đúng variant client/server, version và language;
  ghi exact source path + Git commit/hash, function/line mapping, chuỗi `Include`,
  engine API/data dependency và phần chưa port. Nhiều file PC trùng basename nên
  path trong docstring không phải bằng chứng nếu path đó không tồn tại.
- Phân loại trạng thái từng slice là `runtime-wired`, `metadata-only`,
  `stub/TODO` hoặc `missing`. Chỉ gọi là hoàn tất khi behavior thực sự được wire
  qua application/API hoặc game loop và có test outcome; constant/string mô tả
  Lua engine call không phải implementation.
- Không đoán behavior của C++/Lua engine, global state, scheduler, RNG, encoding,
  index hay time unit. Giữ rõ semantics 1-based/0-based, TCVN3/GB2312/raw bytes;
  nếu chưa chứng minh được thì để gap/TODO có source evidence, không bịa default
  hoặc silently normalize dữ liệu.
- Stack hiện tại là Python 3.12, FastAPI, SQLAlchemy/PostgreSQL và modular
  monolith DDD. Domain giữ pure; application điều phối use case; infrastructure
  chứa adapter/repository; API chỉ gọi application. Không mở rộng cross-layer
  import hoặc tiện tay refactor các vi phạm legacy ngoài slice đang làm.
- Router được auto-discover và lỗi import chỉ bị log rồi bỏ qua; sau thay đổi API
  phải kiểm tra route thực sự xuất hiện trong app/OpenAPI, không coi server boot
  thành công là đủ. Mọi đổi endpoint/DTO/envelope phải đối chiếu Unity
  `IGameBackend`/DTO và contract tương ứng.
- Port local lấy từ `.env`/README (hiện là `8120`), không hard-code theo scaffold
  `8020`. Backend là game server độc lập; không thêm phụ thuộc auth/user/ERP nếu
  không có contract được chấp thuận.
- Validation mặc định: test unit nhỏ nhất, rồi `ruff check` và `black --check`
  trên phạm vi thay đổi; mở rộng theo wiring/contract. `tests/integration/` và
  `tests/e2e/` dùng PostgreSQL thật và fixture có `TRUNCATE ... CASCADE`: chỉ chạy
  khi đã xác minh database test disposable/isolated, tuyệt đối không chạy vào DB
  shared, dev có dữ liệu hoặc production.


<!-- HARNESS:BEGIN -->
## Harness

Choose the request class before any Harness operation.

- When the requested outcome is only an answer, explanation, review, diagnosis,
  plan, or status report: inspect only the material needed to respond. Keep the
  task read-only. Do not bootstrap, initialize or migrate a database, record
  intake, or record a trace.
- When the user explicitly asks to change, build, fix, or write repository
  artifacts: first run `scripts/bootstrap-harness.sh`
  on macOS/Linux or `.\scripts\bootstrap-harness.ps1` on Windows. Then use
  `docs/FEATURE_INTAKE.md` to classify and record the request, query
  `scripts/bin/harness-cli query matrix --active --summary` on macOS/Linux or
  `.\scripts\bin\harness-cli.exe query matrix --active --summary` on Windows,
  and retrieve only the lane- and task-specific context described in
  `docs/CONTEXT_RULES.md`.
<!-- HARNESS:END -->
