# Inventory PC Panel — Asset Provenance

Source of truth: PC `UiItem` sheet, decoded from `05ea8560.dat`.

## Decision

Use real PC Hành Trang panel + buttons. No recreated placeholder art.

- Config UID: `05ea8560`
- Winning config package: `update03.pak`
- Panel size: `214×454`
- Code consumer: `Assets/Scripts/UI/Inventory/InventoryContent.cs`
- USS consumer: `Assets/UI/Popup/Inventory/Inventory.uss`

Unity overlays only 6×10 item cells, live count text, and transparent hit zones.

## package.ini priority

Relevant PC `bin/client/package.ini` rows:

```text
20=update03.pak
21=update01.pak
23=spr.pak
```

Button/panel SPRs below exist in `update01.pak` and `spr.pak`; `update01.pak` wins. Extra `dmjx01/eurofun` copies are not listed by this runtime `package.ini`, so not winners.

## Vendored PNGs

| Unity asset | Logical PC path | UID | Winning package | Frame | Size | SHA-256 |
| --- | --- | --- | --- | ---: | --- | --- |
| `panel.png` | `\spr\Ui3\道具\道具面板3.spr` | `312b30c9` | `update01` | 0 | `214×454` | `d7882967a038b5fb466eb61aceb1163bda0606f2062e339bc2b3b9482650cfe1` |
| `btn_close_up.png` | `\spr\Ui3\道具\道具－关闭.spr` | `03fb698d` | `update01` | 0 | `65×28` | `3400888c3aa1cca6acc89bb88f796706edb945870d9f726bb8a7b2338a9ac131` |
| `btn_close_down.png` | `\spr\Ui3\道具\道具－关闭.spr` | `03fb698d` | `update01` | 1 | `65×28` | `bcaad83c6bcd09f8fd3b6f83402b4e749e7c6dde385b5750caad1db63a138b0e` |
| `btn_close_over.png` | `\spr\Ui3\道具\道具－关闭.spr` | `03fb698d` | `update01` | 2 | `65×28` | `8790d7a46ae9c62c1a58f6c038d705ba008fd4d2ce9d9e3037ac35b968777d9d` |
| `btn_money_up.png` | `\spr\Ui3\道具\道具－存钱.spr` | `d00e7b1d` | `update01` | 0 | `64×28` | `04a1ab5d6148e934a2bfbb6e38e6071fb050bbf7aa359cbb8fee456806519dd4` |
| `btn_money_down.png` | `\spr\Ui3\道具\道具－存钱.spr` | `d00e7b1d` | `update01` | 1 | `64×28` | `169aab24f719ca23c4ef4a40c3a12aac5fff8b986e0e0394ac4b7987d5db9386` |
| `btn_money_over.png` | `\spr\Ui3\道具\道具－存钱.spr` | `d00e7b1d` | `update01` | 2 | `64×28` | `616b3ee40301ce64bc102b7b1ad8a8c53fa9e3219a03162066ad9db227cde743` |
| `btn_status_up.png` | `\spr\Ui3\道具\道具－装备.spr` | `8bc8706b` | `update01` | 0 | `72×28` | `fe6218deb10810dcbd64b2ca03bec1fa74d88c4cfccc2ddebc1be623007d27f2` |
| `btn_status_down.png` | `\spr\Ui3\道具\道具－装备.spr` | `8bc8706b` | `update01` | 1 | `72×28` | `a8e442b13e8159767200a9fe1110fe3cd3689e8cb19869f57ecea660683561e2` |
| `btn_status_over.png` | `\spr\Ui3\道具\道具－装备.spr` | `8bc8706b` | `update01` | 2 | `72×28` | `893dd511817ee999ff26d7aff64100d5d1b9f111ea0dfd1e88bf26df58848a21` |
| `btn_make_adv_up.png` | `\spr\Ui3\道具\道具－摆摊广告.spr` | `72378328` | `update01` | 0 | `64×20` | `2f93a1d0cf50136af9db50eb532eb4382bf54fa6e8791237eff28faab8ee24c9` |
| `btn_make_adv_down.png` | `\spr\Ui3\道具\道具－摆摊广告.spr` | `72378328` | `update01` | 1 | `64×20` | `1e7c8a0157a58ea9019111f33f3bc463da48959c29c37a0b6d80e8ff68861f91` |
| `btn_make_adv_over.png` | `\spr\Ui3\道具\道具－摆摊广告.spr` | `72378328` | `update01` | 2 | `64×20` | `cf79bbb6ce519a682c9f12bfdd438a7bb8a98a0d424b480b0908469eaaad84c1` |
| `btn_mark_price_up.png` | `\spr\Ui3\道具\道具－摆摊标价.spr` | `cea0ceea` | `update01` | 0 | `64×20` | `16b1cd5a054c8c931caa1967c33773f77814402c2960000bc7c00e7a475713dc` |
| `btn_mark_price_down.png` | `\spr\Ui3\道具\道具－摆摊标价.spr` | `cea0ceea` | `update01` | 1 | `64×20` | `81ceb291ab3465038c148b30f0bf4af185a8f4a9dce91c164c272fe6cde96ac9` |
| `btn_mark_price_over.png` | `\spr\Ui3\道具\道具－摆摊标价.spr` | `cea0ceea` | `update01` | 2 | `64×20` | `47454b07e38331c40b2378db43ef0093a1bf449fc87c41b13b1a682f71158e65` |
| `btn_make_stall_up.png` | `\spr\Ui3\道具\道具－摆摊开关.spr` | `cd3a967b` | `update01` | 0 | `64×20` | `c639cef1a32ca710c77e91b67b436e705a363bde1f8ddfd50d79f91d1de78308` |
| `btn_make_stall_down.png` | `\spr\Ui3\道具\道具－摆摊开关.spr` | `cd3a967b` | `update01` | 1 | `64×20` | `c90f1f27e498d5604242ab98d069a9a4cd8d4fd218f4eb17f816b6fda55d126c` |

