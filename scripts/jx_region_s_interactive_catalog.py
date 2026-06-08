#!/usr/bin/env python3
"""Catalog PC Region_S trap/object records for generated map geometries.

This is metadata-only: it preserves authoritative PC Region_S Trap.dat and Obj_S.dat
records without rendering placeholder visuals. Format is from SceneDataDef.h:
- section 1: KTrapFileHead + KSPTrap[8 bytes]
- section 3: KObjFileHead + variable KSPObj records
"""
from __future__ import annotations

import argparse
import importlib.util
import json
import os
import re
import shutil
import struct
import sys
import time
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

UNITY_ROOT = Path('/var/www/vltk-mobile')
PC_ROOT = Path('/var/www/vltksource_new/vl_update_27')
JX_MAP_PORT = UNITY_ROOT / 'harness/.codex/skills/jx-map-port/scripts/jx_map_port.py'
VLTK_DECODE = Path('/var/www/vltktool/decode_item_texts_vi.py')
REGION_FILE_RE = re.compile(r'^(\d+)_(\d+)_Region_S\.dat$', re.IGNORECASE)
KILLBOSSMATCH_MAP_IDS = set(range(907, 917))
OPEN_SERVER_CONFIG_RELPATH = Path('script/global/pgaming/configserver/configall.lua')
PC_FACTION_IDS = {
    'shaolin': 1,
    'tianwang': 2,
    'tangmen': 3,
    'gaibang': 4,
    'wudu': 5,
    'tianren': 6,
    'emei': 7,
    'cuiyan': 8,
    'wudang': 9,
    'kunlun': 10,
}


def load_module(path: Path, name: str):
    spec = importlib.util.spec_from_file_location(name, path)
    if spec is None or spec.loader is None:
        raise RuntimeError(f'Cannot import {path}')
    module = importlib.util.module_from_spec(spec)
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


jx = load_module(JX_MAP_PORT, 'jx_map_port_interactive')
try:
    decoder = load_module(VLTK_DECODE, 'vltk_decode_interactive')
except Exception:
    decoder = None


def utc_now() -> str:
    return datetime.now(timezone.utc).strftime('%Y-%m-%dT%H:%M:%SZ')


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def make_meta(path: Path) -> None:
    meta = path.with_suffix(path.suffix + '.meta')
    if meta.exists():
        return
    meta.write_text(
        'fileFormatVersion: 2\n'
        f'guid: {uuid.uuid4().hex}\n'
        'DefaultImporter:\n'
        '  externalObjects: {}\n'
        '  userData:\n'
        '  assetBundleName:\n'
        '  assetBundleVariant:\n', encoding='utf-8')


def decode_legacy_text(raw: bytes) -> str:
    if decoder is not None:
        try:
            text, _enc, _score = decoder.decode_best(raw)
            return text
        except Exception:
            pass
    for enc in ('utf-8', 'gb18030', 'cp1252'):
        try:
            return raw.decode(enc)
        except Exception:
            continue
    return raw.decode('utf-8', errors='replace')


def int_col(cols: list[str], index: int) -> int:
    if index < 0 or index >= len(cols):
        return 0
    try:
        return int(cols[index].strip() or '0')
    except ValueError:
        return 0


def normalize_resource_path(path: str) -> str:
    path = (path or '').strip().replace('/', '\\')
    if path and not path.startswith('\\'):
        path = '\\' + path
    return path


def load_objdata_templates(path: Path) -> dict[int, dict[str, Any]]:
    if not path.is_file():
        return {}
    text = decode_legacy_text(path.read_bytes())
    rows = [line.split('\t') for line in text.splitlines() if line.strip()]
    templates: dict[int, dict[str, Any]] = {}
    for cols in rows[1:]:
        if len(cols) < 5:
            continue
        data_id = int_col(cols, 1)
        if data_id <= 0:
            continue
        templates[data_id] = {
            'templateId': data_id,
            'nameVi': cols[0].strip(),
            'kind': cols[2].strip() if len(cols) > 2 else '',
            'scriptName': cols[3].strip() if len(cols) > 3 else '',
            'imageName': normalize_resource_path(cols[4] if len(cols) > 4 else ''),
            'soundName': cols[5].strip() if len(cols) > 5 else '',
            'lifeTime': int_col(cols, 6),
            'layer': int_col(cols, 7),
            'height': int_col(cols, 8),
            'imageTotalFrame': int_col(cols, 21),
            'imageCurFrame': int_col(cols, 22),
            'imageTotalDir': int_col(cols, 23),
            'imageCurDir': int_col(cols, 24),
            'imageInterval': int_col(cols, 25),
            'imageCgXpos': int_col(cols, 26),
            'imageCgYpos': int_col(cols, 27),
            'isUnseen': int_col(cols, 51),
            'obstacleKind': int_col(cols, 52),
            'loopAnimation': int_col(cols, 53),
        }
    return templates


def client_package_order(pc_root: Path) -> list[Path]:
    client_root = pc_root / 'Client 6.0'
    data_dir = client_root / 'data'
    package_ini = client_root / 'package1.ini'
    result: list[Path] = []
    if package_ini.is_file():
        for line in package_ini.read_text(encoding='utf-8', errors='ignore').splitlines():
            line = line.strip()
            if not line or line.startswith('[') or '=' not in line:
                continue
            key, value = line.split('=', 1)
            if not key.strip().isdigit():
                continue
            pak = data_dir / value.strip()
            if pak.is_file():
                result.append(pak)
    return result or sorted(data_dir.glob('*.pak'))


def build_priority_index(package_paths: list[Path]) -> dict[int, tuple[str, int, int, int]]:
    index: dict[int, tuple[str, int, int, int]] = {}
    for pak in package_paths:
        try:
            with pak.open('rb') as f:
                header = f.read(32)
                if len(header) < 32 or header[:4] != b'PACK':
                    continue
                count, index_offset = struct.unpack_from('<II', header, 4)
                f.seek(index_offset)
                for _ in range(count):
                    record = f.read(16)
                    if len(record) != 16:
                        break
                    uid, offset, size, flag = struct.unpack('<IIii', record)
                    index.setdefault(uid & 0xffffffff, (str(pak), offset, size, flag))
        except Exception as ex:
            print(f'WARN: failed to index {pak}: {ex}', file=sys.stderr)
    return index


def extract_spr(index: dict[int, tuple[str, int, int, int]], by_rel: dict[str, str], source_path: str) -> bytes | None:
    source_path = normalize_resource_path(source_path)
    if not source_path:
        return None
    uid = jx.g_filename2id(source_path.encode('gbk', errors='ignore'))
    loc = index.get(uid)
    if loc:
        raw, method, dsize = jx.read_entry(loc)
        data = jx.get_flat_spr(raw, method, dsize)
        if data:
            return data
    loose = jx.loose_path(by_rel, source_path) if by_rel else None
    if loose:
        return Path(loose).read_bytes()
    return None


def stage_object_templates(templates: dict[int, dict[str, Any]], used_template_ids: set[int],
                           pc_root: Path, out_root: Path, extract: bool) -> tuple[list[dict[str, Any]], dict[str, Any]]:
    out_root.mkdir(parents=True, exist_ok=True)
    index = build_priority_index(client_package_order(pc_root))
    by_rel = jx.loose_index([str(pc_root / 'Client 6.0')])

    catalog: list[dict[str, Any]] = []
    unique_paths: set[str] = set()
    staged_paths: set[str] = set()
    missing_paths: set[str] = set()
    for template_id in sorted(used_template_ids):
        template = templates.get(template_id)
        if not template:
            catalog.append({'templateId': template_id, 'missingTemplate': True})
            continue
        image = normalize_resource_path(template.get('imageName', ''))
        uid = jx.compute_path_uid(image) if image else ''
        staged = False
        size = 0
        if image:
            unique_paths.add(image)
            data = extract_spr(index, by_rel, image)
            if data:
                staged = True
                staged_paths.add(image)
                size = len(data)
                if extract:
                    (out_root / f'{uid}.spr').write_bytes(data)
            else:
                missing_paths.add(image)
        entry = dict(template)
        entry.update({
            'sourcePath': image.lstrip('\\'),
            'uid': uid,
            'staged': staged,
            'bytes': size,
            'missingTemplate': False,
        })
        catalog.append(entry)
    coverage = {
        'objectTemplatesUsed': len(used_template_ids),
        'objectTemplatesResolved': sum(1 for e in catalog if not e.get('missingTemplate')),
        'uniqueObjectSpritePaths': len(unique_paths),
        'stagedObjectSpritePaths': len(staged_paths),
        'missingObjectSpritePaths': len(missing_paths),
        'missingObjectSprites': sorted(p.lstrip('\\') for p in missing_paths),
    }
    return catalog, coverage


def server_root(pc_root: Path) -> Path:
    return pc_root / 'Server 6.0/server/home_jxser_bachkim_6.0/server1'


def decode_gbk(raw: bytes) -> str:
    return raw.decode('gbk', errors='replace')


def decode_server_text(raw: bytes) -> str:
    for enc in ('gb18030', 'gbk', 'utf-8'):
        try:
            return raw.decode(enc)
        except Exception:
            continue
    return raw.decode('gb18030', errors='replace')


def split_lua_args(arg_text: str) -> list[str]:
    args: list[str] = []
    buf: list[str] = []
    quote = ''
    for ch in arg_text:
        if quote:
            buf.append(ch)
            if ch == quote:
                quote = ''
            continue
        if ch in ('"', "'"):
            quote = ch
            buf.append(ch)
            continue
        if ch == ',':
            args.append(''.join(buf).strip().strip('\"\''))
            buf = []
            continue
        buf.append(ch)
    args.append(''.join(buf).strip().strip('\"\''))
    return args


def parse_lua_calls(source: str, function_name: str, limit: int = 8) -> list[list[str]]:
    calls = []
    pattern = r'\b' + re.escape(function_name) + r'\s*\(([^)]*)\)'
    for match in re.finditer(pattern, source):
        calls.append(split_lua_args(match.group(1)))
        if len(calls) >= limit:
            break
    return calls


def script_action_summary(source: str) -> dict[str, Any]:
    source = strip_lua_line_comments(source)
    new_world = [args[:3] for args in parse_lua_calls(source, 'NewWorld')]
    set_pos = [args[:2] for args in parse_lua_calls(source, 'SetPos')]
    set_fight_state = [args[:1] for args in parse_lua_calls(source, 'SetFightState')]
    msg2_player = [args[:1] for args in parse_lua_calls(source, 'Msg2Player')]
    talk = parse_lua_calls(source, 'Talk')
    say = parse_lua_calls(source, 'Say')
    add_event_item = [args[:1] for args in parse_lua_calls(source, 'AddEventItem')]
    add_note = [args[:1] for args in parse_lua_calls(source, 'AddNote')]
    set_prop_state = parse_lua_calls(source, 'SetPropState')
    add_termini = [args[:1] for args in parse_lua_calls(source, 'AddTermini')]
    set_protect_time = [args[:1] for args in parse_lua_calls(source, 'SetProtectTime')]
    add_skill_state = [args[:4] for args in parse_lua_calls(source, 'AddSkillState')]
    get_task = parse_lua_calls(source, 'GetTask')
    set_task = parse_lua_calls(source, 'SetTask') + parse_lua_calls(source, 'SetTaskTemp')
    have_item = parse_lua_calls(source, 'HaveItem')
    add_item = parse_lua_calls(source, 'AddItem')
    del_item = parse_lua_calls(source, 'DelItem')
    return {
        'hasMain': re.search(r'function\s+main\s*\(', source) is not None,
        'newWorldCalls': new_world[:8],
        'setPosCalls': set_pos[:8],
        'setFightStateCalls': set_fight_state[:8],
        'msg2PlayerCalls': msg2_player[:8],
        'talkCalls': talk[:8],
        'sayCalls': say[:8],
        'addEventItemCalls': add_event_item[:8],
        'addNoteCalls': add_note[:8],
        'setPropStateCalls': set_prop_state[:8],
        'addTerminiCalls': add_termini[:8],
        'setProtectTimeCalls': set_protect_time[:8],
        'addSkillStateCalls': add_skill_state[:8],
        'getTaskCalls': get_task[:8],
        'setTaskCalls': set_task[:8],
        'haveItemCalls': have_item[:8],
        'addItemCalls': add_item[:8],
        'delItemCalls': del_item[:8],
        'setsFightState': 'SetFightState' in source,
        'talks': 'Talk(' in source or 'Msg2Player' in source,
        'usesTaskApis': any(token in source for token in ('GetTask', 'SetTask', 'SetTaskTemp', 'AddNote')),
        'usesItemApis': any(token in source for token in ('AddItem', 'DelItem', 'HaveItem', 'AddEventItem')),
        'usesObjectApis': any(token in source for token in ('SetPropState', 'SetObjState', 'DelObj')),
        'usesTerminiApis': 'AddTermini' in source,
        'usesProtectApis': 'SetProtectTime' in source or 'AddSkillState' in source,
        'usesCityApis': 'OpenCityManageUI' in source,
    }


def trap_script_action_summary(source: str) -> dict[str, Any]:
    actions = script_action_summary(source)
    return {
        'hasMain': actions.get('hasMain', False),
        'newWorldCalls': actions.get('newWorldCalls', []),
        'setPosCalls': actions.get('setPosCalls', []),
        'setFightStateCalls': actions.get('setFightStateCalls', []),
        'msg2PlayerCalls': actions.get('msg2PlayerCalls', []),
        'talkCalls': actions.get('talkCalls', []),
        'sayCalls': actions.get('sayCalls', []),
        'addTerminiCalls': actions.get('addTerminiCalls', []),
        'setProtectTimeCalls': actions.get('setProtectTimeCalls', []),
        'addSkillStateCalls': actions.get('addSkillStateCalls', []),
        'setsFightState': actions.get('setsFightState', False),
        'talks': actions.get('talks', False),
    }


def build_script_hash_index(root: Path) -> dict[int, dict[str, Any]]:
    root_b = os.fsencode(root)
    index: dict[int, dict[str, Any]] = {}
    if not os.path.isdir(root_b):
        return index
    for dirpath, _dirnames, filenames in os.walk(root_b):
        for filename in filenames:
            if not filename.lower().endswith((b'.lua', b'.txt', b'.ini', b'.cfg', b'.tab')):
                continue
            full = os.path.join(dirpath, filename)
            rel = os.path.relpath(full, root_b).replace(os.sep.encode(), b'\\')
            source_bytes = b'\\' + rel
            script_id = jx.g_filename2id(source_bytes)
            entry = index.setdefault(script_id, {
                'trapId': script_id,
                'trapIdHex': f'0x{script_id:08X}',
                'scriptPath': decode_gbk(source_bytes),
                'sourceRelPath': decode_gbk(rel),
                'sourceFile': os.fsdecode(full),
            })
            if source_bytes.lower().startswith(b'\\script') and not entry['scriptPath'].lower().startswith('\\script'):
                entry.update({
                    'scriptPath': decode_gbk(source_bytes),
                    'sourceRelPath': decode_gbk(rel),
                    'sourceFile': os.fsdecode(full),
                })
    return index


def build_trap_script_catalog(trap_ids: set[int], pc_root: Path) -> tuple[list[dict[str, Any]], dict[str, Any]]:
    root = server_root(pc_root)
    script_index = build_script_hash_index(root)
    entries: list[dict[str, Any]] = []
    resolved = 0
    for trap_id in sorted(trap_ids):
        src = script_index.get(trap_id)
        entry = {
            'trapId': trap_id,
            'trapIdHex': f'0x{trap_id:08X}',
            'resolved': src is not None,
        }
        if src:
            resolved += 1
            try:
                text = decode_legacy_text(Path(src['sourceFile']).read_bytes())
            except Exception:
                text = ''
            entry.update({
                'scriptPath': src['scriptPath'],
                'sourceRelPath': src['sourceRelPath'],
                'actions': trap_script_action_summary(text),
                'sourceText': text,
            })
        entries.append(entry)
    coverage = {
        'uniqueTrapIds': len(trap_ids),
        'resolvedTrapScripts': resolved,
        'missingTrapScripts': len(trap_ids) - resolved,
        'missingTrapScriptIds': [f'0x{e["trapId"]:08X}' for e in entries if not e.get('resolved')],
        'sourceScriptRoot': str(root / 'script'),
    }
    return entries, coverage



def normalize_script_path(script: str) -> str:
    script = (script or '').strip()
    if script and not script.startswith('\\'):
        script = '\\' + script
    return script


