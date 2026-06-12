---
name: jx-map-encoding
description: Handling map encoding and lookup logic for JX/VLTK maps.
---
# Resolving map encoding quirks
When dealing with VLTK specific `maplist.ini` records, expect the map path (e.g., `特殊用地\武林大会专用\武林大会会场`) to be encoded in GBK while the VNG map name suffix (e.g., `540_name=Hội trường liên đấu (1)`) can be encoded in TCVN3. 
To look them up inside the Unity project:
1. Translate the GBK map path string bytes into the original path.
2. Cross-reference that mapped name inside `MapAliasCatalog.json` or `MapPortManifest.cs` via their `geometryKey` (such as `g_7e0478bbbbc310c5`).
