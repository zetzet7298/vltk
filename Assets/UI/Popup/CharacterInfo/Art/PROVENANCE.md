# Character Info PC Panel — Asset Provenance

Source of truth for current Unity port: PC combined character equipment + stats
panel shown in the user reference screenshot.

## Decision

Use the real PC combined panel (`TRANG BỊ VÀ THUỘC TÍNH`), not the earlier
placeholder/tabbed USS recreation.

- Config UID: `2711122c`
- Config name/logical meaning: `装备和属性Equip.ini`
- INI sections: `[Male]`, `[Female]`
- Panel size: `428×430`
- Code consumer: `Assets/Scripts/UI/CharacterInfo/CharacterInfoContent.cs`

This panel sprite already bakes in Vietnamese labels, ornate frame, title,
male/female silhouette, equipment slot frames, and footer tabs. Unity overlays
only live values + transparent hit zones.

## package.ini priority

Relevant listed packages from PC `bin/client/package.ini`:

```text
20=update03.pak
21=update01.pak
23=spr.pak
```

For `装备和属性-男/女.spr`, `update03.pak` wins over `update01.pak`/`spr.pak`.
For `状态加点按钮改.spr`, `update01.pak` wins over `spr.pak`.

## Vendored PNGs

| Unity asset | Logical PC path | UID | Winning package | Frame | Size | SHA-256 |
| --- | --- | --- | --- | ---: | --- | --- |
| `panel_male.png` | `\Spr\Ui3\状态与装备\装备和属性-男.spr` | `e3ecbac9` | `update03` | 0 | `428×430` | `e0fe85a22e2e0f358dacec8385c95bddcdeefb18ccefc4c8e89dc240ec5b88fe` |
| `panel_female.png` | `\Spr\Ui3\状态与装备\装备和属性-女.spr` | `6ce319ab` | `update03` | 0 | `428×430` | `f5896f17ecc41a8a3f9e9c6863ac6a63dd5e103382f6e8725832d4eddf46861a` |
| `btn_addpoint_up.png` | `\Spr\Ui3\状态与装备\状态加点按钮改.spr` | `9e87942b` | `update01` | 0 | `14×14` | `b16600026160fec2827796cf56bb267ee95334487f38d6923e6a5c4e108319fb` |
| `btn_addpoint_down.png` | `\Spr\Ui3\状态与装备\状态加点按钮改.spr` | `9e87942b` | `update01` | 1 | `14×14` | `272e6efd0d98c596b4a0ea4f203ced4b720d85ed9dbe7b0c6f2a2bbd4a4ad788` |
| `btn_addpoint_over.png` | `\Spr\Ui3\状态与装备\状态加点按钮改.spr` | `9e87942b` | `update01` | 2 | `14×14` | `39d6798010a992e558256e707b4fc1a26cfabfc0e3d23f0b5392a4f0035bc84b` |

## Extraction commands

```bash
cd ~/Projects/vltktool
python3 extract_item_spr.py --file /var/www/jx-source/pak_unpacked/update03/unknown/e3ecbac9.spr --out-root /tmp/char-spr-extract
python3 extract_item_spr.py --file /var/www/jx-source/pak_unpacked/update03/unknown/6ce319ab.spr --out-root /tmp/char-spr-extract
python3 extract_item_spr.py --file /var/www/jx-source/pak_unpacked/update01/unknown/9e87942b.spr --out-root /tmp/char-spr-extract
```

Then copied exact output files into:

```text
Assets/UI/Popup/CharacterInfo/Art/
```

## Coordinate source

Decoded with:

```bash
cd ~/Projects/vltktool
python3 decode_ui_ini.py --file /var/www/jx-source/pak_unpacked/update03/unknown/2711122c.dat
```

Important sections wired in code:

- Panel: `[Male]` / `[Female]` `428×430`
- Stats: `Name`, `Title`, `Luck`, `Prestige`, `Level`, `WorldRank`, `Life`,
  `Mana`, `Stamina`, `Status`, `Exp`, `Strength`, `Dexterity`, `Vitality`,
  `Energy`, `LeftDamage`, `RightDamage`, `Attack`, `Defense`, `MoveSpeed`,
  `AttackSpeed`, `RemainPoint`, `ResistPhy`, `ResistCold`, `ResistLighting`,
  `ResistFire`, `ResistPoison`, `PKValue`
- Potential buttons: `AddStrength`, `AddDexterity`, `AddVitality`, `AddEnergy`
- Equipment hit-zones: `Cap`, `Weapon`, `Necklace`, `Mask`, `Bangle`, `Cloth`,
  `Sash`, `Ring1`, `Ring2`, `Pendant`, `Shoes`, `Horse`
- Footer/actions: `Item`, `Close`, `BtnLock`, `BtnBind`, `BtnUnBind`

## Deferred honestly

- Item icon rendering inside equipment slots still awaits PC item SPR binding.
  Current Unity overlays equipped/empty tint only; slot frames themselves are
  real PC sprite art.
- `Bangle` has no current sandbox `EquipSlot` enum mapping; hit-zone is present
  but not data-bound.
- `BtnLock`, `BtnBind`, `BtnUnBind` stay disabled until backend exists.
