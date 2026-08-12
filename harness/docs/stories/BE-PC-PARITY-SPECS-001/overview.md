# Overview

## Current Behavior

Backend Python/FastAPI tại `/var/www/vltk-mobile/backend` đã có nhiều module và
test port từ PC; bộ đặc tả thống nhất hiện nằm tại `backend/specs/`. Trạng thái
port vẫn không thể suy ra an toàn từ số file, docstring hoặc test constant. Intake
high-risk hiện hành là `#3`; story đang được thực thi qua Herdr run
  `orch-5c58d1c2131bc464` (re-audit wave; prior integration wave was
  `orch-46fc75190d57cfa7`).

Canonical PC source là cây read-only `/var/www/jx-pc`, trong đó source engine
được pin tại repo lồng
`/var/www/jx-pc/01_tinh_kiem_source/source/00.src-tinh-kiem`. Worktree PC đang
dirty nên mọi claim behavior phải ghi cả commit và SHA-256 của file thực tế đã
đọc. Mobile contracts tại `/var/www/vltk-mobile/domains/server-runtime/`,
`/var/www/vltk-mobile/contracts/` và `/var/www/vltk-mobile/Assets/Scripts/Backend/`
là tầng target contract thứ hai; implementation/test backend là tầng thứ ba.

## Target Behavior

Audit và cập nhật bộ specs backend tiếng Việt tại `/var/www/vltk-mobile/backend/specs/`
theo CNPM pha 1–3, bao phủ toàn bộ behavior PC có trách nhiệm server. Mỗi requirement
có ID ổn định, provenance exact path/revision/hash, mapping target/backend,
trạng thái `runtime-wired`, `metadata-only`, `stub/TODO` hoặc `missing`, gap,
priority và acceptance proof. Pha `04-thiet-ke-giao-dien` không áp dụng; thay bằng
coverage inventory, gap matrix, roadmap/checklist và completion audit.

## Affected Users

- Lập trình viên backend Python/FastAPI tiếp tục port behavior PC.
- Lập trình viên Unity duy trì client contract và reconciliation server-authoritative.
- Reviewer/tester xác minh parity và bằng chứng runtime.
- Vận hành game server theo dõi durability, content và compatibility.

## Affected Product Docs

- `/var/www/vltk-mobile/backend/specs/`
- `/var/www/vltk-mobile/domains/server-runtime/README.md` (nguồn target, không sửa trong story này)
- `/var/www/vltk-mobile/contracts/` (nguồn target, không sửa trong story này)

## Non-Goals

- Không implement hoặc refactor runtime backend trong story này.
- Không sửa bất kỳ byte nào dưới `/var/www/jx-pc`.
- Không phục hồi specs/stories đang bị xóa trong worktree mobile; chỉ đối chiếu
  target hiện tồn tại và ghi path exact.
- Không đặc tả UI/rendering/audio/SPR thuần client, trừ seam mà server phải phát dữ liệu hoặc enforce rule.
