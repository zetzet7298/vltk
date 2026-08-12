# Validation — Remove `harness-be`

## Proof Strategy

Chứng minh directory không tồn tại và parent repository không còn tracked reference `harness-be`.

## Test Plan

| Layer | Cases |
| --- | --- |
| Unit | Không áp dụng |
| Integration | Không áp dụng |
| E2E | Không áp dụng |
| Platform | Filesystem path không tồn tại |
| Logs/Audit | Harness intake, story và trace tồn tại |

## Fixtures

Không cần.

## Commands

```bash
test ! -e /var/www/vltk-mobile/harness-be
git -C /var/www/vltk-mobile grep -n -I -F 'harness-be' -- . ':!harness/**' || true
git -C /var/www/vltk-mobile diff --check
```

## Acceptance Evidence

- `test ! -e /var/www/vltk-mobile/harness-be`: pass.
- `story verify REMOVE-HARNESS-BE`: pass.
- Tracked reference ngoài story docs: không còn.
- `git diff --check`: pass.
- Root diff: chỉ bỏ dòng `harness-be/` khỏi `.gitignore`; các thay đổi có sẵn khác không bị chạm.
