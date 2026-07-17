# Exec Plan

## Goal

Chứng minh root cause và sửa bộ skill Cái Bang hiện tại theo canonical PC source, với test Unity nhỏ nhất bắt regression.

## Scope

In scope:

- Player-facing skill Cái Bang hiện được Unity catalog expose.
- PC `gaibang.lua`, skill config/source liên quan, child/collide/state relations.
- Unity parser, combat definition, catalog factory, runtime và EditMode parity tests trực tiếp liên quan.

Out of scope:

- Phái khác.
- Asset/audio parity không có PC runtime oracle.
- Server mới hoặc persistence skill.

## Risk Classification

Risk flags:

- Existing behavior.
- Weak proof.
- Public/user-visible combat behavior.

Hard gates:

- Không đoán PAK/hash/encoding; dùng `vltktool` nếu chạm encoded content.
- Không sửa `/var/www/jx-source`.
- Không claim runtime parity vượt quá evidence.
- Một writer cho cùng ownership boundary.

## Work Phases

1. [x] Scout PC authority và lập bảng skill/effect/formula.
2. [x] Scout Unity flow và tìm root cause/test gap.
3. [x] Root đối chiếu evidence, chọn diff tối thiểu.
4. [x] Sửa shared factory/runtime/UI và test.
5. [x] Chạy 136-test verifier, Herdr review, sửa toàn bộ finding high-confidence.
6. [x] Ghi Harness proof và residual platform/runtime-golden gaps.

## Stop Conditions

Pause for human confirmation if:

- Canonical client/server source mâu thuẫn chưa resolve.
- Sửa cần đổi product design thay vì khôi phục PC behavior.
- Validation phải yếu đi.
- Cần asset/config chưa thể resolve bằng `vltktool`.
