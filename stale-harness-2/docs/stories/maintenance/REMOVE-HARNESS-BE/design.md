# Design — Remove `harness-be`

## Application Flow

Không có runtime flow. Đây là cleanup filesystem:

1. Xoá nested repository `/var/www/vltk-mobile/harness-be`.
2. Xoá đúng một dòng `harness-be/` khỏi root `.gitignore`.

## Data Model

Không đổi database hay schema. File untracked trong nested repository bị xoá theo yêu cầu.

## UI / Platform Impact

Không có.

## Observability

Harness intake, story và trace lưu bằng durable CLI.

## Alternatives Considered

1. Đổi tên hoặc archive: loại bỏ vì vẫn giữ nguồn gây nhầm lẫn.
2. Chỉ xoá ignore rule: loại bỏ vì directory vẫn còn.
