# PC Version Priority Rule

User clarification: when the PC client/source contains multiple versions of the same skill/config/resource, the port must prefer the newest PC version.

## Rule
1. Compare available sources when data conflicts:
   - `/var/www/jx-pc/pak_unpacked/*`
   - `/var/www/jx-pc/Client 6.0/*`
   - `/var/www/jx-pc/Server 6.0/*`
   - mobile snapshot under `Assets/StreamingAssets/Reference/*`
2. Prefer the newest/update override version, not merely the mobile snapshot.
3. Record the winning source path in the evidence file for the slice.
4. Implement runtime/test parity from the winning source.

## Implication for Cai Bang SDD
For every remaining Cai Bang skill/resource audit, evidence must state whether the mobile reference matched the newest PC source or whether a newer PC override won.
