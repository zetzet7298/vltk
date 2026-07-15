# Evidence Register

| Trường | Giá trị |
|---|---|
| Mục đích | Danh mục evidence có thể audit và truy ngược |
| Trạng thái | `provisional` |
| Owner / reviewer | Evidence owner / technical reviewer |
| Cập nhật | 2026-07-15 |

## Evidence hiện có

| ID | Location tuyệt đối | Nội dung được phép kết luận | Giới hạn |
|---|---|---|---|
| `E-DHCD-MAP` | `/home/zet/Projects/dhcd/docs/gameplay-evidence-map.md:14-26` | lifecycle, wave, spawn, monster data, hit, drop, card/reroll, actor concepts có declaration/recovered location | Không chứng minh original source, portrait, exact balance hay server |
| `E-DHCD-MAP-GAPS` | `/home/zet/Projects/dhcd/docs/gameplay-evidence-map.md:28-55` | các malformed methods và vai trò từng corpus layer | Không được suy diễn behavior bị thiếu |
| `E-DHCD-METHODOLOGY` | `/home/zet/Projects/dhcd/docs/methodology.md:1-16` | phương pháp reverse và giới hạn IL2CPP | Không chứng minh author intent/scene hierarchy |
| `E-DHCD-SERVER-DECISION` | `/home/zet/Projects/dhcd/docs/server-reverse-decision.md:1-35` | backend target là contract mới; không cần DHCD wire compatibility | Không phải thiết kế API hoàn chỉnh |
| `E-DHCD-APK` | `/var/www/dhcd/localization_vi/output/apktool_clean_from_full/lib/arm64-v8a/libil2cpp.so` | input binary đã hash trong docs | Chỉ là artifact input, không tự chứng minh behavior |
| `E-DHCD-METADATA` | `/var/www/dhcd/localization_vi/output/apktool_clean_from_full/assets/bin/Data/Managed/Metadata/global-metadata.dat` | metadata matching đã hash trong docs | Cần matching binary khi reverse lại |
| `E-JX-PAK` | `/var/www/jx-source/pak_unpacked/` | candidate runtime assets/config | Mỗi file cần manifest resolver riêng |
| `E-JX-LEGACY` | `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/` | behavior/config source evidence | Không tự động là PAK winner |
| `E-JX-TOOL` | `/home/zet/Projects/vltktool/` | resolver/decode/hash evidence | Command và version phải ghi theo asset |
| `E-UNITY` | `/var/www/vltk-mobile` | code reuse/migration surface | Không phải source of truth cho JX parity |
| `E-HARNESS-DB` | `/var/www/vltk-mobile/scripts/schema/001-init.sql` | harness SQLite metadata tables nếu cần test tooling | Không phải game PostgreSQL schema |
| `E-TT-ENV` | `/var/www/tt-docker/.env` | runtime configuration location | Không chép secret vào docs hoặc repo |
| `E-PORT-BRIEF` | `/home/zet/.codex/attachments/19ad23fc-a9f3-4549-9e80-41e12e77df01/pasted-text-1.txt` | target sản phẩm/kỹ thuật, cấu trúc docs và priority do user cung cấp | Không chứng minh runtime behavior hoặc legal clearance |

Runtime check 2026-07-15: `docker inspect postgres --format ...` reported `/postgres`, `running`, `healthy`, working directory `/var/www/tt-docker` and Compose files `docker-compose.infra.yml` plus `docker-compose.infra.dev.yml`. This proves availability only, not a game database/role.

## Snapshot hashes

| Artifact | Bytes | SHA-256 |
|---|---:|---|
| `/home/zet/Projects/dhcd/docs/gameplay-evidence-map.md` | 3975 | `fe92e27172f5d9895f6c1880c2d2be9455e1a88d759fc4c4876c661ae93dbc4f` |
| `/home/zet/Projects/dhcd/docs/methodology.md` | 718 | `4f73995241a92579def4a25f5be51ce6e7799263e43dd2e8b0369c680a071b7d` |
| `/home/zet/Projects/dhcd/docs/server-reverse-decision.md` | 1354 | `f0c456c43adf8456534db3f90e4516770a3f371ed5c7c4e5683a4258a0341ef1` |
| `/var/www/vltk-mobile/scripts/schema/001-init.sql` | 5651 | `c7d3f13ae6849aacdcee7538b5f6a159f237ff5676d0df261855a874dabb96b3` |
| `/home/zet/.codex/attachments/19ad23fc-a9f3-4549-9e80-41e12e77df01/pasted-text-1.txt` | 4643 | `1378e2a1442d224fc1a0c884c0418bd11e6dd1323fab7ec9d56a5d2538375e4a` |

Directory sources (`/var/www/jx-source`, `/var/www/vltk-mobile`, `/home/zet/Projects/dhcd`) require per-selected-file manifest/hash; a directory path alone is not an asset provenance record. Pure to-be/design records may use `design-only`; `[CẦN XÁC NHẬN]` is allowed only while status is `not_started`, `blocked` or `provisional`, and can never satisfy `verified`.

## Card bắt buộc cho evidence mới

Dùng [evidence-card](../templates/evidence-card.md). `absolute path`, `byte count`, `SHA-256`, `confidence`, `owner` là bắt buộc cho mọi evidence. `pack/version`, `load-order`, `Hash_UID`, `encoding`, `path_bytes_hex`, resolver/decode và `name_vi` chỉ bắt buộc cho PAK/hashed asset; source code, document và runtime test dùng loại provenance tương ứng, không bịa UID.

## Quy tắc confidence

- `verified`: artifact + location + hash + test/golden.
- `documented`: tài liệu có claim rõ nhưng chưa có runtime test.
- `provisional`: mapping hoặc reverse result một phần.
- `unresolved`: thiếu evidence hoặc mâu thuẫn.

## Ghi nhận provenance

Không lưu secret, token, dữ liệu cá nhân hay toàn bộ asset binary trong docs. Manifest repo-local chỉ trỏ tới selected bytes và hash; vendor asset theo policy ở [assets](../05-jx-parity/assets.md).

## Acceptance

- [ ] Evidence ID/path/confidence/hash fields parse và trỏ tới artifact thực.
- [ ] PAK fields chỉ bắt buộc cho đúng evidence kind.
- [ ] Claim critical có giới hạn và owner/reviewer.
