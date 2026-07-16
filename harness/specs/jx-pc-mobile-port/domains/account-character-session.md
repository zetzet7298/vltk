# Domain: Tài khoản, nhân vật và phiên

## Định danh và phạm vi

- Domain ID: `DOM-ACS`; DRI: Backend & Data; reviewer: Security/QA; phase sâu: P1.
- Sở hữu REST đăng ký/login/refresh/logout/reset, realm, character create/delete/restore, bootstrap; WSS hello/resume/logout/checkpoint.
- Không sở hữu combat/world rule. Không dùng PaySys, launcher, GM/backoffice hay dữ liệu mock làm production.

## Bằng chứng as-is

- `EVID-0017`: `KPlayer.cpp:742-934` có save/login timeout/quit và sync; đây là evidence tĩnh, không chứng minh protocol runtime.
- `EVID-0018`: `KPlayerDBFuns.cpp:34-39` có add/load DB player; `:740-854` lưu base gồm account/name, thuộc tính, phái, level, map/tọa độ, PK.
- `EVID-0019`: `KPlayerDBFuns.cpp:858-975` lưu item, fight/state skill và task. Định dạng legacy không phải schema PostgreSQL mục tiêu.

## Aggregate và invariant

| Aggregate | Stable ID | Invariant |
| --- | --- | --- |
| Account | `FR-AUTH-001/002` | Credential không vào event/log; refresh rotation; actor chỉ truy cập character thuộc account. |
| Character | `FR-CHAR-001/002` | Tối đa 3 active; soft-delete 7 ngày, giải phóng slot ngay, giữ tên; restore chỉ khi còn slot; create là một transaction đầy đủ. |
| Session | `FR-SESS-001` | Một active gameplay session/account; thiết bị mới thay session cũ; resume grace 30 giây không nhân đôi command. |

## Contract mục tiêu

- Commands: `Register`, `Login`, `Refresh`, `Logout`, `ResetPassword`, `CreateCharacter`, `DeleteCharacter`, `RestoreCharacter`, `Bootstrap`, `Hello`, `Resume`, `Checkpoint`.
- Events: `SessionAccepted`, `SessionRejected`, `CharacterCreated`, `CharacterSoftDeleted`, `CharacterRestored`, `SnapshotIssued`, `ResumeCompleted`, `ResumeExpired`.
- Mọi mutation REST có `request_id`; WSS có `command_id`, sequence và revision. Transport ACK không phải business success.
- State: `Anonymous -> Authenticated -> CharacterSelected -> Connecting -> InWorld -> Reconnecting/Authenticated` như `02-mo-hinh-yeu-cau.md`.

## Nghiệm thu

- `TEST-ACS-001` P1: retry create/delete/restore không nhân đôi; ownership denial ổn định.
- `TEST-ACS-002` P1: disconnect ở trước/sau checkpoint đều resume hoặc full snapshot mà state bằng commit cuối.
- `TEST-ACS-003` P1: persistence round-trip base/item/skill/task; source chỉ cho field coverage, parity byte/runtime là `BLOCKED`.
