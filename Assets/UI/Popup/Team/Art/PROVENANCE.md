# Team PC Panel — Asset Provenance

Source of truth: PC team sheet decoded from `a05d7a2c.dat`.

## Decision

Use real PC Đội/Tổ đội panel + buttons. No recreated placeholder art.

- Config UID: `a05d7a2c`
- Winning config package: `update03.pak`
- Panel size: `340×229`
- Code consumer: `Assets/Scripts/UI/Team/TeamContent.cs`
- USS consumer: `Assets/UI/Popup/Team/Team.uss`

Unity overlays only party roster/nearby-list text and transparent hit zones.

## package.ini priority

Relevant PC `bin/client/package.ini` rows:

```text
20=update03.pak
21=update01.pak
23=spr.pak
```

The team SPRs below are not present in `update03.pak`; `update01.pak` wins over `spr.pak`. Extra `dmjx01/eurofun` copies are not listed by this runtime `package.ini`, so not winners.

## Vendored PNGs

| Logical PC path | UID | Winning package | Unity asset(s) | Frames |
| --- | --- | --- | --- | --- |
| `\Spr\Ui3\组队\组队2.spr` | `7d78d2d7` | `update01` | `panel.png` | `0` |
| `\Spr\Ui3\组队\邀请加入.spr` | `1c90fcf9` | `update01` | `btn_invite_{up,down,disabled,over}.png` | `0,1,2,3` |
| `\Spr\Ui3\组队\踢出队伍.spr` | `5b04c0d7` | `update01` | `btn_kick_{up,down,disabled,over}.png` | `0,1,2,3` |
| `\Spr\Ui3\组队\队长移交.spr` | `72aeaccd` | `update01` | `btn_appoint_{up,down,disabled,over}.png` | winner has frame `0` only, reused for all states |
| `\Spr\Ui3\组队\刷新列表.spr` | `83205731` | `update01` | `btn_refresh_{up,down,disabled,over}.png` | `0,1,2,3` |
| `\Spr\Ui3\组队\离开队伍.spr` | `9149d29d` | `update01` | `btn_leave_{up,down,disabled,over}.png` | `0,1,2,3` |
| `\Spr\Ui3\组队\解散队伍.spr` | `580687af` | `update01` | `btn_dismiss_{up,down,disabled,over}.png` | `0,1,2,3` |
| `\Spr\Ui3\组队\组队开关.spr` | `1ed485e1` | `update01` | `btn_close_team_{up,disabled,down}.png` | `0,1,2` |
| `\spr\Ui3\组队\关闭.spr` | `34ac9480` | `update01` | `btn_cancel_{up,down,over}.png` | `0,1,2` |
| `\Spr\Ui3\好友qq\通用拖动条.spr` | `23fe2a10` | `update01` | `scroll_thumb.png` | `0` |

## SHA-256

```text
77da4fb655955a900d5c83d5511ccc08fe147ada0cfa83c4770b89ec02a4d2d3  panel.png
58948cae3110b80ee7996f3f1aa50f63c7fb5099ced48dde7cdc05fa991c5056  btn_invite_up.png
585978b8016b508b66811f50bd62880e34f4377bfd0b27bf4bd101e3bfec0c3f  btn_invite_down.png
f44e4bd29ff690c52fcc207f050ce93028790a490899f6c36a0f78b6906bc6b9  btn_invite_disabled.png
05abd37026cb2b015b8b8a7753fe4e0338ffb0d8c166c3d47024b652f842816b  btn_invite_over.png
e37aafe89f250e5743e28ac428812d58479192a3bec4a1c884afcb7e3cf8d318  btn_kick_up.png
0b31736638f4a21b929e0ab50b809d0d9a2c846d2b297b85ae217af9e45ed366  btn_kick_down.png
47db176380f24cd015c94ef3dc7f451a80bd463f015e22e7cc68702416e95bed  btn_kick_disabled.png
ddc0dde291c90f5e6afc365a6255a5464f5e27014b59f54ee1a264c455d6b66e  btn_kick_over.png
1954f289e4d1fa65aabaf17176e199e37b7cde0350aca5fe859669fae22b70bb  btn_appoint_up.png
1954f289e4d1fa65aabaf17176e199e37b7cde0350aca5fe859669fae22b70bb  btn_appoint_down.png
1954f289e4d1fa65aabaf17176e199e37b7cde0350aca5fe859669fae22b70bb  btn_appoint_disabled.png
1954f289e4d1fa65aabaf17176e199e37b7cde0350aca5fe859669fae22b70bb  btn_appoint_over.png
9def49c611dda0404ae48123885ff85c501dfaae11bec5a8eeb8c3c02713eb30  btn_refresh_up.png
a3fc40107d6e4c930139edc8d917947d1481d2357576309d3608d62b3f28fa65  btn_refresh_down.png
bea37bd93adedcfce35af09daa81177eec944e6068a0187dd7f30196d789b145  btn_refresh_disabled.png
1bf0e8ed6a5c60210567ae0e70beb5d60b96a0ade91ca4d4b5b9847e601dce54  btn_refresh_over.png
cba56106dbd2adb1525342ff8750fdb8fda26c4c4381c949ea9e2fa2fbfe1e8c  btn_leave_up.png
dfef1fc93100869fa15a3d92f9e6c5e126810339b2b9c9bb29e5244bab4f27b0  btn_leave_down.png
01512aa79f7a409810f8e6424c7fcf946336a9cfd05db09936b604459273d95d  btn_leave_disabled.png
a7cea3dca68a94bf8645df337db31787a8c119110f58e4f85e265ab99bf9b606  btn_leave_over.png
36e11d6f66b0755569ed3409b562cea6054c81d388674f875ebb06f78f08e14f  btn_dismiss_up.png
cbce8fb27e7e4f61af7f3278b4166f94edf1acf44607a2d001d8eec83046f68f  btn_dismiss_down.png
c641fe9c919993d42b5b6aae8747fbde9c763954aac2f8b750f4d3516c17c9f7  btn_dismiss_disabled.png
7eb5db66571692f6dbb902e96b8585fc5a23c01a4306f5277b6eb24d7722910d  btn_dismiss_over.png
b72c75407ead88ceaed067b62f26623989027434332413e5ca6f571d668da330  btn_close_team_up.png
e00b75cbf2fd168c1d33a608201bd096c47980e89a066b60c77db68782fe3be4  btn_close_team_disabled.png
13540ff43bc2d5644c50a0084613198408a53c1ca8b5b5b119d4ecfce029552d  btn_close_team_down.png
6dda71f8d1185317736348e609145ee952a6f1b01c959ca92d9bb9155c80084b  btn_cancel_up.png
b4b9f2b70c46f43897a65aaac809311470b2e7773b83d6919aa8cd64145b7a4a  btn_cancel_down.png
69be06a63adecd485b2311a279cb8f59d152b4630a6912fe93cd1ac163dd2fd8  btn_cancel_over.png
aaa26e1c84918851605a35a287a2b8162f08fa7a9f112499de0be53386e13232  scroll_thumb.png
```

