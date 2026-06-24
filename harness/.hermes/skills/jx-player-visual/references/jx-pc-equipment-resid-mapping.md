# JX PC Equipment → ResID → SPR Variant Mapping (verified)

**Provenance.** This table is the verified mapping used in the static HTML gallery
(`/var/www/vltk-mobile/html/generate_static_gallery.py`). It was rebuilt by reading
the canonical PC source on `/var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/settings/`:

- `item/004/armor.txt` — `Particular` × `Level` × `ResID` × tên áo
- `item/004/helm.txt`  — `Particular` × `Level` × `ResID` × tên mũ
- `npcres/ÄÐÖ÷½ÇÇûÌå.txt` (Male Body) — `ResID` → `MA_BD_<var>_ST01.spr`
- `npcres/Å®Ö÷½ÇÇûÌå.txt` (Female Body) — `ResID` → `FM_BD_<var>_ST01.spr`
- `npcres/ÄÐÖ÷½ÇÍ·²¿.txt` (Male Head), `npcres/ÄÐÖ÷½Ç·¢ÐÍ.txt` (Male Hair)
- `npcres/Å®Ö÷½Ç×óÊÖ.txt` / `ÓÒÊÖ.txt` (Male L/R hands)
- `npcres/Å®Ö÷½Ç·¢ÐÍ.txt`, `Å®Ö÷½Ç×óÊÖ.txt`, `Å®Ö÷½ÇÓÒÊÖ.txt`, `Å®Ö÷½ÇÍ·²¿.txt`

Do NOT trust a name prefix in `armor.txt` for the visual variant — the name says
"Sa Di phục" / "Cổn y" / "Lan Bố y" / "Thô Bố trường bào" / "Sa Ni phục" / "áo vải
thô" but they all share the same `ResID=22` and therefore the same body variant.
PC JX draws the visual difference through palette/paint overlay, not a separate SPR.

## Particular → môn phái → Ngũ Hành (canonical table)

| Particular | Môn phái (Nam)            | ResID_armor | ResID_helm | Hệ   | Visual variant (BD) |
|------------|---------------------------|-------------|------------|------|---------------------|
| 0          | Thiếu Lâm                 | 22          | 28         | Kim  | 001                 |
| 1          | Võ Đang / Côn Lôn         | 24          | 29         | Thổ  | 003                 |
| 2          | Đường Môn                 | 22          | 28         | Mộc  | 001                 |
| 3          | Ngũ Độc                   | 22          | 28         | Mộc  | 001                 |
| 4          | Thiên Vương               | 23          | 29         | Kim  | 002                 |
| 5          | Cái Bang                  | 22          | 28         | Hỏa  | 001                 |
| 6          | Tân Thủ (chưa nhập phái)  | 22          | 28         | —    | 001                 |
| 7          | Nga My nam (Sa Ni)        | 22          | 28         | Thủy | 001                 |
| 8          | Võ Đang nữ                | 24          | 30         | Thổ  | 003                 |
| 9          | Đường Môn nữ (Thục Cẩm)   | 24          | 28         | Mộc  | 003                 |
| 10         | Nga My nữ (Cẩm Sam)       | 24          | 28         | Thủy | 003                 |
| 11         | Thiên Vương nữ (Khảm Thiết)| 25         | 28         | Kim  | 004                 |
| 12         | Thúy Yên nữ (Phá Ma)      | 24          | 28         | Thủy | 003                 |
| 13         | Cái Bang nữ / Thiên Nhẫn (Bạch Điệp) | 24 | 28 | Hỏa | 003 |
| 14-27      | DUPLICATES của 0-13 — cùng SPR, chỉ khác item name/level/stats | | | | same as 0-13 |
| 28         | Phong Trang (event)        | 41          | (none)     | —    | 020                 |

**Key point: 6 cái tên khác nhau (Sa Di, Cổn y, Lan Bố, Thô Bố, Sa Ni, áo vải thô)
đều dùng `ResID_armor=22` → cùng `MA_BD_001_ST01.spr`. Trong PC, sự khác biệt được vẽ
bằng palette/paint overlay, không phải bằng SPR riêng.**

## ResID → SPR variant (Male + Female)

Both `npcres/ÄÐÖ÷½ÇÇûÌå.txt` (male) and `npcres/Å®Ö÷½ÇÇûÌå.txt` (female) share the SAME
ResID → variant mapping for body (verified by parsing both files). The variant number
in the SPR filename is what changes per ResID:

