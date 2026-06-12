#!/usr/bin/env python3
import base64
import json
import sys
import zipfile
from pathlib import Path
from cryptography.hazmat.primitives.ciphers.aead import AESGCM

root = Path(__file__).resolve().parent
public = root.parent / 'customer_release_lien_dau_strong_v1' / 'LienDauMaps.vltkmap'
keys = json.loads((root / 'OWNER_KEYS.json').read_text(encoding='utf-8'))
raw = public.read_bytes()
assert raw.startswith(b'VLTKMAP1\n')
pos = len(b'VLTKMAP1\n')
header_len = int.from_bytes(raw[pos:pos+4], 'big')
pos += 4
header = json.loads(raw[pos:pos+header_len].decode('utf-8'))
pos += header_len
ciphertext = raw[pos:]
manifest = header['manifest']
aad = json.dumps(manifest, ensure_ascii=False, sort_keys=True).encode('utf-8')
plain = AESGCM(base64.b64decode(keys['aes256gcmKeyBase64'])).decrypt(base64.b64decode(header['nonce']), ciphertext, aad)
out = root / 'decrypt_check_payload.zip'
out.write_bytes(plain)
with zipfile.ZipFile(out) as z:
    names = set(z.namelist())
    required = [
        'Assets/StreamingAssets/MapAliasCatalog.json',
        'Assets/StreamingAssets/MapGeometryCatalog.json',
        'Assets/StreamingAssets/Generated/MapRegions/g_7e0478bbbbc310c5/manifest.json',
        'Assets/StreamingAssets/Generated/MapRegions/g_15f3c8b336d024d4/manifest.json',
    ]
    missing = [n for n in required if n not in names]
    print(json.dumps({'status': 'PASS' if not missing else 'FAIL', 'files': len(names), 'missing': missing}, ensure_ascii=False, indent=2))
    sys.exit(0 if not missing else 1)