def build_object_script_catalog(geometries: list[dict[str, Any]], pc_root: Path) -> tuple[list[dict[str, Any]], dict[str, Any]]:
    usage: dict[str, int] = {}
    for geometry in geometries:
        for obj in geometry.get('objects', []):
            script = normalize_script_path(obj.get('script', ''))
            if script:
                usage[script] = usage.get(script, 0) + 1
    root = server_root(pc_root)
    script_index = build_script_hash_index(root)
    entries: list[dict[str, Any]] = []
    resolved = 0
    for script in sorted(usage):
        script_id = jx.g_filename2id(script.encode('gbk', errors='ignore'))
        src = script_index.get(script_id)
        entry = {
            'scriptPath': script,
            'scriptId': int(script_id),
            'scriptIdHex': f'0x{script_id:08X}',
            'objectRefs': usage[script],
            'resolved': src is not None,
        }
        if src:
            resolved += 1
            try:
                raw = Path(src['sourceFile']).read_bytes()
                text = decode_legacy_text(raw)
            except Exception:
                text = ''
            entry.update({
                'sourceRelPath': src['sourceRelPath'],
                'actions': script_action_summary(text),
                'sourceText': text,
            })
        entries.append(entry)
    coverage = {
        'objectScriptRefs': sum(usage.values()),
        'uniqueObjectScripts': len(usage),
        'resolvedObjectScripts': resolved,
        'missingObjectScripts': len(usage) - resolved,
        'missingObjectScriptIds': [e['scriptIdHex'] for e in entries if not e.get('resolved')],
        'objectScriptsWithNewWorld': sum(1 for e in entries if (e.get('actions') or {}).get('newWorldCalls')),
        'objectScriptsWithMsg2Player': sum(1 for e in entries if (e.get('actions') or {}).get('msg2PlayerCalls')),
        'objectScriptsWithTalk': sum(1 for e in entries if (e.get('actions') or {}).get('talkCalls')),
        'objectScriptsWithSay': sum(1 for e in entries if (e.get('actions') or {}).get('sayCalls')),
    }
    return entries, coverage


def clean_user_message(message: str) -> str:
    message = (message or '').strip()
    return (message
            .replace('ChiƠn', 'Chiến')
            .replace('chiƠn', 'chiến')
            .replace('ThiƠu', 'Thiếu')
            .replace('thiƠu', 'thiếu')
            .replace('KiƠm', 'Kiếm')
            .replace('kiƠm', 'kiếm')
            .replace('phƯa', 'phía')
            .replace('PhƯa', 'Phía')
            .replace('tiƠng', 'tiếng')
            .replace('TiƠng', 'Tiếng')
            .replace('giƠng', 'giếng')
            .replace('GiƠng', 'Giếng')
            .replace('Cút mau! Đơng để ta gặp lại thấy ngươi đă!', 'Cút mau! Đừng để ta gặp lại thấy ngươi đấy!')
            .replace('đƠn', 'đến')
            .replace('ĐƠn', 'Đến')
            .replace('nă cứ', 'nó cứ')
            .replace('quay lai', 'quay lại')
            .replace('chiƠc', 'chiếc')
            .replace('ChiƠc', 'Chiếc')
            .replace('khăa', 'khóa')
            .replace('Khăa', 'Khóa')
            .replace('đã đăng, hãy', 'đã đóng, hãy')
            .replace('Đã đăng, hãy', 'Đã đóng, hãy')
            .replace('  ', ' '))


def is_safe_user_message(message: str) -> bool:
    if not message:
        return False
    if '\ufffd' in message or any('\u4e00' <= ch <= '\u9fff' for ch in message):
        return False
    # Keep only decoded Vietnamese/ASCII-style PC text; skip mojibake-heavy lines.
    if any(ch in message for ch in '°Å¶·¸¹º»¼½¾¿'):
        return False
    return True


def single_string(calls: list[list[str]]) -> str:
    if not calls or not calls[0]:
        return ''
    return clean_user_message(str(calls[0][0]))


def int_args(calls: list[list[str]], width: int) -> list[tuple[int, ...]]:
    result: list[tuple[int, ...]] = []
    for call in calls or []:
        if len(call) < width:
            continue
        values = []
        ok = True
        for arg in call[:width]:
            if not re.fullmatch(r'\d+', str(arg).strip()):
                ok = False
                break
            values.append(int(str(arg).strip()))
        if ok:
            result.append(tuple(values))
    return result


def int_expr(value: Any) -> int | None:
    text = str(value).strip()
    if re.fullmatch(r'\d+', text):
        return int(text)
    match = re.fullmatch(r'(\d+)\s*\*\s*(\d+)', text)
    if match:
        return int(match.group(1)) * int(match.group(2))
    return None


def expr_args(calls: list[list[str]], width: int) -> list[tuple[int, ...]]:
    result: list[tuple[int, ...]] = []
    for call in calls or []:
        if len(call) < width:
            continue
        values: list[int] = []
        valid = True
        for arg in call[:width]:
            parsed = int_expr(arg)
            if parsed is None:
                valid = False
                break
            values.append(parsed)
        if valid:
            result.append(tuple(values))
    return result


def is_safe_say_message(actions: dict[str, Any]) -> bool:
    if not actions.get('sayCalls'):
        return False
    if not is_safe_user_message(single_string(actions.get('sayCalls'))):
        return False
    return not (
        actions.get('newWorldCalls') or
        actions.get('setPosCalls') or
        actions.get('setFightStateCalls') or
        actions.get('msg2PlayerCalls') or
        actions.get('talkCalls') or
        actions.get('getTaskCalls') or
        actions.get('setTaskCalls') or
        actions.get('haveItemCalls') or
        actions.get('addEventItemCalls') or
        actions.get('addNoteCalls') or
        actions.get('setPropStateCalls') or
        actions.get('addItemCalls') or
        actions.get('delItemCalls') or
        actions.get('usesCityApis')
    )


def strip_lua_line_comments(source: str) -> str:
    lines: list[str] = []
    for line in (source or '').splitlines():
        buf: list[str] = []
        quote = ''
        i = 0
        while i < len(line):
            ch = line[i]
            if quote:
                buf.append(ch)
                if ch == quote and (i == 0 or line[i - 1] != '\\'):
                    quote = ''
                i += 1
                continue
            if ch in ('"', "'"):
                quote = ch
                buf.append(ch)
                i += 1
                continue
            if ch == '-' and i + 1 < len(line) and line[i + 1] == '-':
                break
            buf.append(ch)
            i += 1
        lines.append(''.join(buf))
    return '\n'.join(lines)


def source_uses_only_calls(source: str, allowed: set[str]) -> bool:
    calls = set(re.findall(r'\b([A-Za-z_][A-Za-z0-9_]*)\s*\(', strip_lua_line_comments(source)))
    return calls.issubset(allowed)


def lua_function_body(source: str, function_name: str) -> str:
    clean_source = strip_lua_line_comments(source)
    pattern = (
        r'\bfunction\s+' + re.escape(function_name) +
        r'\s*\([^)]*\)(?P<body>.*?)(?=\n\s*function\s+[A-Za-z_][A-Za-z0-9_]*\s*\(|\Z)'
    )
    match = re.search(pattern, clean_source, re.S)
    return match.group('body') if match else ''


def has_lua_control_flow(source: str) -> bool:
    return re.search(r'\b(if|for|while)\b', strip_lua_line_comments(source)) is not None


def talk_messages(actions: dict[str, Any]) -> list[str]:
    messages: list[str] = []
    for call in actions.get('talkCalls') or []:
        if len(call) < 3:
            return []
        if not str(call[0]).strip().isdigit():
            return []
        callback = str(call[1]).strip() if len(call) > 1 else ''
        if callback not in ('', 'nil'):
            return []
        for part in call[2:]:
            message = clean_user_message(str(part))
            if not message:
                continue
            if '/' in message or not is_safe_user_message(message):
                return []
            messages.append(message)
    return messages


def callback_talk_messages(calls: list[list[str]]) -> list[str]:
    messages: list[str] = []
    for call in calls or []:
        if len(call) < 3:
            return []
        if not str(call[0]).strip().isdigit():
            return []
        callback = str(call[1]).strip() if len(call) > 1 else ''
        if callback in ('', 'nil'):
            return []
        for part in call[2:]:
            message = clean_user_message(str(part))
            if not message:
                continue
            if '/' in message or not is_safe_user_message(message):
                return []
            messages.append(message)
    return messages


def is_safe_talk_message(actions: dict[str, Any], source: str) -> bool:
    if not actions.get('talkCalls'):
        return False
    if not source_uses_only_calls(source, {'main', 'Talk'}):
        return False
    if has_lua_control_flow(source):
        return False
    if not talk_messages(actions):
        return False
    return not (
        actions.get('newWorldCalls') or
        actions.get('setPosCalls') or
        actions.get('setFightStateCalls') or
        actions.get('msg2PlayerCalls') or
        actions.get('sayCalls') or
        actions.get('getTaskCalls') or
        actions.get('setTaskCalls') or
        actions.get('haveItemCalls') or
        actions.get('addEventItemCalls') or
        actions.get('addNoteCalls') or
        actions.get('setPropStateCalls') or
        actions.get('addItemCalls') or
        actions.get('delItemCalls') or
        actions.get('usesCityApis')
    )


def has_trap_side_effect_actions(actions: dict[str, Any]) -> bool:
    return bool(
        actions.get('newWorldCalls') or
        actions.get('setPosCalls') or
        actions.get('setFightStateCalls') or
        actions.get('getTaskCalls') or
        actions.get('setTaskCalls') or
        actions.get('haveItemCalls') or
        actions.get('addEventItemCalls') or
        actions.get('addNoteCalls') or
        actions.get('setPropStateCalls') or
        actions.get('addItemCalls') or
        actions.get('delItemCalls') or
        actions.get('usesCityApis')
    )


def is_safe_trap_say_message(actions: dict[str, Any], source: str) -> bool:
    if not actions.get('sayCalls'):
        return False
    if actions.get('talkCalls') or actions.get('msg2PlayerCalls') or has_trap_side_effect_actions(actions):
        return False
    if not source_uses_only_calls(source, {'main', 'Say'}) or has_lua_control_flow(source):
        return False
    return is_safe_user_message(single_string(actions.get('sayCalls')))


def is_safe_trap_msg2player_message(actions: dict[str, Any], source: str) -> bool:
    if not actions.get('msg2PlayerCalls'):
        return False
    if actions.get('talkCalls') or actions.get('sayCalls') or has_trap_side_effect_actions(actions):
        return False
    if not source_uses_only_calls(source, {'main', 'Msg2Player'}) or has_lua_control_flow(source):
        return False
    return is_safe_user_message(single_string(actions.get('msg2PlayerCalls')))


def is_safe_trap_talk_message(actions: dict[str, Any], source: str) -> bool:
    if not actions.get('talkCalls'):
        return False
    if actions.get('sayCalls') or actions.get('msg2PlayerCalls') or has_trap_side_effect_actions(actions):
        return False
    if not source_uses_only_calls(source, {'main', 'Talk'}) or has_lua_control_flow(source):
        return False
    return bool(talk_messages(actions))


def main_callback_prompt_message(source: str) -> dict[str, Any] | None:
    main_body = lua_function_body(source, 'main')
    if not main_body:
        return None
    clean_body = strip_lua_line_comments(main_body)
    if has_lua_control_flow(clean_body):
        return None
    if not source_uses_only_calls(clean_body, {'Talk', 'Say'}):
        return None
    messages: list[str] = []
    talk_lines = callback_talk_messages(parse_lua_calls(clean_body, 'Talk', limit=4))
    if talk_lines:
        messages.extend(talk_lines)
    say_calls = parse_lua_calls(clean_body, 'Say', limit=4)
    for call in say_calls:
        if not call:
            return None
        message = clean_user_message(str(call[0]))
        if not message or not is_safe_user_message(message):
            return None
        messages.append(message)
    if not messages:
        return None
    return {
        'message': '\n'.join(messages),
        'messages': messages,
    }


def is_safe_trap_msg2player_newworld(actions: dict[str, Any], source: str) -> bool:
    if not actions.get('msg2PlayerCalls') or not actions.get('newWorldCalls'):
        return False
    if actions.get('setPosCalls') or actions.get('talkCalls') or actions.get('sayCalls'):
        return False
    if (
        actions.get('getTaskCalls') or actions.get('setTaskCalls') or actions.get('haveItemCalls') or
        actions.get('addEventItemCalls') or actions.get('addNoteCalls') or actions.get('setPropStateCalls') or
        actions.get('addItemCalls') or actions.get('delItemCalls') or actions.get('usesCityApis')
    ):
        return False
    if actions.get('addTerminiCalls') and not expr_args(actions.get('addTerminiCalls') or [], 1):
        return False
    if not source_uses_only_calls(source, {'main', 'Msg2Player', 'SetFightState', 'NewWorld', 'AddTermini'}):
        return False
    if has_lua_control_flow(source):
        return False
    if int_args_unique(actions.get('newWorldCalls') or [], 3) is None:
        return False
    if actions.get('setFightStateCalls') and int_args_unique(actions.get('setFightStateCalls') or [], 1) is None:
        return False
    return is_safe_user_message(single_string(actions.get('msg2PlayerCalls')))


