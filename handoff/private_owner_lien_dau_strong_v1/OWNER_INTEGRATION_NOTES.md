# Owner integration notes — StrongPack

Customer gets only `customer_release_lien_dau_strong_v1/`.

Keep private:

- `OWNER_KEYS.json`
- `LienDauMaps.payload.zip`
- raw staging folder `handoff/lien_dau_map_pack_v1/`

Runtime model:

1. Read `LienDauMaps.vltkmap`.
2. Parse magic `VLTKMAP1`, header length, JSON header.
3. Verify public manifest/AAD hash and payload sha256.
4. Decrypt ciphertext with AES-256-GCM using owner/customer-specific key.
5. Mount decrypted zip in memory or extract to app-private cache.
6. Feed `Assets/StreamingAssets` content to existing MapManager/MapRenderer.

For real customer integration, build this loader as compiled DLL/native plugin and obfuscate. Do not ship decrypt script or raw key file.
