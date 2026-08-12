#!/usr/bin/env python3
import base64
import hashlib
import json
import subprocess
import sys
import zipfile
from pathlib import Path

from cryptography.hazmat.primitives.ciphers.aead import AESGCM

UNITY_ROOT = Path('/var/www/vltk-mobile')
STAGE = UNITY_ROOT / 'handoff/lien_dau_map_pack_v1'
PUBLIC = UNITY_ROOT / 'handoff/customer_release_lien_dau_strong_v1'
PRIVATE = UNITY_ROOT / 'handoff/private_owner_lien_dau_strong_v1'
SA = UNITY_ROOT / 'Assets/StreamingAssets'
EXPECTED = {
    396: {
        'nameVi': 'Hội trường liên đấu Kiệt xuất (1)',
        'geometryKey': 'g_7e0478bbbbc310c5',
        'regionC': 176,
        'regionS': 122,
        'imageNames': 68,
        'bounds': {'x': 44544.0, 'y': -51200.0, 'width': 9216.0, 'height': 6144.0},
    },
    397: {
        'nameVi': 'Đấu trường liên đấu Kiệt xuất (1)',
        'geometryKey': 'g_15f3c8b336d024d4',
        'regionC': 826,
        'regionS': 826,
        'imageNames': 10,
        'bounds': {'x': 38912.0, 'y': -62464.0, 'width': 28672.0, 'height': 14336.0},
    },
}


def load(path: Path):
    with path.open(encoding='utf-8-sig') as f:
        return json.load(f)


def count(path: Path, suffix: str):
    return sum(1 for p in path.iterdir() if p.name.endswith(suffix)) if path.exists() else -1


def sha256(path: Path):
    h = hashlib.sha256()
    with path.open('rb') as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b''):
            h.update(chunk)
    return h.hexdigest()


def check_project_data(errors, facts):
    aliases = load(SA / 'MapAliasCatalog.json')['aliases']
    alias_by_id = {a['mapId']: a for a in aliases}
    geometries = load(SA / 'MapGeometryCatalog.json')['geometries']
    geom_by_key = {g['geometryKey']: g for g in geometries}
    servers = load(SA / 'MapServerRegionCatalog.json')['geometries']
    server_by_key = {g['geometryKey']: g for g in servers}

    for map_id, exp in EXPECTED.items():
        alias = alias_by_id.get(map_id)
        if not alias:
            errors.append(f'project missing alias map {map_id}')
            continue
        if alias.get('geometryKey') != exp['geometryKey']:
            errors.append(f'map {map_id} geometryKey mismatch: {alias.get("geometryKey")}')
        if alias.get('nameVi') != exp['nameVi']:
            errors.append(f'map {map_id} name mismatch: {alias.get("nameVi")}')
        geom = geom_by_key.get(exp['geometryKey'])
        if not geom:
            errors.append(f'missing project geometry {exp["geometryKey"]}')
            continue
        srv = server_by_key.get(exp['geometryKey'])
        if not srv:
            errors.append(f'missing project server geometry {exp["geometryKey"]}')
            continue
        region_dir = SA / geom['regionFolder']
        server_dir = SA / srv['serverRegionFolder']
        image_path = region_dir / 'image_names.json'
        rc = count(region_dir, '_Region_C.dat')
        rs = count(server_dir, '_Region_S.dat')
        im = len(load(image_path)) if image_path.exists() else -1
        if rc != exp['regionC']:
            errors.append(f'map {map_id} Region_C expected {exp["regionC"]}, got {rc}')
        if rs != exp['regionS']:
            errors.append(f'map {map_id} Region_S expected {exp["regionS"]}, got {rs}')
        if im != exp['imageNames']:
            errors.append(f'map {map_id} image_names expected {exp["imageNames"]}, got {im}')
        facts[f'projectMap{map_id}'] = {'regionC': rc, 'regionS': rs, 'imageNames': im, 'geometryKey': exp['geometryKey']}


def check_stage_package(errors, facts):
    script = STAGE / 'scripts/verify_lien_dau_package.py'
    proc = subprocess.run([sys.executable, str(script), str(STAGE)], text=True, capture_output=True)
    facts['stageVerifierExitCode'] = proc.returncode
    try:
        facts['stageVerifier'] = json.loads(proc.stdout)
    except Exception:
        facts['stageVerifierRaw'] = proc.stdout[-1000:]
    if proc.returncode != 0:
        errors.append('stage package verifier failed')


def check_encrypted_package(errors, facts):
    payload = PUBLIC / 'LienDauMaps.vltkmap'
    public_zip = UNITY_ROOT / 'handoff/customer_release_lien_dau_strong_v1.zip'
    sha_file = UNITY_ROOT / 'handoff/customer_release_lien_dau_strong_v1.zip.sha256'
    if not payload.exists():
        errors.append('missing encrypted payload')
        return
    facts['encryptedPayloadBytes'] = payload.stat().st_size
    facts['encryptedPayloadSha256'] = sha256(payload)
    facts['customerZipBytes'] = public_zip.stat().st_size if public_zip.exists() else -1
    facts['customerZipSha256'] = sha256(public_zip) if public_zip.exists() else None
    if sha_file.exists() and public_zip.exists():
        expected = sha_file.read_text().split()[0]
        if expected != facts['customerZipSha256']:
            errors.append('customer zip sha256 mismatch')

    keys = load(PRIVATE / 'OWNER_KEYS.json')
    raw = payload.read_bytes()
    if not raw.startswith(b'VLTKMAP1\n'):
        errors.append('payload magic mismatch')
        return
    pos = len(b'VLTKMAP1\n')
    header_len = int.from_bytes(raw[pos:pos+4], 'big')
    pos += 4
    header = json.loads(raw[pos:pos+header_len].decode('utf-8'))
    pos += header_len
    aad = json.dumps(header['manifest'], ensure_ascii=False, sort_keys=True).encode('utf-8')
    plain = AESGCM(base64.b64decode(keys['aes256gcmKeyBase64'])).decrypt(base64.b64decode(header['nonce']), raw[pos:], aad)
    facts['decryptedZipBytes'] = len(plain)
    with zipfile.ZipFile(__import__('io').BytesIO(plain)) as z:
        names = set(z.namelist())
        required = [
            'Assets/StreamingAssets/MapAliasCatalog.json',
            'Assets/StreamingAssets/MapGeometryCatalog.json',
            'Assets/StreamingAssets/Generated/MapRegions/g_7e0478bbbbc310c5/manifest.json',
            'Assets/StreamingAssets/Generated/MapRegions/g_15f3c8b336d024d4/manifest.json',
        ]
        miss = [n for n in required if n not in names]
        facts['decryptedFileCount'] = len(names)
        if miss:
            errors.append(f'decrypted payload missing required files: {miss}')


def main():
    errors = []
    facts = {}
    check_project_data(errors, facts)
    check_stage_package(errors, facts)
    check_encrypted_package(errors, facts)
    result = {'status': 'PASS' if not errors else 'FAIL', 'errors': errors, 'facts': facts}
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0 if not errors else 1

if __name__ == '__main__':
    raise SystemExit(main())