def task_optional_talk_newworld(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    if not source_uses_only_calls(clean_source, {'main', 'GetTask', 'Talk', 'SetFightState', 'NewWorld', 'if'}):
        return None
    if re.search(r'\b(elseif|for|while)\b', clean_source):
        return None

    new_world = int_args(parse_lua_calls(clean_source, 'NewWorld', limit=2), 3)
    fight_state = int_args(parse_lua_calls(clean_source, 'SetFightState', limit=2), 1)
    talk_lines = talk_messages({'talkCalls': parse_lua_calls(clean_source, 'Talk', limit=2)})
    if len(new_world) != 1 or len(fight_state) != 1 or len(talk_lines) != 1:
        return None

    task_match = re.search(r'\b([A-Za-z_][A-Za-z0-9_]*)\s*=\s*GetTask\s*\(\s*(\d+)\s*\)', clean_source)
    if task_match:
        task_var = re.escape(task_match.group(1))
        task_id = int(task_match.group(2))
        value_match = re.search(r'\bif\s*\(\s*' + task_var + r'\s*==\s*(\d+)\s*\)', clean_source)
    else:
        value_match = re.search(r'\bif\s*\(\s*GetTask\s*\(\s*(\d+)\s*\)\s*==\s*(\d+)\s*\)', clean_source)
        task_id = int(value_match.group(1)) if value_match else -1
    if not value_match or task_id < 0:
        return None

    return {
        'targetMapId': new_world[0][0],
        'targetCellX': new_world[0][1],
        'targetCellY': new_world[0][2],
        'fightState': fight_state[0][0],
        'taskId': task_id,
        'taskBranches': [{
            'values': [int(value_match.group(1 if task_match else 2))],
            'targetCellX': 0,
            'targetCellY': 0,
            'message': talk_lines[0],
        }],
    }


def message_random_newworld(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    if not source_uses_only_calls(clean_source, {'main', 'GetSex', 'Talk', 'random', 'if', 'elseif', 'NewWorld'}):
        return None
    if re.search(r'\b(for|while)\b', clean_source):
        return None

    random_match = re.search(r'\b([A-Za-z_][A-Za-z0-9_]*)\s*=\s*random\s*\(\s*(\d+)\s*,\s*(\d+)\s*\)', clean_source)
    if not random_match:
        return None
    random_var = re.escape(random_match.group(1))
    thresholds = [int(value) for value in re.findall(r'\b(?:if|elseif)\s*\(\s*' + random_var + r'\s*<\s*(\d+)\s*\)', clean_source)]
    new_worlds = int_args(parse_lua_calls(clean_source, 'NewWorld', limit=4), 3)
    talk_lines = talk_messages({'talkCalls': parse_lua_calls(clean_source, 'Talk', limit=4)})
    if len(thresholds) != 2 or len(new_worlds) != 3 or len(talk_lines) != 2:
        return None
    if talk_lines[0] != talk_lines[1]:
        return None

    return {
        'message': talk_lines[0],
        'randomMin': int(random_match.group(2)),
        'randomMax': int(random_match.group(3)),
        'randomThresholds': thresholds,
        'randomTargetMapIds': [values[0] for values in new_worlds],
        'randomTargetCellXs': [values[1] for values in new_worlds],
        'randomTargetCellYs': [values[2] for values in new_worlds],
    }


def level_gate_requirement(source: str) -> int | None:
    match = re.search(r'GetLevel\s*\(\s*\)\s*>=\s*(\d+)', strip_lua_line_comments(source))
    return int(match.group(1)) if match else None


def is_safe_trap_level_gate_newworld(actions: dict[str, Any], source: str) -> bool:
    if level_gate_requirement(source) is None:
        return False
    allowed = {
        'main', 'if', 'GetLevel', 'SetFightState', 'NewWorld', 'Talk', 'SetPos',
        'AddTermini', 'SetProtectTime', 'AddSkillState'
    }
    clean_source = strip_lua_line_comments(source)
    if not source_uses_only_calls(clean_source, allowed):
        return False
    if re.search(r'\b(elseif|for|while)\b', clean_source):
        return False
    if actions.get('msg2PlayerCalls') or actions.get('sayCalls'):
        return False
    if (
        actions.get('getTaskCalls') or actions.get('setTaskCalls') or actions.get('haveItemCalls') or
        actions.get('addEventItemCalls') or actions.get('addNoteCalls') or actions.get('setPropStateCalls') or
        actions.get('addItemCalls') or actions.get('delItemCalls') or actions.get('usesCityApis')
    ):
        return False
    if int_args_unique(actions.get('newWorldCalls') or [], 3) is None:
        return False
    if int_args_unique(actions.get('setFightStateCalls') or [], 1) is None:
        return False
    if actions.get('setPosCalls') and int_args_unique(actions.get('setPosCalls') or [], 2) is None:
        return False
    if actions.get('setProtectTimeCalls') and expr_args_unique(actions.get('setProtectTimeCalls') or [], 1) is None:
        return False
    if actions.get('addSkillStateCalls') and expr_args_unique(actions.get('addSkillStateCalls') or [], 4) is None:
        return False
    if actions.get('addTerminiCalls') and not expr_args(actions.get('addTerminiCalls') or [], 1):
        return False
    return bool(talk_messages(actions))


def level_bracket_newworld(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    allowed = {
        'main', 'Include', 'if', 'elseif', 'GetLevel', 'Talk', 'NewWorld',
        'SetFightState', 'Msg2Player', 'SetProtectTime', 'AddSkillState'
    }
    if not source_uses_only_calls(clean_source, allowed):
        return None
    if 'GetLevel() < 40' not in clean_source or 'elseif' not in clean_source:
        return None
    if re.search(r'\b(for|while)\b', clean_source):
        return None
    new_worlds = int_args(parse_lua_calls(clean_source, 'NewWorld', limit=6), 3)
    if len(new_worlds) != 3:
        return None
    fight_state = int_args_unique(parse_lua_calls(clean_source, 'SetFightState', limit=6), 1)
    if fight_state is None:
        return None
    if [target[0] for target in new_worlds] != [323, 324, 325]:
        return None

    fail_lines = talk_messages({'talkCalls': parse_lua_calls(clean_source, 'Talk', limit=2)})
    if len(fail_lines) != 1:
        return None
    branch_messages = [clean_user_message(str(call[0])) for call in parse_lua_calls(clean_source, 'Msg2Player', limit=6) if call]
    if len(branch_messages) != 3 or not all(is_safe_user_message(message) for message in branch_messages):
        return None
    protect = expr_args_unique(parse_lua_calls(clean_source, 'SetProtectTime', limit=2), 1) if 'SetProtectTime' in clean_source else None
    skill = expr_args_unique(parse_lua_calls(clean_source, 'AddSkillState', limit=2), 4) if 'AddSkillState' in clean_source else None

    return {
        'requiredLevel': 40,
        'message': fail_lines[0],
        'levelBracketMinLevels': [40, 80, 120],
        'levelBracketMaxExclusiveLevels': [80, 120, 0],
        'levelBracketTargetMapIds': [target[0] for target in new_worlds],
        'levelBracketTargetCellXs': [target[1] for target in new_worlds],
        'levelBracketTargetCellYs': [target[2] for target in new_worlds],
        'levelBracketMessages': branch_messages,
        'fightState': fight_state[0],
        'protectTicks': protect[0] if protect is not None else 0,
        'skillStateId': skill[0] if skill is not None else 0,
        'skillStateLevel': skill[1] if skill is not None else 0,
        'skillStateTime': skill[3] if skill is not None else 0,
    }


def open_server_config(pc_root: Path = PC_ROOT) -> tuple[int, str]:
    path = server_root(pc_root) / OPEN_SERVER_CONFIG_RELPATH
    try:
        text = decode_legacy_text(path.read_bytes())
    except Exception:
        return 0, ''
    date_match = re.search(r'ThoiGianOpenServer\s*=\s*(\d+)', text)
    msg_match = re.search(r'ThoiGianOpenServerText\s*=\s*"([^"]*)"', text)
    date_value = int(date_match.group(1)) if date_match else 0
    message = clean_user_message(msg_match.group(1)) if msg_match else ''
    return date_value, message


def split_date_gate_blocks(source: str) -> tuple[str, str] | None:
    lines = (source or '').splitlines()
    start = None
    for index, line in enumerate(lines):
        if re.search(r'\bif\s+nDate\s*<\s*ThoiGianOpenServer\s*then\b', line):
            start = index
            break
    if start is None:
        return None

    closed: list[str] = []
    opened: list[str] = []
    branch = closed
    depth = 1
    for line in lines[start + 1:]:
        stripped = line.strip()
        if depth == 1 and re.fullmatch(r'else\s*;?', stripped):
            branch = opened
            continue
        if re.fullmatch(r'end\s*;?', stripped):
            depth -= 1
            if depth <= 0:
                break
            branch.append(line)
            continue
        branch.append(line)
        if re.search(r'\bif\b.*\bthen\b', stripped) and not re.search(r'\belseif\b', stripped):
            depth += 1
    if not closed or not opened:
        return None
    return '\n'.join(closed), '\n'.join(opened)


def unique_int_list(calls: list[list[str]]) -> list[int]:
    values: list[int] = []
    for parsed in expr_args(calls or [], 1):
        value = parsed[0]
        if value not in values:
            values.append(value)
    return values


def first_expr_arg(calls: list[list[str]], width: int) -> tuple[int, ...] | None:
    parsed = expr_args(calls or [], width)
    return parsed[0] if parsed else None


def open_server_date_gate_setpos(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    allowed = {
        'main', 'Include', 'tonumber', 'GetLocalDate', 'if', 'GetFightState',
        'SetPos', 'Msg2Player', 'AddStation', 'SetProtectTime', 'AddSkillState',
        'SetFightState'
    }
    if 'ThoiGianOpenServer' not in clean_source or not source_uses_only_calls(clean_source, allowed):
        return None
    blocks = split_date_gate_blocks(clean_source)
    if blocks is None:
        return None
    closed_block, open_block = blocks
    closed_positions = int_args(parse_lua_calls(closed_block, 'SetPos'), 2)
    if not closed_positions:
        return None
    open_branch = conditional_fight_state_setpos(open_block)
    if open_branch is None:
        return None
    open_server_date, open_server_message = open_server_config()
    if open_server_date <= 0 or not is_safe_user_message(open_server_message):
        return None
    closed_actions = script_action_summary(closed_block)
    open_actions = script_action_summary(open_block)
    closed_skill = first_expr_arg(closed_actions.get('addSkillStateCalls') or [], 4)
    open_skill = first_expr_arg(open_actions.get('addSkillStateCalls') or [], 4)
    closed_protect = first_expr_arg(closed_actions.get('setProtectTimeCalls') or [], 1)
    open_protect = first_expr_arg(open_actions.get('setProtectTimeCalls') or [], 1)
    return {
        'openServerDate': open_server_date,
        'openServerMessage': open_server_message,
        'closedTargetCellX': closed_positions[0][0],
        'closedTargetCellY': closed_positions[0][1],
        'closedStationIds': unique_int_list([args[:1] for args in parse_lua_calls(closed_block, 'AddStation')]),
        'openStationIds': unique_int_list([args[:1] for args in parse_lua_calls(open_block, 'AddStation')]),
        'closedProtectTicks': closed_protect[0] if closed_protect else 0,
        'openProtectTicks': open_protect[0] if open_protect else 0,
        'closedSkillStateId': closed_skill[0] if closed_skill else 0,
        'closedSkillStateLevel': closed_skill[1] if closed_skill else 0,
        'closedSkillStateTime': closed_skill[3] if closed_skill else 0,
        'openSkillStateId': open_skill[0] if open_skill else 0,
        'openSkillStateLevel': open_skill[1] if open_skill else 0,
        'openSkillStateTime': open_skill[3] if open_skill else 0,
        **open_branch,
    }


def desert_maze_random_newworld(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    allowed = {'main', 'random', 'if', 'elseif', 'SetFightState', 'NewWorld', 'SubWorldIdx2ID', 'GetWorldPos'}
    if 'random(0,120)' not in clean_source or not source_uses_only_calls(clean_source, allowed):
        return None
    random_match = re.search(r'\brandom\s*\(\s*(\d+)\s*,\s*(\d+)\s*\)', clean_source)
    if not random_match:
        return None
    branch_source = clean_source[random_match.start():]
    thresholds = [int(v) for v in re.findall(r'(?:if|elseif)\s*\(?\s*i\s*<\s*(\d+)\s*\)?\s*then', branch_source)]
    branch_targets = int_args(parse_lua_calls(branch_source, 'NewWorld', limit=32), 3)
    branch_fight_states = [v[0] for v in int_args(parse_lua_calls(branch_source, 'SetFightState', limit=32), 1)]
    if len(branch_targets) < 2 or len(thresholds) != len(branch_targets) - 1:
        return None
    if len(branch_fight_states) != len(branch_targets) or len(set(branch_fight_states)) != 1:
        return None
    gate_match = re.search(
        r'if\s*\(?\s*n_mapid\s*==\s*(\d+)\s*\)?\s*then(?P<body>.*?)\breturn\s*end',
        clean_source,
        re.S)
    gate: dict[str, int] = {}
    if gate_match:
        gate_body = gate_match.group('body')
        gate_target = int_args(parse_lua_calls(gate_body, 'NewWorld', limit=4), 3)
        gate_fight = int_args(parse_lua_calls(gate_body, 'SetFightState', limit=4), 1)
        if len(gate_target) != 1 or len(gate_fight) != 1:
            return None
        gate = {
            'gateCurrentMapId': int(gate_match.group(1)),
            'gateTargetMapId': gate_target[0][0],
            'gateTargetCellX': gate_target[0][1],
            'gateTargetCellY': gate_target[0][2],
            'gateFightState': gate_fight[0][0],
        }
    no_action_map_ids = [int(v) for v in re.findall(r'nSubWorldId\s*==\s*(\d+)', clean_source)]
    return {
        'randomMin': int(random_match.group(1)),
        'randomMax': int(random_match.group(2)),
        'randomThresholds': thresholds,
        'randomTargetMapIds': [target[0] for target in branch_targets],
        'randomTargetCellXs': [target[1] for target in branch_targets],
        'randomTargetCellYs': [target[2] for target in branch_targets],
        'randomFightState': branch_fight_states[0],
        'noActionMapIds': no_action_map_ids,
        **gate,
    }


def revive_return_newworld(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    allowed = {'main', 'SubWorldIdx2ID', 'RevID2WXY', 'GetPlayerRev', 'NewWorld', 'SetFightState', 'AddTermini', 'if'}
    if 'RevID2WXY(GetPlayerRev())' not in clean_source or not source_uses_only_calls(clean_source, allowed):
        return None
    fixed_targets = []
    for call in parse_lua_calls(clean_source, 'NewWorld', limit=8):
        if len(call) >= 3 and all(re.fullmatch(r'\d+', str(arg).strip()) for arg in call[:3]):
            fixed_targets.append(tuple(int(str(arg).strip()) for arg in call[:3]))
    if len(fixed_targets) != 1:
        return None
    fight_state = int_args_unique(parse_lua_calls(clean_source, 'SetFightState', limit=8), 1)
    if fight_state is None:
        return None
    revive_return_map_ids = [int(v) for v in re.findall(r'nSubWorldId\s*==\s*(\d+)', clean_source)]
    if not revive_return_map_ids:
        return None
    target = fixed_targets[0]
    termini_ids = [values[0] for values in expr_args(parse_lua_calls(clean_source, 'AddTermini', limit=4), 1)]
    return {
        'reviveReturnMapIds': revive_return_map_ids,
        'targetMapId': target[0],
        'targetCellX': target[1],
        'targetCellY': target[2],
        'fightState': fight_state[0],
        'terminiIds': termini_ids,
    }


def task_setpos_message_gate(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    allowed = {'main', 'if', 'elseif', 'GetTask', 'SetPos', 'Msg2Player'}
    if not source_uses_only_calls(clean_source, allowed):
        return None
    if re.search(r'\b(for|while)\b', clean_source):
        return None
    branches: list[dict[str, Any]] = []
    task_id: int | None = None
    branch_re = re.compile(
        r'(?:if|elseif)\s*\((?P<cond>.*?)\)\s*then(?P<body>.*?)(?=^\s*elseif\s*\(|^\s*end\b)',
        re.S | re.M)
    for match in branch_re.finditer(clean_source):
        cond = match.group('cond')
        body = match.group('body')
        task_matches = re.findall(r'GetTask\s*\(\s*(\d+)\s*\)\s*==\s*(\d+)', cond)
        if not task_matches:
            return None
        branch_task_ids = {int(tid) for tid, _value in task_matches}
        if len(branch_task_ids) != 1:
            return None
        branch_task_id = next(iter(branch_task_ids))
        if task_id is None:
            task_id = branch_task_id
        elif task_id != branch_task_id:
            return None
        values = [int(value) for _tid, value in task_matches]
        set_pos = int_args(parse_lua_calls(body, 'SetPos', limit=4), 2)
        if len(set_pos) != 1:
            return None
        messages = parse_lua_calls(body, 'Msg2Player', limit=2)
        if len(messages) > 1:
            return None
        message = clean_user_message(messages[0][0]) if messages and messages[0] else ''
        if message and not is_safe_user_message(message):
            return None
        branches.append({
            'values': values,
            'targetCellX': set_pos[0][0],
            'targetCellY': set_pos[0][1],
            'message': message,
        })
    if task_id is None or not branches:
        return None
    return {'taskId': task_id, 'taskBranches': branches}


def citywar_camp_gate_setpos(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    allowed = {
        'main', 'Include', 'GetFightState', 'SetPos', 'SetFightState',
        'bt_RankEffect', 'BT_GetData', 'if', 'GetCurCamp', 'Msg2Player'
    }
    if 'citywar_city' not in clean_source or 'bt_RankEffect(BT_GetData(PL_CURRANK))' not in clean_source:
        return None
    if not source_uses_only_calls(clean_source, allowed):
        return None
    if re.search(r'\b(for|while|elseif)\b', clean_source):
        return None
    positions = int_args(parse_lua_calls(clean_source, 'SetPos', limit=8), 2)
    fight_states = int_args(parse_lua_calls(clean_source, 'SetFightState', limit=8), 1)
    if len(positions) != 3 or len(fight_states) != 2:
        return None
    if positions[0] != positions[1] or fight_states[0][0] != 1 or fight_states[1][0] != 0:
        return None
    camp_match = re.search(r'GetCurCamp\s*\(\s*\)\s*~=\s*(\d+)', clean_source)
    if camp_match is None:
        return None
    message = single_string(parse_lua_calls(clean_source, 'Msg2Player', limit=2))
    if not is_safe_user_message(message):
        return None
    return {
        'requiredCamp': int(camp_match.group(1)),
        'ifFightState': 0,
        'enterCellX': positions[0][0],
        'enterCellY': positions[0][1],
        'enterNextFightState': fight_states[0][0],
        'blockedCellX': positions[1][0],
        'blockedCellY': positions[1][1],
        'blockedMessage': message,
        'exitCellX': positions[2][0],
        'exitCellY': positions[2][1],
        'exitNextFightState': fight_states[1][0],
        'applyRankEffectOnEnter': True,
    }


def citywar_camp_return_newworld(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    allowed = {'main', 'if', 'GetCurCamp', 'Msg2Player', 'SetCurCamp', 'GetCamp', 'SetFightState', 'SetLogoutRV', 'NewWorld'}
    if 'SetCurCamp(GetCamp())' not in clean_source or 'SetLogoutRV(0)' not in clean_source:
        return None
    if not source_uses_only_calls(clean_source, allowed):
        return None
    if re.search(r'\b(for|while|elseif)\b', clean_source):
        return None
    camp_match = re.search(r'GetCurCamp\s*\(\s*\)\s*~=\s*(\d+)', clean_source)
    if camp_match is None:
        return None
    new_world = int_args_unique(parse_lua_calls(clean_source, 'NewWorld', limit=4), 3)
    fight_state = int_args_unique(parse_lua_calls(clean_source, 'SetFightState', limit=4), 1)
    logout = int_args_unique(parse_lua_calls(clean_source, 'SetLogoutRV', limit=4), 1)
    if new_world is None or fight_state is None or logout is None:
        return None
    message = single_string(parse_lua_calls(clean_source, 'Msg2Player', limit=2))
    if not is_safe_user_message(message):
        return None
    return {
        'requiredCamp': int(camp_match.group(1)),
        'targetMapId': new_world[0],
        'targetCellX': new_world[1],
        'targetCellY': new_world[2],
        'fightState': fight_state[0],
        'logoutRv': logout[0],
        'resetCurCampToOriginal': True,
        'blockedMessage': message,
    }


def clearskill_constants(pc_root: Path = PC_ROOT) -> dict[str, Any]:
    path = server_root(pc_root) / 'script/missions/clearskill/head.lua'
    try:
        text = decode_legacy_text(path.read_bytes())
    except Exception:
        return {}

    def source_line(name: str) -> str:
        return next((ln for ln in text.splitlines() if re.search(r'\b' + re.escape(name) + r'\b\s*=', ln)), '')

    def flat_table(name: str) -> list[int]:
        return [int(v) for v in re.findall(r'\d+', source_line(name))]

    def nested_table(name: str) -> list[list[int]]:
        rows = re.findall(r'\{([^{}]+)\}', source_line(name))
        return [[int(v) for v in re.findall(r'\d+', row)] for row in rows]

    revive_match = re.search(r'\bCSP_RevieSWID\s*=\s*(\d+)', text)
    return {
        'clearMapTab': flat_table('CSP_ClearMapTab'),
        'testMapBeginTab': flat_table('CSP_TestMapBeginTab'),
        'clearHoleTab': nested_table('CSP_ClearHoleTab'),
        'clearTrapTab': nested_table('CSP_ClearTrapTab'),
        'reviveSubWorldId': int(revive_match.group(1)) if revive_match else 1,
        'testMapCount': 10,
    }


def clearskill_switch_trap(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    if not source_uses_only_calls(clean_source, {'main', 'Include', 'CSP_SwitchTrap'}):
        return None
    trap_index = int_args_unique(parse_lua_calls(clean_source, 'CSP_SwitchTrap', limit=2), 1)
    if trap_index is None:
        return None
    constants = clearskill_constants()
    rows = constants.get('clearTrapTab') or []
    index = trap_index[0]
    if index < 1 or index > len(rows) or len(rows[index - 1]) < 4:
        return None
    x1, y1, x2, y2 = rows[index - 1][:4]
    return {
        'trapIndex': index,
        'ifFightState': 0,
        'enterCellX': x1,
        'enterCellY': y1,
        'enterNextFightState': 1,
        'pkFlag': 0,
        'forbidChangePk': 1,
        'punish': 0,
        'logoutRv': 1,
        'exitCellX': x2,
        'exitCellY': y2,
        'exitNextFightState': 0,
        'exitPkFlag': 1,
        'exitForbidChangePk': 0,
    }


def clearskill_leave_game(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    if not source_uses_only_calls(clean_source, {'main', 'Include', 'LeaveGame'}):
        return None
    trap_index = int_args_unique(parse_lua_calls(clean_source, 'LeaveGame', limit=2), 1)
    if trap_index is None:
        return None
    constants = clearskill_constants()
    rows = constants.get('clearHoleTab') or []
    index = trap_index[0]
    if index < 1 or index > len(rows) or len(rows[index - 1]) < 2:
        return None
    x, y = rows[index - 1][:2]
    clear_maps = constants.get('clearMapTab') or []
    test_begins = constants.get('testMapBeginTab') or []
    if not clear_maps or not test_begins:
        return None
    return {
        'trapIndex': index,
        'fightState': 1,
        'pkFlag': 0,
        'forbidChangePk': 1,
        'punish': 0,
        'logoutRv': 1,
        'setTaskTempId': 100,
        'setTaskTempValue': 0,
        'deathScript': '',
        'reviveSubWorldId': constants.get('reviveSubWorldId', 1),
        'enterCellX': x,
        'enterCellY': y,
        'clearSkillClearMapIds': clear_maps,
        'clearSkillTestMapBeginIds': test_begins,
        'clearSkillTestMapCount': constants.get('testMapCount', 10),
    }


def cs_arena_constants(pc_root: Path = PC_ROOT) -> dict[str, Any]:
    root = server_root(pc_root)
    for src in build_script_hash_index(root).values():
        rel = str(src.get('sourceRelPath', '')).lower()
        script_path = str(src.get('scriptPath', ''))
        if not rel.endswith('head.lua') or 'cs竞技场' not in script_path:
            continue
        try:
            text = decode_legacy_text(Path(src['sourceFile']).read_bytes())
        except Exception:
            continue
        if 'CS_RevId' not in text or 'CS_RevData' not in text or 'GetLeavePos' not in text:
            continue
        rev_id = re.search(r'\bCS_RevId\s*=\s*(\d+)', text)
        rev_data = re.search(r'\bCS_RevData\s*=\s*(\d+)', text)
        leave = re.search(r'function\s+GetLeavePos\s*\(\s*\)(.*?)end\s*;?', text, re.S)
        task_ids = [int(v) for v in re.findall(r'GetTask\s*\(\s*(\d+)\s*\)', leave.group(1) if leave else '')]
        if rev_id and rev_data and len(task_ids) >= 3:
            return {
                'reviveMapId': int(rev_id.group(1)),
                'reviveSubWorldId': int(rev_data.group(1)),
                'leaveTaskIds': task_ids[:3],
            }
    return {}


def normalize_lua_engine_path(path: str) -> str:
    normalized = str(path).replace('/', '\\')
    while '\\\\' in normalized:
        normalized = normalized.replace('\\\\', '\\')
    return normalized


def read_pc_script_by_engine_path(engine_path: str, pc_root: Path = PC_ROOT) -> str:
    wanted = normalize_lua_engine_path(engine_path).lower()
    root = server_root(pc_root)
    for src in build_script_hash_index(root).values():
        if str(src.get('scriptPath', '')).lower() != wanted:
            continue
        try:
            return decode_legacy_text(Path(src['sourceFile']).read_bytes())
        except Exception:
            return ''
    return ''


def task_triplet_leave_trap(source: str, pc_root: Path = PC_ROOT) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    effective_source = clean_source
    if not re.search(r'function\s+main\s*\(', clean_source):
        includes = parse_lua_calls(clean_source, 'Include', limit=4)
        include_paths = [normalize_lua_engine_path(str(call[0])) for call in includes if call]
        if len(include_paths) != 1 or include_paths[0].lower() != r'\script\missions\citywar_arena\leavetrap.lua':
            return None
        effective_source = strip_lua_line_comments(read_pc_script_by_engine_path(include_paths[0], pc_root))
        if not effective_source:
            return None

    allowed = {
        'main', 'Include', 'SetCurCamp', 'GetCamp', 'SetFightState', 'SetRevPos',
        'SetLogoutRV', 'SetCreateTeam', 'SetDeathScript', 'SetPKFlag',
        'ForbidChangePK', 'SetTaskTemp', 'NewWorld', 'GetLeavePos'
    }
    if 'LeaveTeam' in effective_source:
        return None
    if not re.search(r'NewWorld\s*\(\s*GetLeavePos\s*\(\s*\)\s*\)', effective_source):
        return None
    if 'SetCurCamp(GetCamp())' not in effective_source:
        return None
    if not source_uses_only_calls(effective_source, allowed):
        return None
    if re.search(r'\b(if|for|while|elseif)\b', effective_source):
        return None

    fight_state = int_args_unique(parse_lua_calls(effective_source, 'SetFightState', limit=2), 1)
    revive_pos = int_args_unique(parse_lua_calls(effective_source, 'SetRevPos', limit=2), 2)
    if fight_state is None or revive_pos is None:
        return None

    set_task_temp = int_args_unique(parse_lua_calls(effective_source, 'SetTaskTemp', limit=2), 2)
    logout = int_args_unique(parse_lua_calls(effective_source, 'SetLogoutRV', limit=2), 1) if 'SetLogoutRV' in effective_source else None
    create_team = int_args_unique(parse_lua_calls(effective_source, 'SetCreateTeam', limit=2), 1) if 'SetCreateTeam' in effective_source else None
    pk_flag = int_args_unique(parse_lua_calls(effective_source, 'SetPKFlag', limit=2), 1) if 'SetPKFlag' in effective_source else None
    forbid = int_args_unique(parse_lua_calls(effective_source, 'ForbidChangePK', limit=2), 1) if 'ForbidChangePK' in effective_source else None
    death_calls = parse_lua_calls(effective_source, 'SetDeathScript', limit=2)
    death_script = str(death_calls[0][0]) if death_calls and death_calls[0] else None
    return {
        'fightState': fight_state[0],
        'reviveMapId': revive_pos[0],
        'reviveSubWorldId': revive_pos[1],
        'logoutRv': logout[0] if logout is not None else -1,
        'createTeam': create_team[0] if create_team is not None else -1,
        'pkFlag': pk_flag[0] if pk_flag is not None else -1,
        'forbidChangePk': forbid[0] if forbid is not None else -1,
        'setTaskTempId': set_task_temp[0] if set_task_temp is not None else 0,
        'setTaskTempValue': set_task_temp[1] if set_task_temp is not None else 0,
        'deathScript': death_script,
        'leaveMapTaskId': 300,
        'leaveCellXTaskId': 301,
        'leaveCellYTaskId': 302,
    }


def cs_arena_leave_trap(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    allowed = {
        'main', 'Include', 'LeaveTeam', 'SetCurCamp', 'GetCamp', 'SetFightState',
        'SetLogoutRV', 'SetRevPos', 'NewWorld', 'GetLeavePos'
    }
    if not re.search(r'NewWorld\s*\(\s*GetLeavePos\s*\(\s*\)\s*\)', clean_source):
        return None
    if 'SetCurCamp(GetCamp())' not in clean_source:
        return None
    if not source_uses_only_calls(clean_source, allowed):
        return None
    if re.search(r'\b(if|for|while|elseif)\b', clean_source):
        return None
    fight_state = int_args_unique(parse_lua_calls(clean_source, 'SetFightState', limit=2), 1)
    logout = int_args_unique(parse_lua_calls(clean_source, 'SetLogoutRV', limit=2), 1)
    if fight_state is None or logout is None:
        return None
    constants = cs_arena_constants()
    task_ids = constants.get('leaveTaskIds') or []
    if len(task_ids) < 3 or constants.get('reviveMapId', 0) <= 0:
        return None
    return {
        'fightState': fight_state[0],
        'logoutRv': logout[0],
        'reviveMapId': constants['reviveMapId'],
        'reviveSubWorldId': constants.get('reviveSubWorldId', 0),
        'leaveMapTaskId': task_ids[0],
        'leaveCellXTaskId': task_ids[1],
        'leaveCellYTaskId': task_ids[2],
    }


def is_safe_pickup_message(actions: dict[str, Any]) -> bool:
    if not actions.get('msg2PlayerCalls'):
        return False
    if not is_safe_user_message(single_string(actions.get('msg2PlayerCalls'))):
        return False
    if actions.get('newWorldCalls') or actions.get('setPosCalls') or actions.get('setFightStateCalls'):
        return False
    if actions.get('talkCalls') or actions.get('sayCalls'):
        return False
    if actions.get('getTaskCalls') or actions.get('setTaskCalls') or actions.get('haveItemCalls'):
        return False
    if actions.get('addItemCalls') or actions.get('delItemCalls'):
        return False
    if actions.get('usesCityApis'):
        return False
    return bool(actions.get('addEventItemCalls') or actions.get('addNoteCalls') or actions.get('setPropStateCalls'))


def object_open_box_action(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    if not source_uses_only_calls(clean_source, {'main', 'OpenBox', 'SetRevPos'}):
        return None
    if has_lua_control_flow(clean_source):
        return None
    if len(parse_lua_calls(clean_source, 'OpenBox', limit=2)) != 1:
        return None
    revive_calls = parse_lua_calls(clean_source, 'SetRevPos', limit=2)
    revive_id = 0
    if revive_calls:
        revive = int_args_unique(revive_calls, 1)
        if revive is None:
            return None
        revive_id = revive[0]
    return {'reviveId': revive_id}


def object_faction_open_box_action(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    if not source_uses_only_calls(clean_source, {'main', 'OpenBox', 'GetFaction', 'SetRevPos', 'if'}):
        return None
    if re.search(r'\b(elseif|else|for|while|repeat)\b', clean_source):
        return None
    if len(parse_lua_calls(clean_source, 'OpenBox', limit=2)) != 1:
        return None
    if len(parse_lua_calls(clean_source, 'GetFaction', limit=2)) != 1:
        return None
    if len(parse_lua_calls(clean_source, 'SetRevPos', limit=2)) != 1:
        return None
    if_match = re.search(
        r'if\s*\(?\s*GetFaction\s*\(\s*\)\s*==\s*["\']([a-z]+)["\']\s*\)?\s*then(?P<body>.*?)end',
        clean_source, re.S | re.I)
    if not if_match:
        return None
    faction = if_match.group(1).lower()
    faction_id = PC_FACTION_IDS.get(faction)
    if faction_id is None:
        return None
    revive = int_args_unique(parse_lua_calls(if_match.group('body'), 'SetRevPos', limit=2), 1)
    if revive is None:
        return None
    return {'requiredFaction': faction, 'requiredFactionId': faction_id, 'reviveId': revive[0]}


def int_lua_constant_expr(text: str) -> int | None:
    text = str(text).strip()
    if re.fullmatch(r'\d+', text):
        return int(text)
    match = re.fullmatch(r'(\d+)\s*\*\s*(\d+)\s*([+-])\s*(\d+)', text)
    if match:
        base = int(match.group(1)) * int(match.group(2))
        delta = int(match.group(4))
        return base + delta if match.group(3) == '+' else base - delta
    match = re.fullmatch(r'(\d+)\s*\*\s*(\d+)', text)
    if match:
        return int(match.group(1)) * int(match.group(2))
    return None


def object_task_optional_pickup_message_action(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    if not source_uses_only_calls(clean_source, {'main', 'SetPropState', 'AddEventItem', 'Msg2Player', 'GetTask', 'AddNote', 'if', 'and'}):
        return None
    if re.search(r'\b(elseif|else|for|while|repeat)\b', clean_source):
        return None
    if len(parse_lua_calls(clean_source, 'SetPropState', limit=2)) != 1:
        return None
    event_items = int_args(parse_lua_calls(clean_source, 'AddEventItem', limit=2), 1)
    if len(event_items) != 1:
        return None
    msg = single_string(parse_lua_calls(clean_source, 'Msg2Player', limit=2))
    if not is_safe_user_message(msg):
        return None
    if len(parse_lua_calls(clean_source, 'AddNote', limit=2)) != 1:
        return None
    match = re.search(
        r'if\s*\(?\s*GetTask\s*\(\s*(\d+)\s*\)\s*>\s*([0-9\s*+\-]+)\s*\)?\s*and\s*\(?\s*GetTask\s*\(\s*\1\s*\)\s*<\s*([0-9\s*+\-]+)\s*\)?\s*then(?P<body>.*?)end',
        clean_source, re.S | re.I)
    if not match:
        return None
    min_value = int_lua_constant_expr(match.group(2))
    max_value = int_lua_constant_expr(match.group(3))
    if min_value is None or max_value is None or min_value >= max_value:
        return None
    task_notes = [call[0] for call in parse_lua_calls(match.group('body'), 'AddNote', limit=2) if call]
    task_notes = [clean_user_message(note) for note in task_notes if is_safe_user_message(clean_user_message(note))]
    if not task_notes:
        return None
    return {
        'message': msg,
        'eventItemIds': [event_items[0][0]],
        'setPropState': True,
        'noteTaskId': int(match.group(1)),
        'noteTaskMinExclusive': min_value,
        'noteTaskMaxExclusive': max_value,
        'taskNotes': task_notes,
    }


def object_task_talk_message_action(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    if not source_uses_only_calls(clean_source, {'main', 'GetTask', 'Talk', 'if'}):
        return None
    if re.search(r'\b(elseif|for|while|repeat)\b', clean_source):
        return None
    if len(parse_lua_calls(clean_source, 'GetTask', limit=2)) != 1:
        return None
    if len(parse_lua_calls(clean_source, 'Talk', limit=3)) != 2:
        return None
    match = re.search(
        r'if\s*\(?\s*GetTask\s*\(\s*(\d+)\s*\)\s*==\s*([0-9\s*+\-]+)\s*\)?\s*then(?P<then>.*?)else(?P<else>.*?)end',
        clean_source, re.S | re.I)
    if not match:
        return None
    task_value = int_lua_constant_expr(match.group(2))
    if task_value is None:
        return None
    then_messages = talk_messages({'talkCalls': parse_lua_calls(match.group('then'), 'Talk', limit=2)})
    else_messages = talk_messages({'talkCalls': parse_lua_calls(match.group('else'), 'Talk', limit=2)})
    if not then_messages or not else_messages:
        return None
    return {
        'taskId': int(match.group(1)),
        'taskValue': task_value,
        'messages': then_messages,
        'elseMessages': else_messages,
    }


def object_camp_open_box_action(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    if not source_uses_only_calls(clean_source, {'main', 'GetCurCamp', 'OpenBox', 'Talk', 'if'}):
        return None
    if re.search(r'\b(elseif|for|while|repeat)\b', clean_source):
        return None
    if len(parse_lua_calls(clean_source, 'GetCurCamp', limit=2)) != 1:
        return None
    if len(parse_lua_calls(clean_source, 'OpenBox', limit=2)) != 1:
        return None
    if len(parse_lua_calls(clean_source, 'Talk', limit=2)) != 1:
        return None
    match = re.search(
        r'if\s*\(?\s*GetCurCamp\s*\(\s*\)\s*==\s*(\d+)\s*\)?\s*then(?P<then>.*?)else(?P<else>.*?)end',
        clean_source, re.S | re.I)
    if not match:
        return None
    if len(parse_lua_calls(match.group('then'), 'OpenBox', limit=2)) != 1:
        return None
    talk_calls = parse_lua_calls(match.group('else'), 'Talk', limit=2)
    messages = talk_messages({'talkCalls': talk_calls})
    if len(messages) != 1:
        return None
    return {'requiredCamp': int(match.group(1)), 'message': messages[0]}


def object_show_ladder_action(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    if not source_uses_only_calls(clean_source, {'main', 'ShowLadder'}):
        return None
    if has_lua_control_flow(clean_source):
        return None
    calls = parse_lua_calls(clean_source, 'ShowLadder', limit=2)
    if len(calls) != 1 or not calls[0]:
        return None
    ids: list[int] = []
    for arg in calls[0]:
        parsed = int_expr(arg)
        if parsed is None:
            return None
        ids.append(parsed)
    return {'ladderIds': ids}


def build_object_action_catalog(object_scripts: list[dict[str, Any]]) -> tuple[list[dict[str, Any]], dict[str, Any]]:
    entries: list[dict[str, Any]] = []
    for script in object_scripts:
        actions = script.get('actions') or {}
        source_text = script.get('sourceText', '')
        if not script.get('resolved') or not actions.get('hasMain'):
            continue
        fight_state = int_args_unique(actions.get('setFightStateCalls') or [], 1)
        fight_value = fight_state[0] if fight_state is not None else -1
        new_world = int_args_unique(actions.get('newWorldCalls') or [], 3)
        if new_world and not (actions.get('talks') or actions.get('sayCalls') or actions.get('usesTaskApis') or actions.get('usesItemApis') or actions.get('usesObjectApis') or actions.get('usesCityApis')):
            entries.append({
                'scriptPath': script.get('scriptPath', ''),
                'scriptId': script.get('scriptId', 0),
                'scriptIdHex': script.get('scriptIdHex', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'NewWorld',
                'targetMapId': new_world[0],
                'targetCellX': new_world[1],
                'targetCellY': new_world[2],
                'fightState': fight_value,
                'source': 'PC object Lua main(): deterministic NewWorld with optional SetFightState and no dialog/task/item/object side effects',
            })
            continue
        if is_safe_pickup_message(actions):
            entries.append({
                'scriptPath': script.get('scriptPath', ''),
                'scriptId': script.get('scriptId', 0),
                'scriptIdHex': script.get('scriptIdHex', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'PickupMessage',
                'targetMapId': 0,
                'targetCellX': 0,
                'targetCellY': 0,
                'fightState': -1,
                'message': single_string(actions.get('msg2PlayerCalls') or []),
                'eventItemIds': [values[0] for values in int_args(actions.get('addEventItemCalls') or [], 1)],
                'notes': [call[0] for call in (actions.get('addNoteCalls') or []) if call],
                'setPropState': bool(actions.get('setPropStateCalls')),
                'source': 'PC object Lua main(): deterministic SetPropState/AddEventItem/AddNote/Msg2Player with no Talk/Say/GetTask/SetTask/HaveItem/DelItem branch',
            })
            continue
        if is_safe_say_message(actions):
            entries.append({
                'scriptPath': script.get('scriptPath', ''),
                'scriptId': script.get('scriptId', 0),
                'scriptIdHex': script.get('scriptIdHex', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'SayMessage',
                'targetMapId': 0,
                'targetCellX': 0,
                'targetCellY': 0,
                'fightState': -1,
                'message': single_string(actions.get('sayCalls') or []),
                'source': 'PC object Lua main(): deterministic read-only Say(message,0) with no Talk/Msg2Player/task/item/object/warp branch',
            })
            continue
        open_box = object_open_box_action(source_text)
        if open_box is not None:
            entries.append({
                'scriptPath': script.get('scriptPath', ''),
                'scriptId': script.get('scriptId', 0),
                'scriptIdHex': script.get('scriptIdHex', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'OpenBox',
                'targetMapId': 0,
                'targetCellX': 0,
                'targetCellY': 0,
                'fightState': -1,
                'source': 'PC object Lua main(): deterministic OpenBox() with optional one-arg SetRevPos(id), no branch/task/item/faction side effects',
                **open_box,
            })
            continue
        faction_open_box = object_faction_open_box_action(source_text)
        if faction_open_box is not None:
            entries.append({
                'scriptPath': script.get('scriptPath', ''),
                'scriptId': script.get('scriptId', 0),
                'scriptIdHex': script.get('scriptIdHex', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'FactionOpenBox',
                'targetMapId': 0,
                'targetCellX': 0,
                'targetCellY': 0,
                'fightState': -1,
                'source': 'PC object Lua main(): OpenBox() always executes; SetRevPos(id) only when GetFaction() equals required PC faction',
                **faction_open_box,
            })
            continue
        camp_open_box = object_camp_open_box_action(source_text)
        if camp_open_box is not None:
            entries.append({
                'scriptPath': script.get('scriptPath', ''),
                'scriptId': script.get('scriptId', 0),
                'scriptIdHex': script.get('scriptIdHex', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'CampOpenBox',
                'targetMapId': 0,
                'targetCellX': 0,
                'targetCellY': 0,
                'fightState': -1,
                'source': 'PC object Lua main(): battlefield GetCurCamp() gate opens storage box on matching camp; otherwise posts PC Talk message',
                **camp_open_box,
            })
            continue
        task_optional_pickup = object_task_optional_pickup_message_action(source_text)
        if task_optional_pickup is not None:
            entries.append({
                'scriptPath': script.get('scriptPath', ''),
                'scriptId': script.get('scriptId', 0),
                'scriptIdHex': script.get('scriptIdHex', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'TaskOptionalPickupMessage',
                'targetMapId': 0,
                'targetCellX': 0,
                'targetCellY': 0,
                'fightState': -1,
                'source': 'PC object Lua main(): deterministic pickup plus AddNote only when GetTask(id) is inside PC range, no task mutation',
                **task_optional_pickup,
            })
            continue
        task_talk = object_task_talk_message_action(source_text)
        if task_talk is not None:
            entries.append({
                'scriptPath': script.get('scriptPath', ''),
                'scriptId': script.get('scriptId', 0),
                'scriptIdHex': script.get('scriptIdHex', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'TaskTalkMessage',
                'targetMapId': 0,
                'targetCellX': 0,
                'targetCellY': 0,
                'fightState': -1,
                'source': 'PC object Lua main(): read-only Talk branch selected by GetTask(id)==value, no task/item/object mutation',
                **task_talk,
            })
            continue
        show_ladder = object_show_ladder_action(source_text)
        if show_ladder is not None:
            entries.append({
                'scriptPath': script.get('scriptPath', ''),
                'scriptId': script.get('scriptId', 0),
                'scriptIdHex': script.get('scriptIdHex', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'ShowLadder',
                'targetMapId': 0,
                'targetCellX': 0,
                'targetCellY': 0,
                'fightState': -1,
                'source': 'PC object Lua main(): deterministic ShowLadder(id...) with no branch/task/item/faction side effects',
                **show_ladder,
            })
            continue
        talk_lines = talk_messages(actions)
        if is_safe_talk_message(actions, source_text):
            entries.append({
                'scriptPath': script.get('scriptPath', ''),
                'scriptId': script.get('scriptId', 0),
                'scriptIdHex': script.get('scriptIdHex', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'TalkMessage',
                'targetMapId': 0,
                'targetCellX': 0,
                'targetCellY': 0,
                'fightState': -1,
                'message': '\n'.join(talk_lines),
                'messages': talk_lines,
                'source': 'PC object Lua main(): deterministic read-only Talk(count,"",message...) with no conditional/API branch',
            })
    coverage = {
        'deterministicObjectActions': len(entries),
        'deterministicObjectNewWorldActions': sum(1 for e in entries if e['actionKind'] == 'NewWorld'),
        'deterministicObjectPickupMessageActions': sum(1 for e in entries if e['actionKind'] == 'PickupMessage'),
        'deterministicObjectTaskOptionalPickupMessageActions': sum(1 for e in entries if e['actionKind'] == 'TaskOptionalPickupMessage'),
        'deterministicObjectSayMessageActions': sum(1 for e in entries if e['actionKind'] == 'SayMessage'),
        'deterministicObjectTalkMessageActions': sum(1 for e in entries if e['actionKind'] == 'TalkMessage'),
        'deterministicObjectTaskTalkMessageActions': sum(1 for e in entries if e['actionKind'] == 'TaskTalkMessage'),
        'deterministicObjectOpenBoxActions': sum(1 for e in entries if e['actionKind'] == 'OpenBox'),
        'deterministicObjectFactionOpenBoxActions': sum(1 for e in entries if e['actionKind'] == 'FactionOpenBox'),
        'deterministicObjectCampOpenBoxActions': sum(1 for e in entries if e['actionKind'] == 'CampOpenBox'),
        'deterministicObjectShowLadderActions': sum(1 for e in entries if e['actionKind'] == 'ShowLadder'),
    }
    return entries, coverage


def task_faction_gate_newworld_trap(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    if not source_uses_only_calls(clean_source, {'main', 'GetTask', 'GetFaction', 'NewWorld', 'SetFightState', 'Talk', 'SetPos', 'if', 'elseif', 'and'}):
        return None
    if re.search(r'\b(for|while|repeat)\b', clean_source):
        return None
    if 'elseif' not in clean_source or 'else' not in clean_source:
        return None
    match = re.search(
        r'(?P<var>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*GetTask\s*\(\s*(?P<taskId>\d+)\s*\).*?'
        r'if\s*\(?\s*(?P=var)\s*>=\s*(?P<passMin>[0-9\s*+\-]+)\s*\)?\s*and\s*\(?\s*GetFaction\s*\(\s*\)\s*==\s*["\'](?P<faction>[a-z]+)["\']\s*\)?\s*then(?P<pass>.*?)'
        r'elseif\s*\(?\s*(?P=var)\s*>\s*(?P<midMin>[0-9\s*+\-]+)\s*\)?\s*and\s*\(?\s*(?P=var)\s*<\s*(?P<midMax>[0-9\s*+\-]+)\s*\)?\s*then(?P<mid>.*?)'
        r'else(?P<fail>.*?)end',
        clean_source, re.S | re.I)
    if not match:
        return None
    task_id = int(match.group('taskId'))
    pass_min = int_lua_constant_expr(match.group('passMin'))
    faction = match.group('faction').lower()
    faction_id = PC_FACTION_IDS.get(faction)
    mid_min = int_lua_constant_expr(match.group('midMin'))
    mid_max = int_lua_constant_expr(match.group('midMax'))
    if pass_min is None or mid_min is None or mid_max is None or faction_id is None:
        return None
    new_world = int_args_unique(parse_lua_calls(match.group('pass'), 'NewWorld', limit=2), 3)
    fight_state = int_args_unique(parse_lua_calls(match.group('pass'), 'SetFightState', limit=2), 1)
    mid_pos = int_args_unique(parse_lua_calls(match.group('mid'), 'SetPos', limit=2), 2)
    fail_pos = int_args_unique(parse_lua_calls(match.group('fail'), 'SetPos', limit=2), 2)
    if new_world is None or fight_state is None or mid_pos is None or fail_pos is None or mid_pos != fail_pos:
        return None
    mid_messages = talk_messages({'talkCalls': parse_lua_calls(match.group('mid'), 'Talk', limit=2)})
    fail_messages = talk_messages({'talkCalls': parse_lua_calls(match.group('fail'), 'Talk', limit=2)})
    if len(mid_messages) != 1 or len(fail_messages) != 1:
        return None
    return {
        'taskId': task_id,
        'passTaskMinInclusive': pass_min,
        'midTaskMinExclusive': mid_min,
        'midTaskMaxExclusive': mid_max,
        'requiredFaction': faction,
        'requiredFactionId': faction_id,
        'targetMapId': new_world[0],
        'targetCellX': new_world[1],
        'targetCellY': new_world[2],
        'fightState': fight_state[0],
        'failTargetCellX': mid_pos[0],
        'failTargetCellY': mid_pos[1],
        'message': mid_messages[0],
        'blockedMessage': fail_messages[0],
    }


def task_prompt_default_newworld_trap(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    main_body = lua_function_body(clean_source, 'main')
    if not main_body:
        return None
    main_calls = set(re.findall(r'\b([A-Za-z_][A-Za-z0-9_]*)\s*\(', main_body))
    if not main_calls.issubset({'GetTask', 'Say', 'Talk', 'enter_cave', 'if', 'elseif'}):
        return None
    if 'elseif' not in main_body or 'else' not in main_body or 'enter_cave()' not in main_body:
        return None
    match = re.search(
        r'if\s*\(?\s*GetTask\s*\(\s*(?P<taskId>\d+)\s*\)\s*==\s*(?P<sayValue>\d+)\s*\)?\s*then(?P<sayBranch>.*?)'
        r'elseif\s*\(?\s*GetTask\s*\(\s*(?P=taskId)\s*\)\s*==\s*(?P<talkValue>\d+)\s*\)?\s*then(?P<talkBranch>.*?)'
        r'else\s*enter_cave\s*\(\s*\)\s*end',
        main_body, re.S | re.I)
    if not match:
        return None
    say_messages = [clean_user_message(str(c[0])) for c in parse_lua_calls(match.group('sayBranch'), 'Say', limit=2) if c]
    say_messages = [m for m in say_messages if m and is_safe_user_message(m)]
    talk_messages_ = callback_talk_messages(parse_lua_calls(match.group('talkBranch'), 'Talk', limit=2))
    if len(say_messages) != 1 or len(talk_messages_) != 1:
        return None
    enter_body = lua_function_body(clean_source, 'enter_cave')
    if not enter_body:
        return None
    enter_calls = set(re.findall(r'\b([A-Za-z_][A-Za-z0-9_]*)\s*\(', enter_body))
    if not enter_calls.issubset({'SetFightState', 'NewWorld', 'AddTermini'}):
        return None
    new_world = int_args_unique(parse_lua_calls(enter_body, 'NewWorld', limit=2), 3)
    fight_state = int_args_unique(parse_lua_calls(enter_body, 'SetFightState', limit=2), 1)
    termini = expr_args(parse_lua_calls(enter_body, 'AddTermini', limit=4), 1)
    if new_world is None or fight_state is None:
        return None
    return {
        'taskId': int(match.group('taskId')),
        'taskBranches': [
            {
                'values': [int(match.group('sayValue'))],
                'message': say_messages[0],
            },
            {
                'values': [int(match.group('talkValue'))],
                'message': talk_messages_[0],
            },
        ],
        'targetMapId': new_world[0],
        'targetCellX': new_world[1],
        'targetCellY': new_world[2],
        'fightState': fight_state[0],
        'terminiIds': [value[0] for value in termini],
    }


def task_faction_message_gate_newworld(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    allowed = {'main', 'GetTask', 'GetFaction', 'SetFightState', 'NewWorld', 'Talk', 'if', 'elseif', 'and'}
    if not source_uses_only_calls(clean_source, allowed):
        return None
    if re.search(r'\b(for|while|repeat)\b', clean_source):
        return None
    match = re.search(
        r'(?P<var>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*GetTask\s*\(\s*(?P<taskId>\d+)\s*\).*?'
        r'if\s*\(?\s*(?P=var)\s*>\s*(?P<passMin>[0-9\s*+\-]+)\s*\)?\s*and\s*\(?\s*GetFaction\s*\(\s*\)\s*==\s*["\'](?P<faction>[a-z]+)["\']\s*\)?\s*then(?P<pass>.*?)'
        r'elseif\s*\(?\s*(?P=var)\s*<=\s*(?P<lowMax>[0-9\s*+\-]+)\s*\)?\s*then(?P<low>.*?)'
        r'else(?P<blocked>.*?)end',
        clean_source, re.S | re.I)
    if not match:
        return None
    task_id = int(match.group('taskId'))
    pass_min_exclusive = int_lua_constant_expr(match.group('passMin'))
    low_max = int_lua_constant_expr(match.group('lowMax'))
    faction = match.group('faction').lower()
    faction_id = PC_FACTION_IDS.get(faction)
    if pass_min_exclusive is None or low_max is None or pass_min_exclusive != low_max or faction_id is None:
        return None
    new_world = int_args_unique(parse_lua_calls(match.group('pass'), 'NewWorld', limit=2), 3)
    fight_state = int_args_unique(parse_lua_calls(match.group('pass'), 'SetFightState', limit=2), 1)
    low_messages = talk_messages({'talkCalls': parse_lua_calls(match.group('low'), 'Talk', limit=2)})
    blocked_messages = talk_messages({'talkCalls': parse_lua_calls(match.group('blocked'), 'Talk', limit=2)})
    if new_world is None or fight_state is None or len(low_messages) != 1 or len(blocked_messages) != 1:
        return None
    return {
        'taskId': task_id,
        'passTaskMinInclusive': pass_min_exclusive + 1,
        'requiredFaction': faction,
        'requiredFactionId': faction_id,
        'targetMapId': new_world[0],
        'targetCellX': new_world[1],
        'targetCellY': new_world[2],
        'fightState': fight_state[0],
        'message': low_messages[0],
        'blockedMessage': blocked_messages[0],
    }


def task_faction_prompt_gate_newworld(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    main_body = lua_function_body(clean_source, 'main')
    if not main_body:
        return None
    if re.search(r'\b(for|while|repeat)\b', main_body):
        return None
    match = re.search(
        r'(?P<var>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*GetTask\s*\(\s*(?P<taskId>\d+)\s*\).*?'
        r'if\s*\(?\s*GetSeries\s*\(\s*\)\s*==\s*(?P<series>\d+)\s*\)?\s*and\s*\(?\s*GetFaction\s*\(\s*\)\s*==\s*["\'](?P<faction>[a-z]+)["\']\s*\)?\s*then\s*'
        r'if\s*\(?\s*(?P=var)\s*==\s*(?P<promptValue>[0-9\s*+\-]+)\s*\)?\s*then(?P<prompt>.*?)'
        r'elseif\s*\(?\s*(?P=var)\s*>=\s*(?P<passMin>[0-9\s*+\-]+)\s*\)?\s*then(?P<pass>.*?)'
        r'else(?P<low>.*?)end\s*else(?P<blocked>.*?)end',
        main_body, re.S | re.I)
    if not match:
        return None
    task_id = int(match.group('taskId'))
    required_series = int(match.group('series'))
    prompt_value = int_lua_constant_expr(match.group('promptValue'))
    pass_min = int_lua_constant_expr(match.group('passMin'))
    faction = match.group('faction').lower()
    faction_id = PC_FACTION_IDS.get(faction)
    if prompt_value is None or pass_min is None or prompt_value != pass_min or faction_id is None:
        return None
    prompt_messages = callback_talk_messages(parse_lua_calls(match.group('prompt'), 'Talk', limit=2))
    low_messages = [clean_user_message(str(call[0])) for call in parse_lua_calls(match.group('low'), 'Msg2Player', limit=2) if call]
    blocked_messages = [clean_user_message(str(call[0])) for call in parse_lua_calls(match.group('blocked'), 'Msg2Player', limit=2) if call]
    new_world = int_args_unique(parse_lua_calls(match.group('pass'), 'NewWorld', limit=2), 3)
    if (
        new_world is None or len(prompt_messages) != 1 or len(low_messages) != 1 or
        len(blocked_messages) != 1 or not is_safe_user_message(low_messages[0]) or
        not is_safe_user_message(blocked_messages[0])
    ):
        return None
    return {
        'taskId': task_id,
        'requiredSeries': required_series,
        'passTaskMinInclusive': pass_min,
        'requiredFaction': faction,
        'requiredFactionId': faction_id,
        'taskBranches': [{
            'values': [prompt_value],
            'message': prompt_messages[0],
        }],
        'targetMapId': new_world[0],
        'targetCellX': new_world[1],
        'targetCellY': new_world[2],
        'fightState': -1,
        'message': low_messages[0],
        'blockedMessage': blocked_messages[0],
    }


def task_current_map_return_newworld(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    if 'tab_cityid' not in clean_source or 'SubWorldIdx2ID' not in clean_source:
        return None
    if any(token in clean_source for token in ('SetTask', 'HaveItem', 'DelItem', 'AddItem', 'SetTaskTemp')):
        return None
    rows = [tuple(int(v) for v in match) for match in re.findall(
        r'\{\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\}', clean_source)]
    if not rows:
        return None
    main_body = lua_function_body(clean_source, 'main')
    back_town_body = lua_function_body(clean_source, 'back_town')
    back_mingyue_body = lua_function_body(clean_source, 'back_mingyue')
    if not main_body or not back_town_body or not back_mingyue_body:
        return None
    task_match = re.search(r'(?P<var>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*GetTask\s*\(\s*(?P<taskId>\d+)\s*\)', main_body)
    if not task_match:
        return None
    task_var = task_match.group('var')
    if not re.search(rf'\b{re.escape(task_var)}\s*~=\s*0', main_body):
        return None
    say_calls = parse_lua_calls(main_body, 'Say', limit=2)
    if len(say_calls) != 1 or not say_calls[0]:
        return None
    message = clean_user_message(str(say_calls[0][0]))
    if not is_safe_user_message(message):
        return None
    compact_main = re.sub(r'\s+', '', main_body)
    compact_back_town = re.sub(r'\s+', '', back_town_body)
    compact_back_mingyue = re.sub(r'\s+', '', back_mingyue_body)
    if 'NewWorld(tab_cityid[i][2],tab_cityid[i][3],tab_cityid[i][4])' not in compact_main:
        return None
    if 'NewWorld(tab_cityid[i][2],tab_cityid[i][3],tab_cityid[i][4])' not in compact_back_town:
        return None
    if 'NewWorld(tab_cityid[i][1],1565,3156)' not in compact_back_mingyue:
        return None
    return {
        'taskId': int(task_match.group('taskId')),
        'currentMapIds': [row[0] for row in rows],
        'currentTargetMapIds': [row[1] for row in rows],
        'currentTargetCellXs': [row[2] for row in rows],
        'currentTargetCellYs': [row[3] for row in rows],
        'targetMapId': rows[0][1],
        'targetCellX': rows[0][2],
        'targetCellY': rows[0][3],
        'fightState': -1,
        'message': message,
    }


def task_settask_faction_gate_newworld(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    if not source_uses_only_calls(clean_source, {
        'main', 'GetTask', 'GetFaction', 'NewWorld', 'SetFightState', 'SetTask', 'Talk', 'SetPos', 'AddNote', 'if', 'elseif', 'and'
    }):
        return None
    if re.search(r'\b(for|while|repeat)\b', clean_source):
        return None
    if 'elseif' not in clean_source or 'else' not in clean_source:
        return None
    match = re.search(
        r'(?P<altVar>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*GetTask\s*\(\s*(?P<altTaskId>\d+)\s*\).*?'
        r'(?P<var>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*GetTask\s*\(\s*(?P<taskId>\d+)\s*\).*?'
        r'if\s*\(?\s*(?P=var)\s*==\s*(?P<taskValue>[0-9\s*+\-]+)\s*\)?\s*then(?P<primary>.*?)'
        r'elseif\s*\(?\s*(?P=altVar)\s*>=\s*(?P<passMin>[0-9\s*+\-]+)\s*\)?\s*and\s*\(?\s*GetFaction\s*\(\s*\)\s*==\s*["\'](?P<faction>[a-z]+)["\']\s*\)?\s*then(?P<secondary>.*?)'
        r'else(?P<fail>.*?)end',
        clean_source, re.S | re.I)
    if not match:
        return None
    task_value = int_lua_constant_expr(match.group('taskValue'))
    pass_min = int_lua_constant_expr(match.group('passMin'))
    faction = match.group('faction').lower()
    faction_id = PC_FACTION_IDS.get(faction)
    if task_value is None or pass_min is None or faction_id is None:
        return None
    primary_new_world = int_args_unique(parse_lua_calls(match.group('primary'), 'NewWorld', limit=2), 3)
    secondary_new_world = int_args_unique(parse_lua_calls(match.group('secondary'), 'NewWorld', limit=2), 3)
    primary_fight = int_args_unique(parse_lua_calls(match.group('primary'), 'SetFightState', limit=2), 1)
    secondary_fight = int_args_unique(parse_lua_calls(match.group('secondary'), 'SetFightState', limit=2), 1)
    fail_pos = int_args_unique(parse_lua_calls(match.group('fail'), 'SetPos', limit=2), 2)
    if (
        primary_new_world is None or secondary_new_world is None or primary_new_world != secondary_new_world or
        primary_fight is None or secondary_fight is None or primary_fight != secondary_fight or fail_pos is None
    ):
        return None
    set_task_calls = parse_lua_calls(match.group('primary'), 'SetTask', limit=4)
    set_task_ids: list[int] = []
    set_task_values: list[int] = []
    for call in set_task_calls:
        if len(call) < 2 or not str(call[0]).strip().isdigit():
            return None
        value = int_lua_constant_expr(str(call[1]))
        if value is None:
            return None
        set_task_ids.append(int(str(call[0]).strip()))
        set_task_values.append(value)
    if not set_task_ids:
        return None
    fail_messages = talk_messages({'talkCalls': parse_lua_calls(match.group('fail'), 'Talk', limit=2)})
    notes = [clean_user_message(str(call[0])) for call in parse_lua_calls(match.group('fail'), 'AddNote', limit=2) if call]
    notes = [note for note in notes if note and is_safe_user_message(note)]
    if len(fail_messages) != 1:
        return None
    return {
        'taskId': int(match.group('taskId')),
        'taskValue': task_value,
        'alternateTaskId': int(match.group('altTaskId')),
        'passTaskMinInclusive': pass_min,
        'requiredFaction': faction,
        'requiredFactionId': faction_id,
        'targetMapId': primary_new_world[0],
        'targetCellX': primary_new_world[1],
        'targetCellY': primary_new_world[2],
        'fightState': primary_fight[0],
        'setTaskIds': set_task_ids,
        'setTaskValues': set_task_values,
        'failTargetCellX': fail_pos[0],
        'failTargetCellY': fail_pos[1],
        'message': fail_messages[0],
        'notes': notes,
    }


def task_settask_prompt_callback_newworld(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    main_body = lua_function_body(clean_source, 'main')
    if not main_body:
        return None
    if re.search(r'\b(for|while|repeat)\b', clean_source):
        return None
    if any(token in clean_source for token in (
        'HaveItem', 'DelItem', 'AddItem', 'AddEventItem', 'SetTaskTemp', 'GetFaction', 'GetSeries',
        'GetMission', 'GetTong', 'Team', 'Include', 'IncludeLib'
    )):
        return None
    main_calls = set(re.findall(r'\b([A-Za-z_][A-Za-z0-9_]*)\s*\(', main_body))
    if not main_calls.issubset({'GetTask', 'SetTask', 'Talk', 'if', 'elseif'}):
        return None
    task_match = re.search(r'(?P<var>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*GetTask\s*\(\s*(?P<taskId>\d+)\s*\)', main_body)
    if not task_match:
        return None
    task_var = task_match.group('var')
    branch_match = re.search(
        rf'if\s*\(?\s*{re.escape(task_var)}\s*==\s*(?P<firstValue>\d+)\s*\)?\s*then(?P<first>.*?)'
        rf'elseif\s*\(?\s*{re.escape(task_var)}\s*==\s*(?P<secondValue>\d+)\s*\)?\s*then(?P<second>.*?)end',
        main_body, re.S | re.I)
    if not branch_match:
        return None

    branches: list[dict[str, Any]] = []
    callback_name = ''
    for value_name, body_name in (('firstValue', 'first'), ('secondValue', 'second')):
        body = branch_match.group(body_name)
        talk_calls = parse_lua_calls(body, 'Talk', limit=2)
        if len(talk_calls) != 1 or len(talk_calls[0]) < 3:
            return None
        next_callback = str(talk_calls[0][1]).strip()
        if not next_callback:
            return None
        if callback_name and callback_name != next_callback:
            return None
        callback_name = next_callback
        messages = callback_talk_messages(talk_calls)
        if not messages:
            return None
        branch: dict[str, Any] = {'values': [int(branch_match.group(value_name))], 'messages': messages}
        set_task_calls = parse_lua_calls(body, 'SetTask', limit=4)
        if set_task_calls:
            set_task_ids: list[int] = []
            set_task_values: list[int] = []
            for call in set_task_calls:
                if len(call) < 2 or not str(call[0]).strip().isdigit():
                    return None
                parsed_value = int_lua_constant_expr(str(call[1]))
                if parsed_value is None:
                    return None
                set_task_ids.append(int(str(call[0]).strip()))
                set_task_values.append(parsed_value)
            branch['setTaskIds'] = set_task_ids
            branch['setTaskValues'] = set_task_values
        branches.append(branch)

    if not callback_name:
        return None
    callback_body = lua_function_body(clean_source, callback_name)
    if not callback_body:
        return None
    callback_calls = set(re.findall(r'\b([A-Za-z_][A-Za-z0-9_]*)\s*\(', callback_body))
    if not callback_calls.issubset({'NewWorld'}):
        return None
    target = int_args_unique(parse_lua_calls(callback_body, 'NewWorld', limit=2), 3)
    if target is None:
        return None
    return {
        'taskId': int(task_match.group('taskId')),
        'targetMapId': target[0],
        'targetCellX': target[1],
        'targetCellY': target[2],
        'fightState': -1,
        'callback': callback_name,
        'promptBranches': branches,
    }


def task_item_consume_faction_gate_newworld(source: str) -> dict[str, Any] | None:
    clean_source = strip_lua_line_comments(source)
    main_body = lua_function_body(clean_source, 'main')
    if not main_body or re.search(r'\b(for|while|repeat)\b', main_body):
        return None
    if not source_uses_only_calls(clean_source, {
        'main', 'GetTask', 'HaveItem', 'DelItem', 'SetFightState', 'NewWorld', 'SetTask', 'GetFaction', 'Talk', 'if', 'elseif', 'and'
    }):
        return None
    task_match = re.search(r'(?P<var>[A-Za-z_][A-Za-z0-9_]*)\s*=\s*GetTask\s*\(\s*(?P<taskId>\d+)\s*\)', main_body)
    if not task_match:
        return None
    task_var = task_match.group('var')
    item_cond = (
        rf'\(?\(?\s*{re.escape(task_var)}\s*==\s*(?P<itemTask>[0-9\s*+\-]+)\s*\)?\s*and\s*'
        rf'\(?\s*HaveItem\s*\(\s*(?P<itemId>\d+)\s*\)\s*==\s*(?P<itemCount>\d+)\s*\)?\s*\)?'
    )
    faction_cond = (
        rf'\(?\s*{re.escape(task_var)}\s*(?P<op>>=|>)\s*(?P<passMin>[0-9\s*+\-]+)\s*\)?\s*and\s*'
        rf'\(?\s*GetFaction\s*\(\s*\)\s*==\s*["\'](?P<faction>[a-z]+)["\']\s*\)?'
    )
    patterns = [
        rf'if\s*{item_cond}\s*then(?P<item>.*?)elseif\s*{faction_cond}\s*then(?P<repeat>.*?)else(?P<fail>.*?)end',
        rf'if\s*{faction_cond}\s*then(?P<repeat>.*?)elseif\s*{item_cond}\s*then(?P<item>.*?)else(?P<fail>.*?)end',
    ]
    match = None
    for pattern in patterns:
        match = re.search(pattern, main_body, re.S | re.I)
        if match:
            break
    if not match:
        return None

    item_task = int_lua_constant_expr(match.group('itemTask'))
    pass_min_raw = int_lua_constant_expr(match.group('passMin'))
    item_id = int(match.group('itemId'))
    item_count = int(match.group('itemCount'))
    faction = match.group('faction').lower()
    faction_id = PC_FACTION_IDS.get(faction)
    if item_task is None or pass_min_raw is None or item_count <= 0 or faction_id is None:
        return None
    pass_min = pass_min_raw + 1 if match.group('op') == '>' else pass_min_raw

    item_body = match.group('item')
    repeat_body = match.group('repeat')
    fail_body = match.group('fail')
    item_new_world = int_args_unique(parse_lua_calls(item_body, 'NewWorld', limit=2), 3)
    repeat_new_world = int_args_unique(parse_lua_calls(repeat_body, 'NewWorld', limit=2), 3)
    item_fight = int_args_unique(parse_lua_calls(item_body, 'SetFightState', limit=2), 1)
    repeat_fight = int_args_unique(parse_lua_calls(repeat_body, 'SetFightState', limit=2), 1)
    consume = int_args_unique(parse_lua_calls(item_body, 'DelItem', limit=2), 1)
    if (
        item_new_world is None or repeat_new_world is None or item_new_world != repeat_new_world or
        item_fight is None or repeat_fight is None or item_fight != repeat_fight or consume is None or consume[0] != item_id
    ):
        return None
    set_task_ids: list[int] = []
    set_task_values: list[int] = []
    for call in parse_lua_calls(item_body, 'SetTask', limit=4):
        if len(call) < 2 or not str(call[0]).strip().isdigit():
            return None
        value = int_lua_constant_expr(str(call[1]))
        if value is None:
            return None
        set_task_ids.append(int(str(call[0]).strip()))
        set_task_values.append(value)
    if not set_task_ids:
        return None
    fail_messages = talk_messages({'talkCalls': parse_lua_calls(fail_body, 'Talk', limit=2)})
    if len(fail_messages) != 1:
        return None
    return {
        'taskId': int(task_match.group('taskId')),
        'taskValue': item_task,
        'passTaskMinInclusive': pass_min,
        'requiredFaction': faction,
        'requiredFactionId': faction_id,
        'requiredItemId': item_id,
        'requiredItemCount': item_count,
        'consumeItemId': item_id,
        'consumeItemCount': item_count,
        'targetMapId': item_new_world[0],
        'targetCellX': item_new_world[1],
        'targetCellY': item_new_world[2],
        'fightState': item_fight[0],
        'setTaskIds': set_task_ids,
        'setTaskValues': set_task_values,
        'message': fail_messages[0],
    }


def conditional_fight_state_setpos(source: str) -> dict[str, int] | None:
    if 'Talk(' in source or 'Msg2Player' in source or 'NewWorld' in source:
        return None
    match = re.search(
        r'if\s*\(?\s*GetFightState\s*\(\s*\)\s*==\s*(\d+)\s*\)?\s*then(?P<then>.*?)else(?P<else>.*?)end\s*;?',
        source, re.S)
    if not match:
        return None

    def first_pair(block: str) -> tuple[int, int] | None:
        pairs = re.findall(r'\bSetPos\s*\(\s*(\d+)\s*,\s*(\d+)\s*\)', block)
        if len(pairs) != 1:
            return None
        return int(pairs[0][0]), int(pairs[0][1])

    def first_state(block: str, fallback: int) -> int:
        states = re.findall(r'\bSetFightState\s*\(\s*(\d+)\s*\)', block)
        return int(states[0]) if len(states) == 1 else fallback

    then_pos = first_pair(match.group('then'))
    else_pos = first_pair(match.group('else'))
    if then_pos is None or else_pos is None:
        return None

    if_state = int(match.group(1))
    else_state = 1 - if_state if if_state in (0, 1) else -1
    return {
        'ifFightState': if_state,
        'ifTargetCellX': then_pos[0],
        'ifTargetCellY': then_pos[1],
        'ifNextFightState': first_state(match.group('then'), if_state),
        'elseFightState': else_state,
        'elseTargetCellX': else_pos[0],
        'elseTargetCellY': else_pos[1],
        'elseNextFightState': first_state(match.group('else'), else_state),
    }


def int_args_unique(calls: list[list[str]], width: int) -> tuple[int, ...] | None:
    parsed: set[tuple[int, ...]] = set()
    for call in calls or []:
        if len(call) < width:
            return None
        values = []
        for arg in call[:width]:
            if not re.fullmatch(r'\d+', str(arg).strip()):
                return None
            values.append(int(str(arg).strip()))
        parsed.add(tuple(values))
    if len(parsed) != 1:
        return None
    return next(iter(parsed))


def expr_args_unique(calls: list[list[str]], width: int) -> tuple[int, ...] | None:
    parsed = set(expr_args(calls or [], width))
    if len(parsed) != 1:
        return None
    return next(iter(parsed))


def build_trap_action_catalog(trap_scripts: list[dict[str, Any]]) -> tuple[list[dict[str, Any]], dict[str, Any]]:
    entries: list[dict[str, Any]] = []
    for script in trap_scripts:
        source = script.get('sourceText', '')
        actions = script_action_summary(source) if source else (script.get('actions') or {})
        if not script.get('resolved'):
            continue
        task_triplet_leave = task_triplet_leave_trap(source)
        if task_triplet_leave is not None:
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'TaskTripletLeaveTrap',
                'targetMapId': 0,
                'targetCellX': 0,
                'targetCellY': 0,
                'source': 'PC mission leave trap: SetCurCamp(GetCamp), SetFightState, SetRevPos, optional logout/team/pk/death/task side effects, then NewWorld(GetLeavePos()) from GetTask(300/301/302)',
                **task_triplet_leave,
            })
            continue
        if not actions.get('hasMain'):
            continue
        if is_safe_trap_msg2player_message(actions, source):
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'Msg2Player',
                'targetMapId': 0,
                'targetCellX': 0,
                'targetCellY': 0,
                'fightState': -1,
                'message': single_string(actions.get('msg2PlayerCalls') or []),
                'source': 'PC trap Lua main(): deterministic read-only Msg2Player(message) with no movement/fight/task/item branch',
            })
            continue
        if is_safe_trap_say_message(actions, source):
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'SayMessage',
                'targetMapId': 0,
                'targetCellX': 0,
                'targetCellY': 0,
                'fightState': -1,
                'message': single_string(actions.get('sayCalls') or []),
                'source': 'PC trap Lua main(): deterministic read-only Say(message,0) with no movement/fight/task/item branch',
            })
            continue
        if is_safe_trap_talk_message(actions, source):
            talk_lines = talk_messages(actions)
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'TalkMessage',
                'targetMapId': 0,
                'targetCellX': 0,
                'targetCellY': 0,
                'fightState': -1,
                'message': '\n'.join(talk_lines),
                'messages': talk_lines,
                'source': 'PC trap Lua main(): deterministic read-only Talk(count,"",message...) with no movement/fight/task/item branch',
            })
            continue
        if is_safe_trap_msg2player_newworld(actions, source):
            new_world = int_args_unique(actions.get('newWorldCalls') or [], 3)
            fight_state = int_args_unique(actions.get('setFightStateCalls') or [], 1) if actions.get('setFightStateCalls') else None
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'Msg2PlayerNewWorld',
                'targetMapId': new_world[0],
                'targetCellX': new_world[1],
                'targetCellY': new_world[2],
                'fightState': fight_state[0] if fight_state is not None else -1,
                'message': single_string(actions.get('msg2PlayerCalls') or []),
                'terminiIds': [values[0] for values in expr_args(actions.get('addTerminiCalls') or [], 1)],
                'source': 'PC trap Lua main(): deterministic Msg2Player(message) followed by NewWorld(map,x,y), optional SetFightState/AddTermini, with no branch/task/item side effects',
            })
            continue
        prompt = main_callback_prompt_message(source)
        if prompt is not None:
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'PromptMessage',
                'targetMapId': 0,
                'targetCellX': 0,
                'targetCellY': 0,
                'fightState': -1,
                'source': 'PC trap Lua main(): read-only Talk/Say prompt with callback choices; callback side effects remain deferred until PC dialog callbacks/task/item APIs are ported',
                **prompt,
            })
            continue
        task_optional = task_optional_talk_newworld(source)
        if task_optional is not None:
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'TaskOptionalMessageNewWorld',
                'source': 'PC trap Lua main(): optional read-only Talk when GetTask(id)==value, then deterministic SetFightState/NewWorld regardless of task state',
                **task_optional,
            })
            continue
        level_bracket = level_bracket_newworld(source)
        if level_bracket is not None:
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'LevelBracketNewWorld',
                'targetMapId': 0,
                'targetCellX': 0,
                'targetCellY': 0,
                'source': 'PC Song/Jin battlefield trap Lua: GetLevel <40 Talk fail; level 40-79/80-119/120+ NewWorld bracket, SetFightState, Msg2Player, optional protect/buff side effects',
                **level_bracket,
            })
            continue
        if is_safe_trap_level_gate_newworld(actions, source):
            new_world = int_args_unique(actions.get('newWorldCalls') or [], 3)
            fight_state = int_args_unique(actions.get('setFightStateCalls') or [], 1)
            fail_pos = int_args_unique(actions.get('setPosCalls') or [], 2) if actions.get('setPosCalls') else None
            protect_time = expr_args_unique(actions.get('setProtectTimeCalls') or [], 1) if actions.get('setProtectTimeCalls') else None
            skill_state = expr_args_unique(actions.get('addSkillStateCalls') or [], 4) if actions.get('addSkillStateCalls') else None
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'LevelGateNewWorld',
                'targetMapId': new_world[0],
                'targetCellX': new_world[1],
                'targetCellY': new_world[2],
                'fightState': fight_state[0],
                'requiredLevel': level_gate_requirement(source),
                'failTargetCellX': fail_pos[0] if fail_pos else 0,
                'failTargetCellY': fail_pos[1] if fail_pos else 0,
                'message': '\n'.join(talk_messages(actions)),
                'messages': talk_messages(actions),
                'terminiIds': [values[0] for values in expr_args(actions.get('addTerminiCalls') or [], 1)],
                'protectTicks': protect_time[0] if protect_time else 0,
                'skillStateId': skill_state[0] if skill_state else 0,
                'skillStateLevel': skill_state[1] if skill_state else 0,
                'skillStateTime': skill_state[3] if skill_state else 0,
                'source': 'PC trap Lua main(): if GetLevel()>=N then SetFightState/NewWorld/AddTermini/SetProtectTime/AddSkillState else Talk/optional SetPos',
            })
            continue
        open_server_gate = open_server_date_gate_setpos(source)
        if open_server_gate is not None:
            entry = {
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'OpenServerDateGateSetPos',
                'targetMapId': 0,
                'targetCellX': open_server_gate['closedTargetCellX'],
                'targetCellY': open_server_gate['closedTargetCellY'],
                'fightState': -1,
                'source': 'PC trap Lua main(): Include(configall.lua), if tonumber(GetLocalDate()) < ThoiGianOpenServer then SetPos/Msg2Player/AddStation/SetProtectTime/AddSkillState else GetFightState SetPos/SetFightState plus optional AddStation/protect/buff',
            }
            entry.update(open_server_gate)
            entries.append(entry)
            continue
        message_random = message_random_newworld(source)
        if message_random is not None:
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'MessageRandomNewWorld',
                'targetMapId': 0,
                'targetCellX': 0,
                'targetCellY': 0,
                'fightState': -1,
                'source': 'PC trap Lua main(): read-only Talk message then random(min,max) branch table NewWorld targets; duplicated GetSex message branches are identical',
                **message_random,
            })
            continue
        random_maze = desert_maze_random_newworld(source)
        if random_maze is not None:
            entry = {
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'RandomNewWorld',
                'targetMapId': 0,
                'targetCellX': 0,
                'targetCellY': 0,
                'fightState': -1,
                'source': 'PC trap Lua main(): random(0,120) desert-maze SetFightState/NewWorld branch table, with optional current-map return/gate guard',
            }
            entry.update(random_maze)
            entries.append(entry)
            continue
        revive_return = revive_return_newworld(source)
        if revive_return is not None:
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'ReviveReturnNewWorld',
                'source': 'PC trap Lua main(): if SubWorldIdx2ID(SubWorld) is return map then RevID2WXY(GetPlayerRev())/NewWorld else SetFightState/NewWorld fixed target with optional AddTermini',
                **revive_return,
            })
            continue
        task_setpos = task_setpos_message_gate(source)
        if task_setpos is not None:
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'TaskSetPosMessage',
                'targetMapId': 0,
                'targetCellX': 0,
                'targetCellY': 0,
                'fightState': -1,
                'source': 'PC trap Lua main(): task-state gate using GetTask(id), deterministic SetPos plus optional Msg2Player per branch',
                **task_setpos,
            })
            continue
        task_faction_gate = task_faction_gate_newworld_trap(source)
        if task_faction_gate is not None:
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'TaskFactionGateNewWorld',
                'source': 'PC trap Lua main(): GetTask/GetFaction gate, pass NewWorld+SetFightState, otherwise Talk+SetPos to fail target',
                **task_faction_gate,
            })
            continue
        task_prompt_default = task_prompt_default_newworld_trap(source)
        if task_prompt_default is not None:
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'TaskPromptDefaultNewWorld',
                'source': 'PC trap Lua main(): GetTask prompt branches show Say/Talk and do not auto-warp; default branch calls enter_cave SetFightState/NewWorld/AddTermini',
                **task_prompt_default,
            })
            continue
        task_faction_message_gate = task_faction_message_gate_newworld(source)
        if task_faction_message_gate is not None:
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'TaskFactionMessageGateNewWorld',
                'source': 'PC trap Lua main(): GetTask/GetFaction pass NewWorld+SetFightState, task-low Talk, wrong-faction Talk, no SetPos/task/item mutation',
                **task_faction_message_gate,
            })
            continue
        task_faction_prompt_gate = task_faction_prompt_gate_newworld(source)
        if task_faction_prompt_gate is not None:
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'TaskFactionPromptGateNewWorld',
                'source': 'PC trap Lua main(): GetSeries/GetFaction/GetTask gate, exact task callback prompt is read-only, higher task NewWorld, low/wrong faction Msg2Player, no item/task mutation',
                **task_faction_prompt_gate,
            })
            continue
        task_current_map_return = task_current_map_return_newworld(source)
        if task_current_map_return is not None:
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'TaskCurrentMapReturnNewWorld',
                'source': 'PC trap Lua main(): Mid-Autumn tab_cityid return; GetTask nonzero shows Say callback prompt only, task zero maps current event map id to city NewWorld; callback choices are not auto-run',
                **task_current_map_return,
            })
            continue
        task_settask_faction_gate = task_settask_faction_gate_newworld(source)
        if task_settask_faction_gate is not None:
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'TaskSetTaskFactionGateNewWorld',
                'source': 'PC trap Lua main(): exact GetTask branch applies SetFightState/NewWorld/SetTask; completed faction branch enters; fail branch Talk+SetPos+AddNote',
                **task_settask_faction_gate,
            })
            continue
        task_item_gate = task_item_consume_faction_gate_newworld(source)
        if task_item_gate is not None:
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'TaskItemConsumeFactionGateNewWorld',
                'source': 'PC trap Lua main(): ordered GetTask/HaveItem/DelItem questkey branch plus completed faction repeat branch, Talk-only fail branch',
                **task_item_gate,
            })
            continue
        task_settask_prompt_callback = task_settask_prompt_callback_newworld(source)
        if task_settask_prompt_callback is not None:
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'TaskSetTaskPromptCallbackNewWorld',
                'source': 'PC trap Lua main(): GetTask branches show Talk(callback); first branch applies SetTask before prompt; deterministic callback NewWorld executes after prompt',
                **task_settask_prompt_callback,
            })
            continue
        citywar_gate = citywar_camp_gate_setpos(source)
        if citywar_gate is not None:
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'CityWarCampGateSetPos',
                'targetMapId': 0,
                'targetCellX': citywar_gate['enterCellX'],
                'targetCellY': citywar_gate['enterCellY'],
                'fightState': -1,
                'source': 'PC citywar_city chengzhan_map ctrap: GetFightState enter branch SetPos/SetFightState/bt_RankEffect; else camp guard Msg2Player+SetPos or exit SetPos/SetFightState',
                **citywar_gate,
            })
            continue
        citywar_return = citywar_camp_return_newworld(source)
        if citywar_return is not None:
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'CityWarCampReturnNewWorld',
                'source': 'PC citywar_city chengzhan_map trap1/trap2: camp guard then SetCurCamp(GetCamp), SetFightState, SetLogoutRV, NewWorld reserve map',
                **citywar_return,
            })
            continue
        clear_switch = clearskill_switch_trap(source)
        if clear_switch is not None:
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'ClearSkillSwitchTrap',
                'targetMapId': 0,
                'targetCellX': clear_switch['enterCellX'],
                'targetCellY': clear_switch['enterCellY'],
                'source': 'PC clearskill head.lua CSP_SwitchTrap: GetFightState toggles fight/pk/forbid/punish/logout and SetPos via CSP_ClearTrapTab',
                **clear_switch,
            })
            continue
        clear_leave = clearskill_leave_game(source)
        if clear_leave is not None:
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'ClearSkillLeaveGame',
                'targetMapId': 0,
                'targetCellX': clear_leave['enterCellX'],
                'targetCellY': clear_leave['enterCellY'],
                'source': 'PC clearskill testhole.lua LeaveGame: derive clear map from current test map group, reset fight/pk/punish/logout/death/revive state, LeaveTeam, NewWorld to CSP_ClearHoleTab',
                **clear_leave,
            })
            continue
        cs_leave = cs_arena_leave_trap(source)
        if cs_leave is not None:
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'CsArenaLeaveTrap',
                'targetMapId': 0,
                'targetCellX': 0,
                'targetCellY': 0,
                'source': 'PC cs arena leavetrap.lua: LeaveTeam, SetCurCamp(GetCamp), SetFightState, SetLogoutRV, SetRevPos(CS_RevId,CS_RevData), NewWorld(GetLeavePos()) where GetLeavePos returns GetTask(300/301/302)',
                **cs_leave,
            })
            continue
        if actions.get('talks'):
            continue
        fight_state_setpos = conditional_fight_state_setpos(source)
        if fight_state_setpos is not None:
            entry = {
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'FightStateSetPos',
                'targetMapId': 0,
                'targetCellX': 0,
                'targetCellY': 0,
                'fightState': -1,
                'source': 'PC trap Lua main(): if GetFightState()==N then SetPos/SetFightState else SetPos/SetFightState with no Talk/Msg2Player/NewWorld',
            }
            entry.update(fight_state_setpos)
            entries.append(entry)
            continue

        fight_state = int_args_unique(actions.get('setFightStateCalls') or [], 1)
        fight_value = fight_state[0] if fight_state is not None else None
        new_world = int_args_unique(actions.get('newWorldCalls') or [], 3)
        set_pos = int_args_unique(actions.get('setPosCalls') or [], 2)
        if new_world is not None:
            map_id, cell_x, cell_y = new_world
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'NewWorld',
                'targetMapId': map_id,
                'targetCellX': cell_x,
                'targetCellY': cell_y,
                'fightState': fight_value if fight_value is not None else -1,
                'source': 'PC trap Lua main(): deterministic NewWorld with no Talk/Msg2Player branch',
            })
        elif set_pos is not None:
            cell_x, cell_y = set_pos
            entries.append({
                'trapId': script['trapId'],
                'trapIdHex': script['trapIdHex'],
                'scriptPath': script.get('scriptPath', ''),
                'sourceRelPath': script.get('sourceRelPath', ''),
                'actionKind': 'SetPos',
                'targetMapId': 0,
                'targetCellX': cell_x,
                'targetCellY': cell_y,
                'fightState': fight_value if fight_value is not None else -1,
                'source': 'PC trap Lua main(): deterministic SetPos with no Talk/Msg2Player branch',
            })
    coverage = {
        'deterministicTrapActions': len(entries),
        'deterministicNewWorldActions': sum(1 for e in entries if e['actionKind'] == 'NewWorld'),
        'deterministicSetPosActions': sum(1 for e in entries if e['actionKind'] == 'SetPos'),
        'deterministicFightStateSetPosActions': sum(1 for e in entries if e['actionKind'] == 'FightStateSetPos'),
        'deterministicTrapMsg2PlayerActions': sum(1 for e in entries if e['actionKind'] == 'Msg2Player'),
        'deterministicTrapSayMessageActions': sum(1 for e in entries if e['actionKind'] == 'SayMessage'),
        'deterministicTrapTalkMessageActions': sum(1 for e in entries if e['actionKind'] == 'TalkMessage'),
        'deterministicTrapMessageActions': sum(1 for e in entries if e['actionKind'] in {'Msg2Player', 'SayMessage', 'TalkMessage', 'PromptMessage'}),
        'deterministicTrapPromptMessageActions': sum(1 for e in entries if e['actionKind'] == 'PromptMessage'),
        'deterministicTrapMsg2PlayerNewWorldActions': sum(1 for e in entries if e['actionKind'] == 'Msg2PlayerNewWorld'),
        'deterministicTrapLevelGateNewWorldActions': sum(1 for e in entries if e['actionKind'] == 'LevelGateNewWorld'),
        'deterministicTrapLevelBracketNewWorldActions': sum(1 for e in entries if e['actionKind'] == 'LevelBracketNewWorld'),
        'deterministicTrapOpenServerDateGateSetPosActions': sum(1 for e in entries if e['actionKind'] == 'OpenServerDateGateSetPos'),
        'deterministicTrapRandomNewWorldActions': sum(1 for e in entries if e['actionKind'] == 'RandomNewWorld'),
        'deterministicTrapMessageRandomNewWorldActions': sum(1 for e in entries if e['actionKind'] == 'MessageRandomNewWorld'),
        'deterministicTrapReviveReturnNewWorldActions': sum(1 for e in entries if e['actionKind'] == 'ReviveReturnNewWorld'),
        'deterministicTrapTaskSetPosMessageActions': sum(1 for e in entries if e['actionKind'] == 'TaskSetPosMessage'),
        'deterministicTrapTaskOptionalMessageNewWorldActions': sum(1 for e in entries if e['actionKind'] == 'TaskOptionalMessageNewWorld'),
        'deterministicTrapTaskFactionGateNewWorldActions': sum(1 for e in entries if e['actionKind'] == 'TaskFactionGateNewWorld'),
        'deterministicTrapTaskPromptDefaultNewWorldActions': sum(1 for e in entries if e['actionKind'] == 'TaskPromptDefaultNewWorld'),
        'deterministicTrapTaskFactionMessageGateNewWorldActions': sum(1 for e in entries if e['actionKind'] == 'TaskFactionMessageGateNewWorld'),
        'deterministicTrapTaskFactionPromptGateNewWorldActions': sum(1 for e in entries if e['actionKind'] == 'TaskFactionPromptGateNewWorld'),
        'deterministicTrapTaskCurrentMapReturnNewWorldActions': sum(1 for e in entries if e['actionKind'] == 'TaskCurrentMapReturnNewWorld'),
        'deterministicTrapTaskSetTaskFactionGateNewWorldActions': sum(1 for e in entries if e['actionKind'] == 'TaskSetTaskFactionGateNewWorld'),
        'deterministicTrapTaskSetTaskPromptCallbackNewWorldActions': sum(1 for e in entries if e['actionKind'] == 'TaskSetTaskPromptCallbackNewWorld'),
        'deterministicTrapTaskItemConsumeFactionGateNewWorldActions': sum(1 for e in entries if e['actionKind'] == 'TaskItemConsumeFactionGateNewWorld'),
        'deterministicTrapCityWarCampGateSetPosActions': sum(1 for e in entries if e['actionKind'] == 'CityWarCampGateSetPos'),
        'deterministicTrapCityWarCampReturnNewWorldActions': sum(1 for e in entries if e['actionKind'] == 'CityWarCampReturnNewWorld'),
        'deterministicTrapClearSkillSwitchTrapActions': sum(1 for e in entries if e['actionKind'] == 'ClearSkillSwitchTrap'),
        'deterministicTrapClearSkillLeaveGameActions': sum(1 for e in entries if e['actionKind'] == 'ClearSkillLeaveGame'),
        'deterministicTrapCsArenaLeaveTrapActions': sum(1 for e in entries if e['actionKind'] == 'CsArenaLeaveTrap'),
        'deterministicTrapTaskTripletLeaveTrapActions': sum(1 for e in entries if e['actionKind'] == 'TaskTripletLeaveTrap'),
    }
    return entries, coverage


