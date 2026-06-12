# 0011 — Reverse-engineer công thức sát thương từ jx_linux_y

- **Status:** accepted
- **Date:** 2026-06-12
- **Story:** US-007 (combat) — phần engine-internal

## Bối cảnh

Khi port combat (US-007), công thức sát thương được xác định là engine-internal:
script Lua chỉ gọi `DoAttack(...)` và khai báo thuộc tính magic
(`addphysicsdamage_p`, `fireres_p`, ...), KHÔNG tự tính sát thương. Phần tính
nằm trong binary `jx_linux_y`.

Ban đầu giả định phải đoán/bỏ qua. Nhưng recon binary cho thấy điều kiện lý
tưởng để reverse:

- `jx_linux_y` là **ELF 32-bit, NOT stripped, có đầy đủ DWARF** (`.debug_info`,
  `.debug_line`, `.debug_str`).
- Các hàm sát thương có symbol C++ demangle được, ánh xạ chính xác tới
  `swordonline/gameworld/core/src/KNpc.cpp`.

## Quyết định

Reverse và port công thức thật (không đoán), với provenance số dòng nguồn:

| Hàm engine | Địa chỉ | Nguồn | Vai trò |
|---|---|---|---|
| `KNpc::CalcDamage` | 0x0809e790 | KNpc.cpp:3551 | pipeline chính |
| `KNpc::CalcCurRes` | 0x08090320 | KNpc.cpp:3441 | trần kháng làm mềm |
| `KNpc::CalcPhysicsAttribDamage` | 0x080925f0 | KNpc.cpp:3342 | bổ trợ % |
| `Calc{Cold,Fire,Light,Poison}AttribDamage` | 0x08092xxx | — | cùng khuôn |

Công thức khôi phục (đối chiếu mã máy):

1. **Sát thương cơ bản** (KNpc.cpp:3556-3575):
   `if (min+max)<=0 -> 0; damage = min + g_Random(max-min); if damage<=0 -> 0`
2. **Bổ trợ %** (KNpc.cpp:3384): `(100 + bonus_pct) * base / 100`
   (magic const 0x51eb851f + sar 5 = reciprocal chia 100; literal 0x64=100)
3. **Trần kháng** (KNpc.cpp:3448): `cur<=cap ? cur : cap + (cur-cap)*(95-cap)/400`
   (literal 0x5f=95; reciprocal + sar 7 = chia 400)
4. **Sát thương cuối** = base sau % trừ phòng thủ hiệu dụng, sàn 0.

## Hiện thực

- `app/modules/combat/domain/damage.py`: `roll_base_damage`, `calc_cur_res`,
  `apply_attrib_percent`, `calc_damage` — mỗi hàm chú thích số dòng KNpc.cpp.
- `g_Random(n) -> [0, n)` được mô hình hóa qua `rng` injectable để test tất
  định và đối chiếu parity.
- API: `POST /v1/combat/damage/calc` (có `seed` để tính tất định).
- 13 unit test khẳng định parity từng bước + pipeline.

## Đánh giá parity

- Cấu trúc số học, hằng số (95, 100, 400), thứ tự áp dụng: **khớp mã máy**.
- Tính tất định: seed cố định cho kết quả lặp lại (verified live: 111==111).
- Pipeline tổ hợp: 100 base +50% = 150, kháng 90/cap50 -> trừ 54 = 96 (verified).
- HẠN CHẾ: một số nhánh phụ trong CalcDamage (state-skill-effect loop
  KNpc.cpp:3692-3712, immunity flags, DAMAGE_TYPE switch 0-6) chưa port đầy đủ —
  đây là các modifier theo trạng thái buff/debuff, sẽ port khi làm hệ buff. Lõi
  atk-def-res-crit đã chính xác.

## Hệ quả

- Damage parity đạt mức **lõi chính xác theo binary**, không còn là hộp đen.
- Phương pháp (DWARF + objdump line-annotated + đối chiếu hằng số) tái dùng được
  cho các formula engine-internal khác (exp curve, drop rate, ...).
