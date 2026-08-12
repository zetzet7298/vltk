# vltk (vltk-mobile)

**Unity client cho bản mobile mới của Võ Lâm Truyền Kỳ 1**, port lại từ client/engine PC gốc (SwordOnline/JX). Kết nối với backend ở repo `vltk-server`. Đây là testbed thực tế cho quy trình phát triển **AI-native, multi-agent** — build tính năng thật, có kiểm thử, không chỉ demo/snippet.

## Repo này làm gì

- Client Unity/C# cho game, hiện đang tập trung vào chế độ "Survivor" (kiểu Vampire-Survivors) dùng skill/VFX/nhân vật port từ game gốc (đã thấy trong log: skill phái Cái Bộng, joystick mobile, Y-sort, death VFX...).
- Giao tiếp với backend qua REST hiện tại (`IGameBackend`), có seam hướng tới WSS/Protobuf `game.v1` cho tương lai.

## Điểm nổi bật về AI-native workflow (quan trọng cho portfolio)

1. **Multi-agent harness tự xây**: cấu hình `.harness`, `.agent`, `.mcp`, `.opencode`, `.kiro` để nhiều AI coding tool (Claude Code, Kiro, OpenCode) làm việc trên cùng codebase qua context layer chung dựa trên **MCP (Model Context Protocol)** — chạy MCP-for-Unity server thật (`http://127.0.0.1:8080`) kết nối trực tiếp vào Unity Editor.
2. **Quy trình kanban có kiểm chứng**: công việc theo dõi bằng ticket (`docs/plans/active/...`); mỗi commit ghi rõ ticket, thay đổi, và bằng chứng kiểm thử cụ thể (ví dụ: "EditMode 283/283 pass, PlayMode 41 tests, 0 console errors").
3. **Cổng dual-review**: nhiều ticket bắt buộc qua bước "dual review PASS" trước khi đóng — cơ chế QA nhẹ để code do AI viết không được merge nếu chưa có review độc lập.
4. **Context/spec engineering**: `openspec/`, `contracts/`, `domains/`, `handoff/`, `CHANGELOG.md` giúp agent tiếp tục công việc sau nhiều ngày mà không cần giải thích lại toàn bộ codebase.
5. **Provenance tracking cho asset**: mỗi skill/sprite port có file provenance ghi SHA-256 + pak index + frame để truy vết nguồn gốc chính xác.

## Tech stack

Unity, C#, MCP for Unity, custom agent harness, EditMode/PlayMode automated tests.

## Vai trò của tôi

Tôi là người duy nhất vận hành: thiết kế harness, viết ticket/spec, cấu hình MCP + agent tooling, review từng diff, quyết định kiến trúc/sản phẩm. Commit đứng tên agent identity (`vltk-unity-worker`) do harness tự cấu hình — không phải cộng tác viên khác.

## Lưu ý phạm vi

Project cá nhân dùng để luyện AI-native game dev workflow trên một codebase lấy cảm hứng từ game PC kinh điển; không phân phối thương mại, không kèm asset/bytes gốc có bản quyền của game gốc.