def sections(data: bytes) -> tuple[int, int, list[tuple[int, int]]]:
    if len(data) < 4:
        return 0, 0, []
    count = struct.unpack_from('<I', data, 0)[0]
    if count <= 0 or count > 20:
        return 0, 0, []
    header = 4 + count * 8
    if len(data) < header:
        return 0, 0, []
    return count, header, [struct.unpack_from('<II', data, 4 + i * 8) for i in range(count)]


def clean_script(raw: bytes) -> str:
    try:
        return raw.split(b'\0', 1)[0].decode('gb2312', errors='ignore').strip()
    except Exception:
        return ''


def parse_traps(data: bytes, header: int, secs: list[tuple[int, int]], col: int, row: int) -> list[dict[str, Any]]:
    if len(secs) <= 1:
        return []
    off, length = secs[1]
    start = header + off
    if length < 12 or start + length > len(data):
        return []
    count = struct.unpack_from('<I', data, start)[0]
    pos = start + 12
    end = start + length
    traps = []
    for idx in range(count):
        if pos + 8 > end:
            break
        cell_x, cell_y, num_cell, reserved = struct.unpack_from('<BBBB', data, pos)
        trap_id = struct.unpack_from('<I', data, pos + 4)[0]
        pos += 8
        traps.append({
            'regionCol': col,
            'regionRow': row,
            'index': idx,
            'cellX': cell_x,
            'cellY': cell_y,
            'numCell': num_cell,
            'trapId': int(trap_id),
            'reserved': reserved,
        })
    return traps


