# JX port-skill audit playbook (verify by execution, not by prose)

Use when asked to review/audit the JX port skills, or before trusting any of them for a
high-stakes port. The governing principle: **a skill's prose is a claim, not proof.** Docstrings,
"already implements", and baked counts drift. Verify with the live system. Every bug found in the
2026-06-12 audit was a case of code/data/path contradicting the prose around it.

## The five checks (run all; each is fast)

### 1. Empty-section scan (structural rot)
Guard-block inserts can clobber a skill's intro and leave a heading with no body. Detect:

```python
import re
for s in ["jx-pc-port-rule","jx-map-port","jx-enemy-port","jx-player-visual","jx-hud-port","jx-skill-ui-port"]:
    lines=open(f".hermes/skills/.../{s}/SKILL.md",encoding="utf-8").read().split("\n")
    for i,l in enumerate(lines):
        if re.match(r'^##\s',l):
            j=i+1; body=False
            while j<len(lines) and not re.match(r'^##\s',lines[j]):
                if lines[j].strip(): body=True; break
                j+=1
            if not body: print(f"{s}: EMPTY -> {l.strip()}")
```
Fix: refill the intro with a concise, correct summary of that section's topic.

### 2. UID hash evidence pair (the signed-byte trap)
Any uid/hash helper that *claims* signed-byte must prove it. Canonical pair:
- `\spr\Ui\技能图标\icon_sk_ty_at.spr` (GB2312) → **signed `c4454165`**, **unsigned `bedc5b69`**.
- ASCII control: `spr\npcres\man\MA_BD_019_ST01.spr` → `45488ea8` (signed==unsigned).

Run the helper against both. If the CJK path returns `bedc5b69`, the helper is unsigned despite
its docstring → it will miss real `unknown/<uid>.spr` CJK assets. The correct loop is
`c = b-256 if b>=128 else b` applied BEFORE the A-Z lowercase step, then
`value = ((value + idx*c) % 0x8000000B) * 0xFFFFFFEF & 0xFFFFFFFF`, final `value ^ 0x12345678`.
Ground truth lives in C# `SprRuntimeService.ComputePathUid(..., signedBytes:true)` (default SIGNED;
`ResolveSpr` tries uidFromPath → signed → unsigned).

### 3. Manifest read (counts drift)
Never cite a baked "N/M, X failed" number. Read the live manifest:
`/var/www/vltksource_new/vl_update_27/pak_unpacked/_unpack_summary.json` →
`total_exported`, `total_failed`, `partial`, and per-item `dmjx01.pak`. Cross-check `.spr` on
disk if a count looks off. The 2026-06-11 23:25 run = 46 paks, 403560/403560, 0 failed, 0 partial,
dmjx01 ok 1621/1621. AGENTS.md / older audit files / memory may lag this — manifest wins.

### 4. Dead-path stat (out-of-scope / nonexistent trees)
`os.path.exists` every hardcoded source path in scripts and SKILL.md. Known offenders that were
dead: `/var/www/vltk-mobile/jxwin-kinnox/.../Utility/Run/spr/Ui3`,
`/var/www/vhst/survivors/external-data/vltklinux/data/spr.pak`. Real PC source is always under
`/var/www/vltksource_new/vl_update_27`. If a path is dead, repoint to `pak_unpacked` / vltktool.
If a C++ provenance citation (e.g. `KRepresentShell3.cpp`, `KIpoTree.cpp`) isn't under the
in-scope tree (only `represent3.dll` ships), keep it as a marked provenance caveat + point to
`reverse-engineering` against the DLL — don't present it as greppable source.

### 5. Ad-hoc scanner / decoder check (AGENTS rule violation)
Grep bundled scripts for `rglob('*.spr')`, `os.walk`, hand-rolled SPR header parsing. Per AGENTS
rules: do NOT write your own SPR/PAK decoder and do NOT scan the whole source (can crash the box).
The canonical decoder is `/var/www/vltktool/extract_item_spr.py` (top-down rows; `--legacy-flip`
for legacy inverted output; `--file` for one SPR, `--frame N` for one frame). Replace ad-hoc
scripts with a thin wrapper that requires an explicit `--file`/narrow `--src` and refuses a
whole-root scan. Find a SPR's uid/path first via signed-byte hash + pak index, or vltktool
resolvers (`resolve_uid.py`, `find_spr_by_image.py --pak <one pak>`).

## Fix discipline
- Re-read each file immediately before editing — these skills can have a concurrent writer this
  session; `patch` failing with "modified since read" means re-read, don't force.
- After editing any bundled `.py`: `py_compile` it and re-run its evidence check (e.g. uid pair,
  or actually decode one real SPR end-to-end) before claiming the fix works.
- Delete stale `__pycache__` after editing a script so a future run doesn't import the old bytecode.
- Commit + push when done (AGENTS.md requires it).
