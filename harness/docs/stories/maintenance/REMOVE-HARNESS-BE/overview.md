# Overview — Remove `harness-be`

## Current Behavior

Project root có hai cây dễ nhầm: Harness chính `harness/` và nested repository bị ignore `harness-be/`.

## Target Behavior

Chỉ còn `harness/`. `harness-be/` không tồn tại và `.gitignore` không còn rule dành riêng cho nó.

## Affected Users

- Human và agent làm việc trong `/var/www/vltk-mobile`.

## Affected Product Docs

- Không đổi product contract.

## Non-Goals

- Không chuyển code từ `harness-be` sang backend hiện tại.
- Không thay đổi runtime game.