def parse_objects(data: bytes, header: int, secs: list[tuple[int, int]], col: int, row: int) -> list[dict[str, Any]]:
    if len(secs) <= 3:
        return []
    off, length = secs[3]
    start = header + off
    if length < 12 or start + length > len(data):
        return []
    count = struct.unpack_from('<I', data, start)[0]
    pos = start + 12
    end = start + length
    objects = []
    for idx in range(count):
        if pos + 24 > end:
            break
        template_id = struct.unpack_from('<i', data, pos)[0]
        state = struct.unpack_from('<h', data, pos + 4)[0]
        bio_index = struct.unpack_from('<H', data, pos + 6)[0]
        x, y, z = struct.unpack_from('<iii', data, pos + 8)
        direction = struct.unpack_from('<b', data, pos + 20)[0]
        skip_paint = data[pos + 21] != 0
        script_len = struct.unpack_from('<H', data, pos + 22)[0]
        pos += 24
        script = ''
        if script_len > 0 and pos + script_len <= end:
            script = clean_script(data[pos:pos + script_len])
            pos += script_len
        objects.append({
            'regionCol': col,
            'regionRow': row,
            'index': idx,
            'templateId': template_id,
            'state': state,
            'bioIndex': bio_index,
            'mpsX': x,
            'mpsY': y,
            'z': z,
            'direction': direction,
            'skipPaint': skip_paint,
            'script': script,
        })
    return objects


