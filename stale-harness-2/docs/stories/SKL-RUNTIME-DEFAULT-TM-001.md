# Đường Môn là faction mặc định khi Sandbox khởi động

## Status

in_progress

## Lane

normal — thay đổi existing user-visible Sandbox boot behavior và cần proof runtime/test.

## Product Contract

Fresh Stop/Play tạo player Đường Môn trong cả progression và gameplay combat state.

## Relevant Product Docs

- `harness/docs/product/sandbox-runtime.md`

## Acceptance Criteria

- Fresh `PlayerProgression` bootstrap dùng Đường Môn.
- `GameplayLoop.RegisterPlayer` đặt faction Đường Môn và tính mana theo Đường Môn.
- Tên boot player là `Đường Môn Đệ Tử`.
- Chuyển phái runtime vẫn có thể thay thế default trong session.

## Design Notes

Một `GameplayLoopService.DefaultPlayerFaction` là single source of truth cho boot
progression, gameplay faction và mana formula; không thêm cơ chế persistence profile.

## Validation

- EditMode `GameplayLoopTests.RegisterPlayer_CreatesActorWithCorrectStats`.
- Unity compile và fresh Play Mode smoke: player hiển thị Đường Môn sau Stop/Play.

## Harness Delta

Không thay đổi Harness.

## Evidence

- Unity compile: pass.
- EditMode job `2c75b23f1bbd471695271b95c6b0f6fd`: 1/1 pass.
- Fresh Play Mode smoke: `faction=TangMen`, runtime `TangMen`, name
  `Đường Môn Đệ Tử`, 42 known skills, mana 3700.
