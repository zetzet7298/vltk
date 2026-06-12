# Verify on our machine

Command:

```bash
python3 /var/www/vltk-mobile/handoff/private_owner_lien_dau_strong_v1/verify_lien_dau_on_machine.py
```

Result saved:

```text
/var/www/vltk-mobile/handoff/private_owner_lien_dau_strong_v1/VERIFY_ON_MACHINE_RESULT.json
```

Latest result: `PASS`

Verified layers:

1. Project source data contains both map aliases/geometries/server regions.
2. Raw staging package verifies.
3. Encrypted customer payload checksum exists and matches.
4. AES-256-GCM decrypt works with owner key.
5. Decrypted payload contains required catalogs/region manifests.

Key facts:

- Map 396: 176 Region_C, 122 Region_S, 68 image_names, geometry `g_7e0478bbbbc310c5`.
- Map 397: 826 Region_C, 826 Region_S, 10 image_names, geometry `g_15f3c8b336d024d4`.
- Stage package verifier: PASS.
- Encrypted payload: 13,110,505 bytes.
- Customer zip: 13,115,709 bytes.
- Decrypted payload zip: 13,109,827 bytes.
- Decrypted file count: 4070.
- Customer zip checksum: `7eb33901d19a3fdbded0e1be92303e05b49822c9d26eac56f2d1fe06a4b3c407`.

Unity note:

Unity console confirms generated visual map catalogs merged and PC map data merged. For package integrity, use the standalone verifier above because it checks project data, raw staging, encrypted payload, checksum, and decrypt contents deterministically.