| ResID | Variant | Same ResID = same body shape (paint differs in-game) |
|-------|---------|---|
| 1-21  | 001-021 | original 21 distinct body silhouettes |
| 22-42 | 001-021 | **WRAP** — ResID 22 → variant 001, ResID 23 → 002, ..., ResID 42 → 021 |
| 43-46 | `MA_LF_00_PST.spr` etc. | "lying down / dead" poses |
| 47+   | mixed (e.g. 47→015, 48→025) | leftovers / special poses |

The wrap pattern at 22 means: **when the script picks a body visual for any item in
Particular 0-13 (P=0..13, all using ResID 22-25), it should use the variant derived
from `(ResID - 21)`, not the raw ResID number.** Concretely:
- `ResID_armor=22` → `MA_BD_001_ST01.spr` (NOT `MA_BD_022_ST01.spr` which doesn't exist)
- `ResID_armor=23` → `MA_BD_002_ST01.spr`
- `ResID_armor=24` → `MA_BD_003_ST01.spr`
- `ResID_armor=25` → `MA_BD_004_ST01.spr`

## Helm (head) ResID → SPR variant

`npcres/<gender>Í·²¿.txt` is a 1-indexed list (row 1 = ResID 1, row 2 = ResID 2, ...).
Same wrap pattern: ResID 28 → variant 028 directly, but ResID 29 may not exist for
female (verified: `Å®Ö÷½ÇÍ·²¿.txt` row 29 has empty FN).

| ResID | Male FN                      | Female FN                  |
|-------|------------------------------|----------------------------|
| 28    | MA_HD_028_ST01.spr           | FM_HD_028_ST01.spr         |
| 29    | MA_HD_029_ST01.spr           | (EMPTY — no SPR file)      |
| 30    | MA_HD_030_ST01.spr           | FM_HD_030_ST01.spr         |
| 31    | (EMPTY — no SPR file)        | FM_HD_031_ST01.spr         |
| 32    | MA_HD_032_ST01.spr           | FM_HD_032_ST01.spr         |
| 35    | MA_HD_035_ST01.spr           | FM_HD_035_ST01.spr         |

**Implication: any set that needs ResID_helm=29 for a female or ResID_helm=31 for a male
cannot render. PC's hard cap — accept the gap, do not invent a fallback SPR.**

## Hands (LH/RH) and Hair (HR)

Hands use the same ResID as the armor (P-driven), and exist for all P=0-13. So
`ResID=22` → `MA_LH_022_ST01.spr` and `MA_RH_022_ST01.spr` both exist. Hair uses
`ResID_helmet` (same row as helm).

`extract_composite_character_v2` (Python, in `generate_static_gallery.py`) reads
`load_npcres_mapping(filename)` → 5 dicts per gender (`body`, `head`, `hair`,
`lh`, `rh`) indexed by 1-based row number, then `get_var(spr_name)` extracts the
variant from `MA_<PART>_<VAR>_ST01.spr`. Hand code (right place to copy):

```python
parts_to_load = []
if hr_var:  # hair — uses ResID of helm
    parts_to_load.append((..., f"spr/npcres/{folder}/{prefix}_hr_{hr_var}_st01.spr"))
if rh_var:
    parts_to_load.append((..., f"spr/npcres/{folder}/{prefix}_rh_{rh_var}_st01.spr"))
parts_to_load.append((..., f"spr/npcres/{folder}/{prefix}_bd_{bd_var}_st01.spr"))
if lh_var:
    parts_to_load.append((..., f"spr/npcres/{folder}/{prefix}_lh_{lh_var}_st01.spr"))
parts_to_load.append((..., f"spr/npcres/{folder}/{prefix}_hd_{hd_var}_st01.spr"))
```

## helm.txt vs armor.txt scope (and the female-helm-share pitfall)

`helm.txt` has Particular rows **0-13 only**. `armor.txt` has Particular rows 0-28.
The missing helm rows (P=14-27) are exactly the rows where armor is gendered female
(Thiếu Lâm nữ P=21, Ngũ Độc nữ P=23, etc.) — the **female characters share the
male helm** of the same sect. Concretely:

- Nữ Thiếu Lâm (P=21, ResID_armor=22) → uses **male** Thiếu Lâm helm (P=0, ResID=28)
- Nữ Ngũ Độc   (P=23, ResID_armor=22) → uses **male** Ngũ Độc helm   (P=3, ResID=28)
- Nữ Võ Đang   (P=22, ResID_armor=24) → uses **male** Võ Đang helm   (P=1, ResID=29)
- Nữ Côn Lôn   (P=22, ResID_armor=24) → same as Võ Đang nữ

When you build a lookup for a female at P≥14, fall back to `helm_part = P - 14`
(male equivalent) unless you have a direct mapping. The static gallery already
encodes this as `female_helm_part` per sect in `normal_sects`.

## goldequip.txt pitfalls (Hoàng Kim sets)

`goldequip.txt` is the canonical gold-set source. Tên bộ ở cột 0, mỗi bộ có tối đa 5
items (DetailType 2=armor, 5=boots, 6=sash, 7=helm, 9=waist). Critical pitfalls:

- **Many bộ chỉ có armor, không có helm** (e.g. Hám Thiên, Kế Nghiệp, Tê Hoàng,
  Phục Ma, Tứ Không, Lăng Nhạc, Sương Tinh, Ma Sát, Hiệp Cốt, Địch Khái, Nhu Tình).
  Use default-helm fallback by `Particular`.
- **A few bộ chỉ có helm, không có armor** (Lôi Khung, Ma Thị, Vô Trần) — these
  cannot be rendered as a complete set; skip or render helm-only.
- **Tên bộ có thể bị trùng prefix** — match by `startswith(prefix)` against the
  known prefix list, NOT by full string equality. Prefix list:
  `Mộng Long, Phục Ma, Tứ Không, Hám Thiên, Kế Nghiệp, Ngự Long, Vô Gian, Vô Yểm,
   U Lung, Minh Ảo, Vô Ma, Vô Trần, Tê Hoàng, Bích Hải, Đồng Cừu, Địch Khái,
   Ma Sát, Ma Thị, Lăng Nhạc, Sương Tinh, Lôi Khung, Vụ Ảo, Định Quốc, An Bang,
   Định Nghiệp, Kim Phong, Thiên Hoàng, Hiệp Cốt, Nhu Tình, Chúc Dung, Thần Nông,
   Phục Hi, Nữ Oa, Toại Nhân, Thanh Câu, Vân Lộc, Thương Lang, Huyền Viên,
   Tử Mãng, Kim Ô, Xích Lân, Minh Phượng, Đằng Long, Hắc Thần, Bạch Hổ, Vũ Liệt,
   Long Tương`.
- **Particular of the armor IS the sect**. The earlier `gold_set_sects` table that
  hard-coded sect by prefix was redundant and drift-prone — auto-derive the sect
  from the Particular column in `goldequip.txt`. `P=0` → Thiếu Lâm, `P=4` →
  Thiên Vương, `P=2` → Đường Môn, `P=3` → Ngũ Độc, `P=7` → Nga My nam, `P=10`
  → Nga My nữ, `P=12` → Thúy Yên, `P=5` → Cái Bang nam, `P=13` → Cái Bang nữ /
  Thiên Nhẫn, `P=1` → Võ Đang, `P=8` → Võ Đang nữ, `P=15` → Võ Đang (gold
  duplicate), `P=16` → Đường Môn (gold duplicate), `P=18` → Thiên Vương (gold
  duplicate), `P=20` → Tân Thủ.

## Tân Thủ (Novice) is Particular 6, NOT "no class"

Tân Thủ uses the SAME visual as P=0/2/3/5/7 (ResID=22 → variant 001) with helm
`Bố Cân` (ResID=28). This is correct and matches PC: a fresh character wears
áo vải thô + bố cân, identical silhouette to a Thiếu Lâm monk. In-game they are
distinguished only by paint and equipment. If your static page shows "Tân Thủ"
with the same image as "Thiếu Lâm", that is faithful to PC — do not invent a
distinct sprite.

## Reference for "audit, don't fabricate" workflow

When a future session is asked to "render character wearing X armor and Y helm",
the correct sequence is:

1. Read `armor.txt` row for that Particular/Level → confirm ResID_armor and tên áo.
2. Read `helm.txt` row for that Particular/Level → confirm ResID_helm and tên mũ.
3. Map ResID → variant using the table above (wrap pattern: `variant = (ResID-21)
   if ResID >= 22 else ResID` for body).
4. If the SPR file is missing in `npcres/<gender>*/.txt` (e.g. ResID=29 female,
   ResID=31 male) → report the gap honestly, do not invent a substitute.
5. Composite bd/lh/rh from `body` mapping (ResID_armor) and hd/hr from `head`/
   `hair` mapping (ResID_helm). Do not assume head/body share a variant.
6. Verify the output PNG by visual inspection if the user is going to ship the
   gallery publicly.

If a Particular has no helm row in `helm.txt` (P=14-27), use the male-equivalent
helm (P=14-21 → P=0, P=22 → P=1, P=23 → P=3, etc.) — see "helm.txt vs armor.txt
scope" above.