## Extraction commands

```bash
python3 ~/Projects/vltktool/decode_ui_ini.py --file /var/www/jx-pc/pak_unpacked/update03/unknown/a05d7a2c.dat
python3 ~/Projects/vltktool/extract_item_spr.py --file /var/www/jx-pc/pak_unpacked/update01/unknown/7d78d2d7.spr --out-root /tmp/vltk-team-spr
python3 ~/Projects/vltktool/extract_item_spr.py --file /var/www/jx-pc/pak_unpacked/update01/unknown/1c90fcf9.spr --out-root /tmp/vltk-team-spr
python3 ~/Projects/vltktool/extract_item_spr.py --file /var/www/jx-pc/pak_unpacked/update01/unknown/5b04c0d7.spr --out-root /tmp/vltk-team-spr
python3 ~/Projects/vltktool/extract_item_spr.py --file /var/www/jx-pc/pak_unpacked/update01/unknown/72aeaccd.spr --out-root /tmp/vltk-team-spr
python3 ~/Projects/vltktool/extract_item_spr.py --file /var/www/jx-pc/pak_unpacked/update01/unknown/83205731.spr --out-root /tmp/vltk-team-spr
python3 ~/Projects/vltktool/extract_item_spr.py --file /var/www/jx-pc/pak_unpacked/update01/unknown/9149d29d.spr --out-root /tmp/vltk-team-spr
python3 ~/Projects/vltktool/extract_item_spr.py --file /var/www/jx-pc/pak_unpacked/update01/unknown/580687af.spr --out-root /tmp/vltk-team-spr
python3 ~/Projects/vltktool/extract_item_spr.py --file /var/www/jx-pc/pak_unpacked/update01/unknown/1ed485e1.spr --out-root /tmp/vltk-team-spr
python3 ~/Projects/vltktool/extract_item_spr.py --file /var/www/jx-pc/pak_unpacked/update01/unknown/34ac9480.spr --out-root /tmp/vltk-team-spr
python3 ~/Projects/vltktool/extract_item_spr.py --file /var/www/jx-pc/pak_unpacked/update01/unknown/23fe2a10.spr --out-root /tmp/vltk-team-spr
```

## Coordinate source

Decoded `a05d7a2c.dat` sections wired in code/USS:

- `[Main]`: `Width=340`, `Height=229`, `Image=\Spr\Ui3\组队\组队2.spr`
- `[MemberList]`: `5,54,125,112`
- `[NearbyList]`: `210,54,110,112`
- `[NearbyScroll]`: `323,54,13,110`; `[NearbyScroll_Btn]`: `13×27`
- `[LeaderAbility]`: `55,179,75,13`
- `[InputEdit]`: `210,179,126,13`
- `[Invite]`: `140,58,60,16`
- `[Kick]`: `140,78,60,16`
- `[Appoint]`: `140,98,60,16`
- `[Refresh]`: `140,118,60,16`
- `[Leave]` / `[Dismiss]`: `140,138,60,16`
- `[CloseTeam]`: `4,207,173,18`
- `[Cancel]`: `177,207,159,18`

## Deferred honestly

- Invite/kick/appoint/refresh/leave/dismiss/close-team backends are not wired yet. Buttons use exact PC art but input is disabled except the `Cancel` close button.
- Nearby-player runtime list is not available. The nearby list lane displays a safe placeholder rather than fabricated players.
