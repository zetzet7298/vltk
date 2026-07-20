# Exec Plan

## Goal

Tạo canonical learned-membership classification và deterministic static oracle cho
Côn Luân, đủ để phát hiện catalog gap trước khi sửa production/runtime.

## Scope

In scope:

- 24 canonical PC-learned IDs, 18 observed Unity display IDs, union 29.
- Static rows, populated field mapping và direct relationship closure.
- Generator/tests/frozen fixtures; catalog diff và minimum follow-up classification.

Out of scope:

- Runtime formula/timing/projectile/visual/audio parity.
- Android packaged/device proof và PC runtime golden.
- UI slot/deck order.

## Risk Classification

Risk flags:

- Public contracts.
- Cross-platform.
- Existing behavior.
- Weak proof.

Hard gates:

- Encoded PAK-derived tables chỉ qua `vltktool`.
- Không giảm validation hoặc dùng Unity-derived expected values.

## Work Phases

1. [completed] Inventory chọn Côn Luân bằng verified risk-first ranking.
2. [completed] Freeze membership classification và exact source evidence.
3. [completed] Generate learned static oracle + 17-target relationship closure (3 learned overlaps + 14 support-only).
4. [completed] Add 21 independent Python generator tests và independent static-proof audit.
5. [completed] Diff và sửa bounded Unity catalog/selection behavior theo frozen oracle.
6. [completed] Compile và chạy focused Unity/EditMode proof: 138/138 pass.
7. [completed] Independent proof-auditor GO, validation report và Harness trace.

## Stop Conditions

Pause for human confirmation if:

- Client/server membership evidence mâu thuẫn.
- Canonical source hash/encoding drift không resolve được bằng `vltktool`.
- Behavior cần chọn ngoài PC evidence.
- Validation phải yếu đi hoặc runtime architecture direction đổi.