def parse_region_file(path: Path) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    m = REGION_FILE_RE.match(path.name)
    if not m:
        return [], []
    col, row = int(m.group(1)), int(m.group(2))
    data = path.read_bytes()
    _, header, secs = sections(data)
    if not secs:
        return [], []
    return parse_traps(data, header, secs, col, row), parse_objects(data, header, secs, col, row)


def load_server_catalog(path: Path) -> dict[str, dict[str, Any]]:
    data = json.loads(path.read_text(encoding='utf-8'))
    result = {}
    for entry in data.get('geometries', []):
        key = entry.get('geometryKey')
        if key:
            result[key] = entry
    return result


def enrich_object(obj: dict[str, Any], templates: dict[int, dict[str, Any]]) -> dict[str, Any]:
    template = templates.get(int(obj.get('templateId', 0)))
    if not template:
        return obj
    obj = dict(obj)
    obj.update({
        'nameVi': template.get('nameVi', ''),
        'kind': template.get('kind', ''),
        'imageName': template.get('imageName', ''),
        'imageUid': jx.compute_path_uid(template.get('imageName', '')) if template.get('imageName') else '',
        'imageCurFrame': template.get('imageCurFrame', 0),
        'imageCurDir': template.get('imageCurDir', 0),
        'imageTotalFrame': template.get('imageTotalFrame', 0),
        'imageTotalDir': template.get('imageTotalDir', 0),
        'imageInterval': template.get('imageInterval', 0),
        'imageCgXpos': template.get('imageCgXpos', 0),
        'imageCgYpos': template.get('imageCgYpos', 0),
        'height': template.get('height', 0),
        'layer': template.get('layer', 0),
        'isUnseen': template.get('isUnseen', 0),
        'obstacleKind': template.get('obstacleKind', 0),
        'loopAnimation': template.get('loopAnimation', 0),
    })
    return obj


