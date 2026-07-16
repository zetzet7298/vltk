# Design

## Domain Model

`RewardEvidence` separates catalog membership, selected bytes, decoded schema, and native arithmetic. Candidate rows never become active balance without binding proof.

## Application Flow

Trace `resPath -> abPath -> decrypted bytes`, then `LevelExpCalc.AddExp` and `LevelCollectItemMgr.TestRate` against the selected version.

## Interface Contract

Evidence-only; no reward API or balance change.

## Data Model

No database/config mutation.

## UI / Platform Impact

Android AssetBundle/VFS and native reward logic only.

## Observability

Record catalog/index hashes, candidate hashes, parser acceptance, native addresses, runtime identity, and failed decoders.

## Alternatives Considered

1. Use catalog `md5_ex` as plaintext: rejected as an oracle.
2. Infer XP/drop values from declarations: rejected as schema-only evidence.
