# Exec Plan — Remove `harness-be`

## Goal

Xoá repository phụ `harness-be` để chỉ còn Harness chính tại `harness/`.

## Scope

In scope:

- Xoá toàn bộ `/var/www/vltk-mobile/harness-be`, gồm nested Git metadata và worktree nội bộ.
- Bỏ rule `harness-be/` khỏi `.gitignore` cấp project.
- Xác minh không còn thư mục hoặc tracked reference.

Out of scope:

- Thay đổi `harness/`, `backend/`, Unity client hoặc PC canonical source.
- Khôi phục hay vendor nội dung từ `harness-be`.

## Risk Classification

Risk flags:

- Data loss.
- Existing behavior.
- Weak proof.

Hard gates:

- Xoá vĩnh viễn file untracked trong nested repository.

User đã yêu cầu rõ "xoá hẳn"; không còn mơ hồ cần xác nhận thêm.

## Work Phases

1. Kiểm tra Git status, remote, worktree và reference bên ngoài.
2. Ghi nhận story và proof command.
3. Xoá directory, bỏ ignore rule.
4. Xác minh filesystem, tracked reference và diff.
5. Ghi trace.

## Stop Conditions

Dừng nếu phát hiện dependency từ code/config đang hoạt động ngoài `.gitignore`.