def enrich_traps_in_geometries(geometries: list[dict[str, Any]], trap_scripts: dict[int, dict[str, Any]]) -> None:
    for geometry in geometries:
        inactive_maps = [mid for mid in geometry.get('mapIds', []) if mid in KILLBOSSMATCH_MAP_IDS]
        if inactive_maps:
            geometry['staticTrapClearMapIds'] = inactive_maps
        for trap in geometry.get('traps', []):
            trap_id = int(trap.get('trapId', 0))
            script = trap_scripts.get(trap_id)
            trap['trapIdHex'] = f'0x{trap_id:08X}'
            trap['scriptResolved'] = bool(script and script.get('resolved'))
            if script and script.get('resolved'):
                trap['scriptPath'] = script.get('scriptPath', '')
            if inactive_maps:
                trap['inactiveMapIds'] = inactive_maps


def build_catalog(region_root: Path, server_catalog_path: Path,
                  obj_templates: dict[int, dict[str, Any]]) -> tuple[list[dict[str, Any]], dict[str, Any], set[int], set[int]]:
    server = load_server_catalog(server_catalog_path)
    geometries = []
    total_traps = 0
    total_objects = 0
    unique_trap_ids = set()
    unique_obj_templates = set()
    used_template_ids: set[int] = set()
    for folder in sorted(p for p in region_root.iterdir() if p.is_dir()):
        key = folder.name
        server_entry = server.get(key, {})
        traps: list[dict[str, Any]] = []
        objects: list[dict[str, Any]] = []
        for region_file in sorted(folder.glob('*_Region_S.dat')):
            file_traps, file_objects = parse_region_file(region_file)
            traps.extend(file_traps)
            objects.extend(file_objects)
        for trap in traps:
            unique_trap_ids.add(trap['trapId'])
        objects = [enrich_object(obj, obj_templates) for obj in objects]
        for obj in objects:
            template_id = obj['templateId']
            unique_obj_templates.add(template_id)
            used_template_ids.add(template_id)
        total_traps += len(traps)
        total_objects += len(objects)
        geometries.append({
            'geometryKey': key,
            'primaryMapId': server_entry.get('primaryMapId', 0),
            'mapIds': server_entry.get('mapIds', []),
            'pcMapPath': server_entry.get('pcMapPath', ''),
            'serverMapPath': server_entry.get('serverMapPath', server_entry.get('pcMapPath', '')),
            'trapCount': len(traps),
            'objectCount': len(objects),
            'traps': traps,
            'objects': objects,
        })
    coverage = {
        'totalGeometries': len(geometries),
        'geometriesWithTraps': sum(1 for g in geometries if g['trapCount'] > 0),
        'geometriesWithObjects': sum(1 for g in geometries if g['objectCount'] > 0),
        'trapEntries': total_traps,
        'objectEntries': total_objects,
        'uniqueTrapIds': len(unique_trap_ids),
        'uniqueObjectTemplates': len(unique_obj_templates),
    }
    return geometries, coverage, used_template_ids, unique_trap_ids