## Extraction commands

```bash
python3 ~/Projects/vltktool/decode_ui_ini.py --file /var/www/jx-source/pak_unpacked/update03/unknown/05ea8560.dat
python3 ~/Projects/vltktool/extract_item_spr.py --file /var/www/jx-source/pak_unpacked/update01/unknown/312b30c9.spr --out-root /tmp/vltk-inventory-spr
python3 ~/Projects/vltktool/extract_item_spr.py --file /var/www/jx-source/pak_unpacked/update01/unknown/03fb698d.spr --out-root /tmp/vltk-inventory-spr
python3 ~/Projects/vltktool/extract_item_spr.py --file /var/www/jx-source/pak_unpacked/update01/unknown/d00e7b1d.spr --out-root /tmp/vltk-inventory-spr
python3 ~/Projects/vltktool/extract_item_spr.py --file /var/www/jx-source/pak_unpacked/update01/unknown/8bc8706b.spr --out-root /tmp/vltk-inventory-spr
python3 ~/Projects/vltktool/extract_item_spr.py --file /var/www/jx-source/pak_unpacked/update01/unknown/72378328.spr --out-root /tmp/vltk-inventory-spr
python3 ~/Projects/vltktool/extract_item_spr.py --file /var/www/jx-source/pak_unpacked/update01/unknown/cea0ceea.spr --out-root /tmp/vltk-inventory-spr
python3 ~/Projects/vltktool/extract_item_spr.py --file /var/www/jx-source/pak_unpacked/update01/unknown/cd3a967b.spr --out-root /tmp/vltk-inventory-spr
```

## Coordinate source

Decoded `05ea8560.dat` sections wired in code/USS:

- `[Main]`: `Width=214`, `Height=454`, `Image=\spr\Ui3\道具\道具面板3.spr`
- `[ItemBox]`: `Left=24`, `Top=72`, `Width=168`, `Height=280`, `HUnits=6`, `VUnits=10`, `UnitBorder=1`
- `[Money]`: `Left=53`, `Top=353`, `Width=138`, `Height=14`
- `[MakeAdvBtn]`: `6,370,64,20`
- `[MarkPriceBtn]`: `75,370,64,20`
- `[MakeStallBtn]`: `144,370,64,20`
- `[GetMoneyBtn]`: `7,394,64,28`
- `[OpenStatus]`: `70,394,72,28`
- `[CloseBtn]`: `142,394,65,28`

## Deferred honestly

- Money backend unavailable. Current overlay shows `used/capacity`, not fabricated PC money.
- Stall/price/deposit backend unavailable. Buttons use exact art, input disabled.