def main() -> int:
    parser = argparse.ArgumentParser(description='Catalog PC Region_S trap/object records.')
    parser.add_argument('--unity-root', type=Path, default=UNITY_ROOT)
    parser.add_argument('--pc-root', type=Path, default=PC_ROOT)
    parser.add_argument('--region-root', type=Path, default=None)
    parser.add_argument('--server-catalog', type=Path, default=None)
    parser.add_argument('--catalog-root', type=Path, default=None)
    parser.add_argument('--objdata', type=Path, default=None)
    parser.add_argument('--object-sprite-root', type=Path, default=None)
    parser.add_argument('--extract', action='store_true', help='write staged object SPR files under Generated/ObjectSprites')
    parser.add_argument('--clean', action='store_true', help='clean Generated/ObjectSprites before extracting')
    args = parser.parse_args()

    unity_root = args.unity_root.resolve()
    pc_root = args.pc_root.resolve()
    region_root = args.region_root or unity_root / 'Assets/StreamingAssets/Generated/MapServerRegions'
    server_catalog = args.server_catalog or unity_root / 'Assets/StreamingAssets/MapServerRegionCatalog.json'
    catalog_root = args.catalog_root or unity_root / 'Assets/StreamingAssets'
    objdata = args.objdata or unity_root / 'Assets/StreamingAssets/Reference/PcObj/objdata.txt'
    object_sprite_root = args.object_sprite_root or unity_root / 'Assets/StreamingAssets/Generated/ObjectSprites'
    if args.clean and object_sprite_root.exists():
        shutil.rmtree(object_sprite_root)

    start = time.time()
    obj_templates = load_objdata_templates(objdata)
    geometries, coverage, used_template_ids, used_trap_ids = build_catalog(region_root, server_catalog, obj_templates)
    trap_scripts, trap_coverage = build_trap_script_catalog(used_trap_ids, pc_root)
    trap_actions, trap_action_coverage = build_trap_action_catalog(trap_scripts)
    object_scripts, object_script_coverage = build_object_script_catalog(geometries, pc_root)
    object_actions, object_action_coverage = build_object_action_catalog(object_scripts)
    enrich_traps_in_geometries(geometries, {entry['trapId']: entry for entry in trap_scripts})
    object_templates, object_coverage = stage_object_templates(
        obj_templates, used_template_ids, pc_root, object_sprite_root, args.extract)
    now = utc_now()
    catalog = {
        'schemaVersion': 1,
        'generatedAtUtc': now,
        'sourceRegionRoot': str(region_root),
        'sourceServerCatalog': str(server_catalog),
        'formatSource': 'Assets/StreamingAssets/Reference/SceneDataDef.h KSPTrap/KSPObj',
        'geometries': geometries,
    }
    object_catalog = {
        'schemaVersion': 1,
        'generatedAtUtc': now,
        'sourceObjData': str(objdata),
        'sourcePakOrder': str(pc_root / 'Client 6.0/package1.ini'),
        'spriteFolder': 'Generated/ObjectSprites',
        'templates': object_templates,
    }
    trap_catalog_entries = []
    for entry in trap_scripts:
        clean_entry = dict(entry)
        clean_entry.pop('sourceText', None)
        actions = clean_entry.get('actions')
        if actions:
            clean_entry['actions'] = {
                'hasMain': actions.get('hasMain', False),
                'newWorldCalls': actions.get('newWorldCalls', []),
                'setPosCalls': actions.get('setPosCalls', []),
                'setsFightState': actions.get('setsFightState', False),
                'talks': actions.get('talks', False),
            }
        trap_catalog_entries.append(clean_entry)
    trap_catalog = {
        'schemaVersion': 1,
        'generatedAtUtc': now,
        'sourceScriptRoot': str(server_root(pc_root) / 'script'),
        'hashRule': 'PC g_FileName2Id signed-char over leading-backslash GBK script path',
        'entries': trap_catalog_entries,
    }
    object_script_catalog_entries = []
    for entry in object_scripts:
        clean_entry = dict(entry)
        clean_entry.pop('sourceText', None)
        object_script_catalog_entries.append(clean_entry)
    object_script_catalog = {
        'schemaVersion': 1,
        'generatedAtUtc': now,
        'sourceScriptRoot': str(server_root(pc_root) / 'script'),
        'hashRule': 'PC g_FileName2Id signed-char over leading-backslash GBK script path',
        'entries': object_script_catalog_entries,
    }
    trap_action_catalog = {
        'schemaVersion': 1,
        'generatedAtUtc': now,
        'sourceTrapScriptCatalog': 'MapTrapScriptCatalog.json',
        'coordinateRule': 'PC NewWorld/SetPos cell coordinates are multiplied by 32 MPS before MapEnemyDatabase.MpsToWorld',
        'entries': trap_actions,
    }
    object_action_catalog = {
        'schemaVersion': 1,
        'generatedAtUtc': now,
        'sourceObjectScriptCatalog': 'MapObjectScriptCatalog.json',
        'coordinateRule': 'PC object NewWorld cell coordinates are multiplied by 32 MPS before MapEnemyDatabase.MpsToWorld',
        'entries': object_actions,
    }
    coverage_payload = {
        'schemaVersion': 1,
        'generatedAtUtc': now,
        'phase': 'phase4_region_s_trap_object_catalog',
        'scope': 'PC Region_S trap/object catalog + exact ObjData SPR staging; traps remain invisible metadata/no gameplay placeholder',
        'sourceRegionRoot': str(region_root),
        'sourceServerCatalog': str(server_catalog),
        'sourceObjData': str(objdata),
        **coverage,
        **object_coverage,
        **trap_coverage,
        **trap_action_coverage,
        **object_script_coverage,
        **object_action_coverage,
        'elapsedSeconds': round(time.time() - start, 3),
    }
    catalog_path = catalog_root / 'MapInteractiveCatalog.json'
    object_catalog_path = catalog_root / 'MapObjectTemplateCatalog.json'
    trap_catalog_path = catalog_root / 'MapTrapScriptCatalog.json'
    trap_action_catalog_path = catalog_root / 'MapTrapActionCatalog.json'
    object_script_catalog_path = catalog_root / 'MapObjectScriptCatalog.json'
    object_action_catalog_path = catalog_root / 'MapObjectActionCatalog.json'
    coverage_path = catalog_root / 'MapInteractiveCoverage.json'
    object_coverage_path = catalog_root / 'MapObjectSpriteCoverage.json'
    trap_coverage_path = catalog_root / 'MapTrapScriptCoverage.json'
    write_json(catalog_path, catalog)
    write_json(object_catalog_path, object_catalog)
    write_json(trap_catalog_path, trap_catalog)
    write_json(trap_action_catalog_path, trap_action_catalog)
    write_json(object_script_catalog_path, object_script_catalog)
    write_json(object_action_catalog_path, object_action_catalog)
    write_json(coverage_path, coverage_payload)
    write_json(object_coverage_path, {'schemaVersion': 1, 'generatedAtUtc': now, **object_coverage})
    write_json(trap_coverage_path, {'schemaVersion': 1, 'generatedAtUtc': now, **trap_coverage, **trap_action_coverage})
    write_json(catalog_root / 'MapObjectScriptCoverage.json', {
        'schemaVersion': 1, 'generatedAtUtc': now, **object_script_coverage, **object_action_coverage})
    make_meta(catalog_path)
    make_meta(object_catalog_path)
    make_meta(trap_catalog_path)
    make_meta(trap_action_catalog_path)
    make_meta(object_script_catalog_path)
    make_meta(object_action_catalog_path)
    make_meta(catalog_root / 'MapObjectScriptCoverage.json')
    make_meta(coverage_path)
    make_meta(object_coverage_path)
    make_meta(trap_coverage_path)
    print(json.dumps(coverage_payload, ensure_ascii=False, indent=2))
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
