# Từ điển dữ liệu PostgreSQL `game.v1`

<!-- DATA_DICTIONARY_FORMAT: 1 -->
<!-- DATA_DICTIONARY_SOURCE: contracts/sql/game.v1.sql -->
<!-- DATA_DICTIONARY_COVERAGE: tables=48 columns=528 -->

> Shard CNPM pha 3 của [`../../03-du-lieu.md`](../../03-du-lieu.md). Nguồn chân lý duy nhất: [`../../contracts/sql/game.v1.sql`](../../contracts/sql/game.v1.sql).

## Phạm vi và quy ước

| Thuộc tính | Giá trị |
| --- | --- |
| Contract | `contracts/sql/game.v1.sql` |
| SHA-256 contract tại lần sinh | `6b028afd696cea82032d96453dbf14f496e78344e42239a60b0054b3cfdd0b55` |
| Coverage | `48/48` bảng; `528/528` cột `CREATE TABLE` |
| Nullability | “Có” khi khai báo `NOT NULL` hoặc thuộc `PRIMARY KEY`; còn lại “Không” |
| Default | Ghi nguyên `DEFAULT` tại cột Ràng buộc; “Không có DEFAULT” khi SQL không khai báo |
| Độ rộng | Chỉ ghi từ `length`/`octet_length` CHECK; còn lại “Không khai báo trong SQL” |
| Application invariant | Dẫn về `03-du-lieu.md`; không trình bày như PostgreSQL constraint |

Dictionary mô tả DDL, không thay thế SQL. SQL normative thắng khi có khác biệt. Các invariant transaction mà SQL không enforce đầy đủ được quản trị tại [`03-du-lieu.md`](../../03-du-lieu.md#chi-tiết-các-bảng).

## Mục lục coverage

| TT | Bảng | Số cột | Ý nghĩa |
| --- | --- | --- | --- |
| 1 | [`realms`](#1-realms) | 8 | lookup `code`; root admission/content. |
| 2 | [`accounts`](#2-accounts) | 17 | Cùng miền với Register/Login OpenAPI; email lookup không giải mã; password chỉ hash Argon2id; tham số runtime đang `BLOCKED [CẦN XÁC NHẬN]` tại mục Điểm cần xác nhận. |
| 3 | [`auth_sessions`](#3-auth-sessions) | 12 | live session theo realm/account/expiry; family để detect replay. |
| 4 | [`characters`](#4-characters) | 17 | list active theo account; purge 7 ngày; giữ tên trong cửa sổ. |
| 5 | [`character_stats`](#5-character-stats) | 16 | PK cover get character; tiền không nằm ở đây. |
| 6 | [`character_positions`](#6-character-positions) | 9 | `(realm_id,map_id)` cho hydrate/ops; chỉ server ghi. |
| 7 | [`character_skills`](#7-character-skills) | 8 | partial active skills; content giải nghĩa skill ID. |
| 8 | [`content_releases`](#8-content-releases) | 22 | partial active release; immutable khi active/retired; manifest top-level và SQL cùng provenance. |
| 9 | [`admission_tickets`](#9-admission-tickets) | 14 | Bootstrap `POST` chọn character; consume atomic trước WSS admission; không lưu raw ticket. |
| 10 | [`content_artifacts`](#10-content-artifacts) | 33 | Không ghép artifact từ snapshot khác; SPR fallback giữ frame PC, text Việt runtime và attachment/debt. |
| 11 | [`config_entries`](#11-config-entries) | 9 | Artifact provenance bắt buộc thuộc chính release của config; raw source không bị value JSON thay thế. |
| 12 | [`lua_modules`](#12-lua-modules) | 5 | stable identity qua release. |
| 13 | [`lua_module_versions`](#13-lua-module-versions) | 16 | Không ghép source hoặc whitelist release khác; API ngoài whitelist fail closed; chưa approved không activate. |
| 14 | [`inventory_items`](#14-inventory-items) | 13 | character+template active; delete là tombstone, không hard-delete trực tiếp. |
| 15 | [`wallets`](#15-wallets) | 7 | owner lookup; balance cache chỉ đổi cùng ledger transaction. |
| 16 | [`economy_transactions`](#16-economy-transactions) | 11 | Payload bất biến từ lúc tạo; `posted` chỉ sau ledger cân bằng; reversal là transaction đối ứng. |
| 17 | [`economy_entries`](#17-economy-entries) | 9 | wallet history theo descending time; append-only. |
| 18 | [`runtime_checkpoints`](#18-runtime-checkpoints) | 11 | current partial UQ; history descending tick; verify hash trước hydrate. |
| 19 | [`idempotency_keys`](#19-idempotency-keys) | 12 | expiry cleanup; response không chứa secret/token login. |
| 20 | [`outbox_events`](#20-outbox-events) | 13 | pending partial index; append in same aggregate transaction. |
| 21 | [`audit_events`](#21-audit-events) | 13 | target/actor timelines; không lưu password/token/payload PII thô. |
| 22 | [`password_reset_tokens`](#22-password-reset-tokens) | 8 | live token theo account/expiry; raw OTP/token không lưu. |
| 23 | [`world_channels`](#23-world-channels) | 9 | admission theo realm/map/status/population. |
| 24 | [`character_transfers`](#24-character-transfers) | 13 | resume/cutover theo token hash, giữ party affinity. |
| 25 | [`character_quests`](#25-character-quests) | 9 | active quest theo character/state. |
| 26 | [`quest_objectives`](#26-quest-objectives) | 7 | delta objective chính xác. |
| 27 | [`reward_grants`](#27-reward-grants) | 10 | exactly-once cho quest/event/PvP/boss/rebirth. |
| 28 | [`parties`](#28-parties) | 8 | root roster và transfer affinity. |
| 29 | [`party_members`](#29-party-members) | 7 | một party active/character. |
| 30 | [`party_invites`](#30-party-invites) | 9 | retry/expiry idempotent. |
| 31 | [`friendships`](#31-friendships) | 7 | query index cho cả hai đầu. |
| 32 | [`chat_messages`](#32-chat-messages) | 10 | channel timeline; body không ở log. |
| 33 | [`chat_reports`](#33-chat-reports) | 9 | moderation queue theo state/time. |
| 34 | [`trades`](#34-trades) | 12 | participant/state indexes; ACK sau commit. |
| 35 | [`trade_items`](#35-trade-items) | 7 | offer change invalidates confirms. |
| 36 | [`trade_currency_offers`](#36-trade-currency-offers) | 7 | ledger post atomic. |
| 37 | [`stalls`](#37-stalls) | 9 | channel ownership và lifecycle. |
| 38 | [`stall_listings`](#38-stall-listings) | 12 | partial search state/currency/price. |
| 39 | [`guilds`](#39-guilds) | 10 | stable guild root. |
| 40 | [`guild_members`](#40-guild-members) | 10 | RBAC và membership persistent. |
| 41 | [`character_mounts`](#41-character-mounts) | 12 | modifier server-side, content pin. |
| 42 | [`character_pets`](#42-character-pets) | 13 | summon/follow persistence. |
| 43 | [`character_pvp_profiles`](#43-character-pvp-profiles) | 7 | server validates mode/cooldown. |
| 44 | [`pvp_seasons`](#44-pvp-seasons) | 7 | versioned ladder boundary. |
| 45 | [`pvp_ladder_entries`](#45-pvp-ladder-entries) | 10 | rating/rank rebuild index. |
| 46 | [`game_events`](#46-game-events) | 10 | schedule/state index; durable checkpoint. |
| 47 | [`event_participants`](#47-event-participants) | 10 | deterministic ranking index. |
| 48 | [`character_rebirths`](#48-character-rebirths) | 11 | append-only rebirth audit. |

## Chi tiết bảng

### 1. `realms`

lookup `code`; root admission/content.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()` | Mã định danh của bản ghi. |
| 2 | `code` | `citext` | Không khai báo trong SQL | Có | `NOT NULL UNIQUE CHECK (code ~ '^[a-z0-9][a-z0-9-]{1,31}$')`; Không có `DEFAULT` | Giá trị `code` của bản ghi `realms` theo DDL. |
| 3 | `name` | `text` | 1..80 ký tự theo CHECK | Có | `NOT NULL CHECK (length(name) BETWEEN 1 AND 80)`; Không có `DEFAULT` | Giá trị `name` của bản ghi `realms` theo DDL. |
| 4 | `status` | `text` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 'closed' CHECK (status IN ('open','maintenance','closed'))` | Trạng thái theo miền CHECK của bảng. |
| 5 | `active_content_release_id` | `uuid` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Mã `active_content_release` trong `realms`; chỉ là FK khi DDL khai báo. |
| 6 | `created_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm tạo bản ghi. |
| 7 | `updated_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm cập nhật bản ghi. |
| 8 | `version` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (version > 0)` | Phiên bản optimistic concurrency. |

**Ràng buộc cấp bảng**

- Không có ràng buộc cấp bảng trong `CREATE TABLE`; xem constraint inline.

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `ALTER TABLE realms ADD CONSTRAINT fk_active_content FOREIGN KEY (id,active_content_release_id) REFERENCES content_releases(realm_id,id) DEFERRABLE INITIALLY DEFERRED`

### 2. `accounts`

Cùng miền với Register/Login OpenAPI; email lookup không giải mã; password chỉ hash Argon2id; tham số runtime đang `BLOCKED [CẦN XÁC NHẬN]` tại mục Điểm cần xác nhận.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-02-01` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL REFERENCES realms(id)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-02-01` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `account_name` | `citext` | 3..32 ký tự theo CHECK | Có | `NOT NULL CHECK (length(account_name) BETWEEN 3 AND 32)`; Không có `DEFAULT` | Giá trị `account_name` của bản ghi `accounts` theo DDL. |
| 4 | `password_hash` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Hash mật khẩu; không phải plaintext. |
| 5 | `status` | `text` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 'active' CHECK (status IN ('active','locked','banned','disabled'))` | Trạng thái theo miền CHECK của bảng. |
| 6 | `email_ciphertext` | `bytea` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Email ở dạng ciphertext. |
| 7 | `email_lookup_hmac` | `bytea` | 32 byte theo CHECK | Không | `CHECK (email_lookup_hmac IS NULL OR octet_length(email_lookup_hmac)=32)`; Không có `DEFAULT` | HMAC dùng lookup email. |
| 8 | `email_verified_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm `email_verified` của bản ghi. |
| 9 | `otp_secret_ciphertext` | `bytea` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Giá trị `otp_secret` ở dạng ciphertext. |
| 10 | `token_version` | `integer` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (token_version > 0)` | Giá trị `token_version` của bản ghi `accounts` theo DDL. |
| 11 | `legacy_acc_name` | `text` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Giá trị `legacy_acc_name` của bản ghi `accounts` theo DDL. |
| 12 | `service_flag` | `integer` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0` | Giá trị `service_flag` của bản ghi `accounts` theo DDL. |
| 13 | `ext_point` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0 CHECK (ext_point >= 0)` | Giá trị `ext_point` của bản ghi `accounts` theo DDL. |
| 14 | `created_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm tạo bản ghi. |
| 15 | `updated_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm cập nhật bản ghi. |
| 16 | `deleted_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm soft-delete bản ghi. |
| 17 | `version` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (version > 0)` | Phiên bản optimistic concurrency. |

**Ràng buộc cấp bảng**

- `RB-02-01`: `UNIQUE (realm_id,id)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE UNIQUE INDEX uq_accounts_active_name ON accounts(realm_id,account_name) WHERE deleted_at IS NULL`
- `CREATE UNIQUE INDEX uq_accounts_active_email ON accounts(realm_id,email_lookup_hmac) WHERE deleted_at IS NULL AND email_lookup_hmac IS NOT NULL`

### 3. `auth_sessions`

live session theo realm/account/expiry; family để detect replay.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-03-01`, `RB-03-03`, `RB-03-04` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-03-01`, `RB-03-03`, `RB-03-04` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `account_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-03-01`, `RB-03-04` | Mã `account` trong `auth_sessions`; chỉ là FK khi DDL khai báo. |
| 4 | `refresh_token_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL UNIQUE`; Không có `DEFAULT` | Mã `refresh_token` trong `auth_sessions`; chỉ là FK khi DDL khai báo. |
| 5 | `refresh_token_hash` | `bytea` | 32 byte theo CHECK | Có | `NOT NULL CHECK (octet_length(refresh_token_hash)=32)`; Không có `DEFAULT` | Hash refresh token; không lưu token thô. |
| 6 | `token_family_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Mã `token_family` trong `auth_sessions`; chỉ là FK khi DDL khai báo. |
| 7 | `device_id` | `text` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Mã `device` trong `auth_sessions`; chỉ là FK khi DDL khai báo. |
| 8 | `issued_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()`; Ràng buộc cấp bảng: `RB-03-02` | Thời điểm `issued` của bản ghi. |
| 9 | `expires_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-03-02` | Thời điểm `expires` của bản ghi. |
| 10 | `rotated_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm `rotated` của bản ghi. |
| 11 | `revoked_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm `revoked` của bản ghi. |
| 12 | `revoke_reason` | `text` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Giá trị `revoke_reason` của bản ghi `auth_sessions` theo DDL. |

**Ràng buộc cấp bảng**

- `RB-03-01`: `FOREIGN KEY (realm_id,account_id) REFERENCES accounts(realm_id,id) ON DELETE CASCADE`
- `RB-03-02`: `CHECK (expires_at > issued_at)`
- `RB-03-03`: `UNIQUE (realm_id,id)`
- `RB-03-04`: `UNIQUE (realm_id,id,account_id)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE INDEX ix_auth_live ON auth_sessions(realm_id,account_id,expires_at) WHERE revoked_at IS NULL`

### 4. `characters`

list active theo account; purge 7 ngày; giữ tên trong cửa sổ.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-04-01`, `RB-04-02`, `RB-04-03` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-04-01`, `RB-04-02`, `RB-04-03`, `RB-04-04` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `account_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-04-01`, `RB-04-03` | Mã `account` trong `characters`; chỉ là FK khi DDL khai báo. |
| 4 | `legacy_role_id` | `bigint` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-04-04` | Mã `legacy_role` trong `characters`; chỉ là FK khi DDL khai báo. |
| 5 | `name` | `citext` | 2..24 ký tự theo CHECK | Có | `NOT NULL CHECK (length(name) BETWEEN 2 AND 24)`; Không có `DEFAULT` | Giá trị `name` của bản ghi `characters` theo DDL. |
| 6 | `gender` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (gender IN ('male','female'))`; Không có `DEFAULT` | Giá trị `gender` của bản ghi `characters` theo DDL. |
| 7 | `homeland_id` | `integer` | Không khai báo trong SQL | Có | `NOT NULL CHECK (homeland_id>0)`; Không có `DEFAULT` | Mã `homeland` trong `characters`; chỉ là FK khi DDL khai báo. |
| 8 | `character_slot` | `smallint` | Không khai báo trong SQL | Có | `NOT NULL CHECK (character_slot BETWEEN 1 AND 3)`; Không có `DEFAULT` | Giá trị `character_slot` của bản ghi `characters` theo DDL. |
| 9 | `faction` | `smallint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT -1 CHECK (faction BETWEEN -1 AND 9)` | Giá trị `faction` của bản ghi `characters` theo DDL. |
| 10 | `series` | `smallint` | Không khai báo trong SQL | Có | `NOT NULL CHECK (series BETWEEN 0 AND 4)`; Không có `DEFAULT` | Giá trị `series` của bản ghi `characters` theo DDL. |
| 11 | `level` | `smallint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (level BETWEEN 1 AND 200)` | Giá trị `level` của bản ghi `characters` theo DDL. |
| 12 | `appearance` | `jsonb` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT '{}' CHECK (jsonb_typeof(appearance)='object')` | Giá trị `appearance` của bản ghi `characters` theo DDL. |
| 13 | `created_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm tạo bản ghi. |
| 14 | `updated_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm cập nhật bản ghi. |
| 15 | `deleted_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-04-05` | Thời điểm soft-delete bản ghi. |
| 16 | `purge_after` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-04-05` | Giá trị `purge_after` của bản ghi `characters` theo DDL. |
| 17 | `version` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (version > 0)` | Phiên bản optimistic concurrency. |

**Ràng buộc cấp bảng**

- `RB-04-01`: `FOREIGN KEY (realm_id,account_id) REFERENCES accounts(realm_id,id)`
- `RB-04-02`: `UNIQUE (realm_id,id)`
- `RB-04-03`: `UNIQUE (realm_id,id,account_id)`
- `RB-04-04`: `UNIQUE (realm_id,legacy_role_id)`
- `RB-04-05`: `CHECK ((deleted_at IS NULL AND purge_after IS NULL) OR purge_after = deleted_at + interval '7 days')`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE UNIQUE INDEX uq_characters_active_name ON characters(realm_id,name) WHERE deleted_at IS NULL`
- `CREATE UNIQUE INDEX uq_characters_active_slot ON characters(realm_id,account_id,character_slot) WHERE deleted_at IS NULL`
- `CREATE INDEX ix_characters_account ON characters(realm_id,account_id,created_at) WHERE deleted_at IS NULL`
- `CREATE INDEX ix_characters_purge ON characters(purge_after) WHERE purge_after IS NOT NULL`

### 5. `character_stats`

PK cover get character; tiền không nằm ở đây.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-05-01`, `RB-05-02` | Realm sở hữu bản ghi và ranh giới RLS. |
| 2 | `character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-05-01`, `RB-05-02` | Mã `character` trong `character_stats`; chỉ là FK khi DDL khai báo. |
| 3 | `experience` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0 CHECK (experience>=0)` | Giá trị `experience` của bản ghi `character_stats` theo DDL. |
| 4 | `trans_life` | `integer` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0 CHECK (trans_life>=0)` | Giá trị `trans_life` của bản ghi `character_stats` theo DDL. |
| 5 | `free_point` | `integer` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0 CHECK (free_point>=0)` | Giá trị `free_point` của bản ghi `character_stats` theo DDL. |
| 6 | `magic_point` | `integer` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0 CHECK (magic_point>=0)` | Giá trị `magic_point` của bản ghi `character_stats` theo DDL. |
| 7 | `strength` | `integer` | Không khai báo trong SQL | Có | `NOT NULL CHECK (strength>=0)`; Không có `DEFAULT` | Giá trị `strength` của bản ghi `character_stats` theo DDL. |
| 8 | `dexterity` | `integer` | Không khai báo trong SQL | Có | `NOT NULL CHECK (dexterity>=0)`; Không có `DEFAULT` | Giá trị `dexterity` của bản ghi `character_stats` theo DDL. |
| 9 | `vitality` | `integer` | Không khai báo trong SQL | Có | `NOT NULL CHECK (vitality>=0)`; Không có `DEFAULT` | Giá trị `vitality` của bản ghi `character_stats` theo DDL. |
| 10 | `spirit` | `integer` | Không khai báo trong SQL | Có | `NOT NULL CHECK (spirit>=0)`; Không có `DEFAULT` | Giá trị `spirit` của bản ghi `character_stats` theo DDL. |
| 11 | `repute` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0 CHECK (repute>=0)` | Giá trị `repute` của bản ghi `character_stats` theo DDL. |
| 12 | `current_life` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (current_life>=0)` | Giá trị `current_life` của bản ghi `character_stats` theo DDL. |
| 13 | `current_mana` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0 CHECK (current_mana>=0)` | Giá trị `current_mana` của bản ghi `character_stats` theo DDL. |
| 14 | `current_stamina` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0 CHECK (current_stamina>=0)` | Giá trị `current_stamina` của bản ghi `character_stats` theo DDL. |
| 15 | `updated_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm cập nhật bản ghi. |
| 16 | `version` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (version>0)` | Phiên bản optimistic concurrency. |

**Ràng buộc cấp bảng**

- `RB-05-01`: `PRIMARY KEY (realm_id,character_id)`
- `RB-05-02`: `FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- Không có object bổ sung gắn trực tiếp trong DDL canonical.

### 6. `character_positions`

`(realm_id,map_id)` cho hydrate/ops; chỉ server ghi.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-06-01`, `RB-06-02` | Realm sở hữu bản ghi và ranh giới RLS. |
| 2 | `character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-06-01`, `RB-06-02` | Mã `character` trong `character_positions`; chỉ là FK khi DDL khai báo. |
| 3 | `map_id` | `integer` | Không khai báo trong SQL | Có | `NOT NULL CHECK (map_id>=0)`; Không có `DEFAULT` | Mã `map` trong `character_positions`; chỉ là FK khi DDL khai báo. |
| 4 | `pos_x` | `integer` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Giá trị `pos_x` của bản ghi `character_positions` theo DDL. |
| 5 | `pos_y` | `integer` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Giá trị `pos_y` của bản ghi `character_positions` theo DDL. |
| 6 | `facing_millirad` | `integer` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0 CHECK (facing_millirad BETWEEN 0 AND 6283)` | Giá trị `facing_millirad` của bản ghi `character_positions` theo DDL. |
| 7 | `server_tick` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0 CHECK (server_tick>=0)` | Giá trị `server_tick` của bản ghi `character_positions` theo DDL. |
| 8 | `updated_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm cập nhật bản ghi. |
| 9 | `version` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (version>0)` | Phiên bản optimistic concurrency. |

**Ràng buộc cấp bảng**

- `RB-06-01`: `PRIMARY KEY (realm_id,character_id)`
- `RB-06-02`: `FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE INDEX ix_positions_map ON character_positions(realm_id,map_id)`

### 7. `character_skills`

partial active skills; content giải nghĩa skill ID.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-07-01`, `RB-07-02` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-07-01`, `RB-07-02`, `RB-07-03` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-07-01`, `RB-07-03` | Mã `character` trong `character_skills`; chỉ là FK khi DDL khai báo. |
| 4 | `skill_id` | `integer` | Không khai báo trong SQL | Có | `NOT NULL CHECK (skill_id>0)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-07-03` | Mã `skill` trong `character_skills`; chỉ là FK khi DDL khai báo. |
| 5 | `level` | `smallint` | Không khai báo trong SQL | Có | `NOT NULL CHECK (level>0)`; Không có `DEFAULT` | Giá trị `level` của bản ghi `character_skills` theo DDL. |
| 6 | `is_active` | `boolean` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT true` | Giá trị `is_active` của bản ghi `character_skills` theo DDL. |
| 7 | `last_cast_tick` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0 CHECK (last_cast_tick>=0)` | Giá trị `last_cast_tick` của bản ghi `character_skills` theo DDL. |
| 8 | `version` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (version>0)` | Phiên bản optimistic concurrency. |

**Ràng buộc cấp bảng**

- `RB-07-01`: `FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id)`
- `RB-07-02`: `UNIQUE (realm_id,id)`
- `RB-07-03`: `UNIQUE (realm_id,character_id,skill_id)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- Không có object bổ sung gắn trực tiếp trong DDL canonical.

### 8. `content_releases`

partial active release; immutable khi active/retired; manifest top-level và SQL cùng provenance.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-08-01`, `RB-08-02`, `RB-08-03` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL REFERENCES realms(id)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-08-01`, `RB-08-02`, `RB-08-03`, `RB-08-04`, `RB-08-05` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `version` | `text` | 1..64 ký tự theo CHECK | Có | `NOT NULL CHECK (length(version) BETWEEN 1 AND 64)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-08-04` | Phiên bản optimistic concurrency. |
| 4 | `user_facing_locale` | `text` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 'vi' CHECK (user_facing_locale='vi')` | Giá trị `user_facing_locale` của bản ghi `content_releases` theo DDL. |
| 5 | `hot_reload_allowed` | `boolean` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT false CHECK (hot_reload_allowed=false)` | Giá trị `hot_reload_allowed` của bản ghi `content_releases` theo DDL. |
| 6 | `source_snapshot_id` | `text` | 7..128 ký tự theo CHECK | Có | `NOT NULL CHECK (length(source_snapshot_id) BETWEEN 7 AND 128)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-08-02` | Mã `source_snapshot` trong `content_releases`; chỉ là FK khi DDL khai báo. |
| 7 | `source_root` | `text` | 1..1024 ký tự theo CHECK | Có | `NOT NULL CHECK (length(source_root) BETWEEN 1 AND 1024)`; Không có `DEFAULT` | Giá trị `source_root` của bản ghi `content_releases` theo DDL. |
| 8 | `source_snapshot_sha256` | `bytea` | 32 byte theo CHECK | Có | `NOT NULL CHECK (octet_length(source_snapshot_sha256)=32)`; Không có `DEFAULT` | Giá trị kiểm chứng `source_snapshot_sha256` theo DDL. |
| 9 | `source_vcs_revision` | `text` | 7..128 ký tự theo CHECK | Không | `CHECK (source_vcs_revision IS NULL OR length(source_vcs_revision) BETWEEN 7 AND 128)`; Không có `DEFAULT` | Giá trị `source_vcs_revision` của bản ghi `content_releases` theo DDL. |
| 10 | `catalog_generator_revision` | `text` | 7..128 ký tự theo CHECK | Có | `NOT NULL CHECK (length(catalog_generator_revision) BETWEEN 7 AND 128)`; Không có `DEFAULT` | Giá trị `catalog_generator_revision` của bản ghi `content_releases` theo DDL. |
| 11 | `lua_runtime` | `text` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 'Lua 5.1' CHECK (lua_runtime='Lua 5.1')` | Giá trị `lua_runtime` của bản ghi `content_releases` theo DDL. |
| 12 | `lua_sandbox_policy_version` | `text` | 1..64 ký tự theo CHECK | Có | `NOT NULL CHECK (length(lua_sandbox_policy_version) BETWEEN 1 AND 64)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-08-03` | Giá trị `lua_sandbox_policy_version` của bản ghi `content_releases` theo DDL. |
| 13 | `lua_host_api_whitelist` | `jsonb` | Không khai báo trong SQL | Có | `NOT NULL CHECK (jsonb_typeof(lua_host_api_whitelist)='array' AND jsonb_array_length(lua_host_api_whitelist)>0)`; Không có `DEFAULT` | Giá trị `lua_host_api_whitelist` của bản ghi `content_releases` theo DDL. |
| 14 | `lua_host_api_whitelist_sha256` | `bytea` | 32 byte theo CHECK | Có | `NOT NULL CHECK (octet_length(lua_host_api_whitelist_sha256)=32)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-08-03` | Giá trị kiểm chứng `lua_host_api_whitelist_sha256` theo DDL. |
| 15 | `manifest_sha256` | `bytea` | 32 byte theo CHECK | Có | `NOT NULL CHECK (octet_length(manifest_sha256)=32)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-08-05` | Giá trị kiểm chứng `manifest_sha256` theo DDL. |
| 16 | `signature` | `bytea` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Giá trị `signature` của bản ghi `content_releases` theo DDL. |
| 17 | `signing_key_id` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Mã `signing_key` trong `content_releases`; chỉ là FK khi DDL khai báo. |
| 18 | `status` | `text` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 'staged' CHECK (status IN ('staged','active','retired','rejected'))` | Trạng thái theo miền CHECK của bảng. |
| 19 | `created_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm tạo bản ghi. |
| 20 | `activated_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm `activated` của bản ghi. |
| 21 | `retired_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm `retired` của bản ghi. |
| 22 | `created_by` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Giá trị `created_by` của bản ghi `content_releases` theo DDL. |

**Ràng buộc cấp bảng**

- `RB-08-01`: `UNIQUE (realm_id,id)`
- `RB-08-02`: `UNIQUE (realm_id,id,source_snapshot_id)`
- `RB-08-03`: `UNIQUE (realm_id,id,lua_sandbox_policy_version,lua_host_api_whitelist_sha256)`
- `RB-08-04`: `UNIQUE (realm_id,version)`
- `RB-08-05`: `UNIQUE (realm_id,manifest_sha256)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE UNIQUE INDEX uq_content_active ON content_releases(realm_id) WHERE status='active'`

### 9. `admission_tickets`

Bootstrap `POST` chọn character; consume atomic trước WSS admission; không lưu raw ticket.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-09-02`, `RB-09-04`, `RB-09-05`, `RB-09-06`, `RB-09-07` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-09-02`, `RB-09-04`, `RB-09-05`, `RB-09-06`, `RB-09-07` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `auth_session_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-09-04` | Mã `auth_session` trong `admission_tickets`; chỉ là FK khi DDL khai báo. |
| 4 | `account_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-09-04`, `RB-09-05`, `RB-09-06` | Mã `account` trong `admission_tickets`; chỉ là FK khi DDL khai báo. |
| 5 | `character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-09-06` | Mã `character` trong `admission_tickets`; chỉ là FK khi DDL khai báo. |
| 6 | `content_release_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-09-07` | Release content pin cho bản ghi. |
| 7 | `ticket_hash` | `bytea` | 32 byte theo CHECK | Có | `NOT NULL CHECK (octet_length(ticket_hash)=32)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-09-03` | Hash vé admission; không lưu vé thô. |
| 8 | `protocol_version` | `text` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 'game.v1'` | Giá trị `protocol_version` của bản ghi `admission_tickets` theo DDL. |
| 9 | `session_epoch` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL CHECK (session_epoch>0)`; Không có `DEFAULT` | Giá trị `session_epoch` của bản ghi `admission_tickets` theo DDL. |
| 10 | `issued_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()`; Ràng buộc cấp bảng: `RB-09-01` | Thời điểm `issued` của bản ghi. |
| 11 | `expires_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-09-01` | Thời điểm `expires` của bản ghi. |
| 12 | `consumed_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm `consumed` của bản ghi. |
| 13 | `revoked_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm `revoked` của bản ghi. |
| 14 | `reconnect_grace_seconds` | `integer` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 15 CHECK (reconnect_grace_seconds=15)` | Giá trị `reconnect_grace_seconds` của bản ghi `admission_tickets` theo DDL. |

Gate 0 addendum: DDL hiện thêm `content_releases.catalog_union_size/catalog_union_sha256/runtime_skill_policy`, `encounter_preload_acks` và `combat_lifecycle_events`. Từ điển chi tiết cho bảng mới sẽ được regenerate từ SQL sau khi Go runtime chọn migration generator; contract hiện hành là `contracts/sql/game.v1.sql`.

**Ràng buộc cấp bảng**

- `RB-09-01`: `CHECK (expires_at>issued_at)`
- `RB-09-02`: `UNIQUE (realm_id,id)`
- `RB-09-03`: `UNIQUE (ticket_hash)`
- `RB-09-04`: `FOREIGN KEY (realm_id,auth_session_id,account_id) REFERENCES auth_sessions(realm_id,id,account_id)`
- `RB-09-05`: `FOREIGN KEY (realm_id,account_id) REFERENCES accounts(realm_id,id)`
- `RB-09-06`: `FOREIGN KEY (realm_id,character_id,account_id) REFERENCES characters(realm_id,id,account_id)`
- `RB-09-07`: `FOREIGN KEY (realm_id,content_release_id) REFERENCES content_releases(realm_id,id)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE UNIQUE INDEX uq_outstanding_admission ON admission_tickets(realm_id,account_id) WHERE consumed_at IS NULL AND revoked_at IS NULL`

### 10. `content_artifacts`

Không ghép artifact từ snapshot khác; SPR fallback giữ frame PC, text Việt runtime và attachment/debt.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-10-05`, `RB-10-06`, `RB-10-07` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-10-05`, `RB-10-06`, `RB-10-07`, `RB-10-08` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `content_release_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-10-05`, `RB-10-07`, `RB-10-08` | Release content pin cho bản ghi. |
| 4 | `logical_path` | `text` | 1..512 ký tự theo CHECK | Có | `NOT NULL CHECK (length(logical_path) BETWEEN 1 AND 512 AND logical_path !~ '(^/\|\.\.)')`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-10-08` | Giá trị `logical_path` của bản ghi `content_artifacts` theo DDL. |
| 5 | `kind` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (kind IN ('config','lua','map','sprite','audio','binary','localization'))`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-10-04` | Giá trị `kind` của bản ghi `content_artifacts` theo DDL. |
| 6 | `media_type` | `text` | 3..128 ký tự theo CHECK | Có | `NOT NULL CHECK (length(media_type) BETWEEN 3 AND 128)`; Không có `DEFAULT` | Giá trị `media_type` của bản ghi `content_artifacts` theo DDL. |
| 7 | `encoding` | `text` | Không khai báo trong SQL | Không | `CHECK (encoding IS NULL OR length(encoding)<=32)`; Không có `DEFAULT` | Giá trị `encoding` của bản ghi `content_artifacts` theo DDL. |
| 8 | `size_bytes` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL CHECK (size_bytes>=0)`; Không có `DEFAULT` | Giá trị `size_bytes` của bản ghi `content_artifacts` theo DDL. |
| 9 | `sha256` | `bytea` | 32 byte theo CHECK | Có | `NOT NULL CHECK (octet_length(sha256)=32)`; Không có `DEFAULT` | Giá trị `sha256` của bản ghi `content_artifacts` theo DDL. |
| 10 | `object_uri` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (length(object_uri)>0)`; Không có `DEFAULT` | Giá trị `object_uri` của bản ghi `content_artifacts` theo DDL. |
| 11 | `source_snapshot_id` | `text` | 7..128 ký tự theo CHECK | Có | `NOT NULL CHECK (length(source_snapshot_id) BETWEEN 7 AND 128)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-10-05` | Mã `source_snapshot` trong `content_artifacts`; chỉ là FK khi DDL khai báo. |
| 12 | `source_path` | `text` | Không khai báo trong SQL | Không | `CHECK (source_path IS NULL OR length(source_path)<=1024)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-10-03` | Giá trị `source_path` của bản ghi `content_artifacts` theo DDL. |
| 13 | `source_package` | `text` | Không khai báo trong SQL | Không | `CHECK (source_package IS NULL OR length(source_package)<=256)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-10-03` | Giá trị `source_package` của bản ghi `content_artifacts` theo DDL. |
| 14 | `source_uid` | `integer` | Không khai báo trong SQL | Không | `CHECK (source_uid>=0)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-10-03` | Giá trị `source_uid` của bản ghi `content_artifacts` theo DDL. |
| 15 | `source_encoding` | `text` | Không khai báo trong SQL | Không | `CHECK (source_encoding IS NULL OR length(source_encoding)<=32)`; Không có `DEFAULT` | Giá trị `source_encoding` của bản ghi `content_artifacts` theo DDL. |
| 16 | `discovery_tool` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (discovery_tool IN ('semble','gitnexus','vltktool','manual','runtime','importer'))`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-10-04` | Giá trị `discovery_tool` của bản ghi `content_artifacts` theo DDL. |
| 17 | `discovery_tool_revision` | `text` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-10-04` | Giá trị `discovery_tool_revision` của bản ghi `content_artifacts` theo DDL. |
| 18 | `query_used` | `text` | Không khai báo trong SQL | Không | `CHECK (query_used IS NULL OR length(query_used)<=2048)`; Không có `DEFAULT` | Giá trị `query_used` của bản ghi `content_artifacts` theo DDL. |
| 19 | `evidence_note` | `text` | Không khai báo trong SQL | Không | `CHECK (evidence_note IS NULL OR length(evidence_note)<=4096)`; Không có `DEFAULT` | Giá trị `evidence_note` của bản ghi `content_artifacts` theo DDL. |
| 20 | `parser_name` | `text` | 1..128 ký tự theo CHECK | Có | `NOT NULL CHECK (length(parser_name) BETWEEN 1 AND 128)`; Không có `DEFAULT` | Giá trị `parser_name` của bản ghi `content_artifacts` theo DDL. |
| 21 | `parser_version` | `text` | 1..64 ký tự theo CHECK | Có | `NOT NULL CHECK (length(parser_version) BETWEEN 1 AND 64)`; Không có `DEFAULT` | Giá trị `parser_version` của bản ghi `content_artifacts` theo DDL. |
| 22 | `source_package_index` | `integer` | Không khai báo trong SQL | Không | `CHECK (source_package_index>=0)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-10-04` | Giá trị `source_package_index` của bản ghi `content_artifacts` theo DDL. |
| 23 | `winner_status` | `text` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-10-04` | Giá trị `winner_status` của bản ghi `content_artifacts` theo DDL. |
| 24 | `logical_path_bytes` | `bytea` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-10-04` | Giá trị `logical_path_bytes` của bản ghi `content_artifacts` theo DDL. |
| 25 | `raw_sha256` | `bytea` | 32 byte theo CHECK | Không | Không có constraint inline; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-10-01`, `RB-10-04` | Giá trị kiểm chứng `raw_sha256` theo DDL. |
| 26 | `decoded_sha256` | `bytea` | 32 byte theo CHECK | Không | Không có constraint inline; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-10-02`, `RB-10-04` | Giá trị kiểm chứng `decoded_sha256` theo DDL. |
| 27 | `source_locale` | `text` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-10-04` | Giá trị `source_locale` của bản ghi `content_artifacts` theo DDL. |
| 28 | `user_facing_locale` | `text` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-10-04` | Giá trị `user_facing_locale` của bản ghi `content_artifacts` theo DDL. |
| 29 | `vietnamese_mode` | `text` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-10-04` | Giá trị `vietnamese_mode` của bản ghi `content_artifacts` theo DDL. |
| 30 | `visual_debt_id` | `text` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-10-04` | Mã `visual_debt` trong `content_artifacts`; chỉ là FK khi DDL khai báo. |
| 31 | `fallback_policy_id` | `text` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-10-04` | Mã `fallback_policy` trong `content_artifacts`; chỉ là FK khi DDL khai báo. |
| 32 | `parity_status` | `text` | Không khai báo trong SQL | Không | `CHECK (parity_status IS NULL OR parity_status IN ('BLOCKED','GOLDEN_READY','PARITY_DONE'))`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-10-04` | Giá trị `parity_status` của bản ghi `content_artifacts` theo DDL. |
| 33 | `normalized_sha256` | `bytea` | 32 byte theo CHECK | Không | `CHECK (normalized_sha256 IS NULL OR octet_length(normalized_sha256)=32)`; Không có `DEFAULT` | Giá trị kiểm chứng `normalized_sha256` theo DDL. |

**Ràng buộc cấp bảng**

- `RB-10-01`: `CHECK (raw_sha256 IS NULL OR octet_length(raw_sha256)=32)`
- `RB-10-02`: `CHECK (decoded_sha256 IS NULL OR octet_length(decoded_sha256)=32)`
- `RB-10-03`: `CHECK ((source_path IS NOT NULL) OR (source_package IS NOT NULL AND source_uid IS NOT NULL))`
- `RB-10-04`: `CHECK (kind <> 'sprite' OR (source_locale IN ('vi','zh','textless') AND user_facing_locale='vi' AND ((source_locale='vi' AND vietnamese_mode='native') OR (source_locale IN ('zh','textless') AND vietnamese_mode='runtime-text' AND visual_debt_id IS NOT NULL AND fallback_policy_id='SPR-FALLBACK-VI-RUNTIME-TEXT-V1')) AND source_package_index IS NOT NULL AND winner_status='resolved-first-match' AND logical_path_bytes IS NOT NULL AND raw_sha256 IS NOT NULL AND decoded_sha256 IS NOT NULL AND discovery_tool='vltktool' AND discovery_tool_revision IS NOT NULL AND parity_status IS NOT NULL))`
- `RB-10-05`: `FOREIGN KEY (realm_id,content_release_id,source_snapshot_id) REFERENCES content_releases(realm_id,id,source_snapshot_id)`
- `RB-10-06`: `UNIQUE (realm_id,id)`
- `RB-10-07`: `UNIQUE (realm_id,id,content_release_id)`
- `RB-10-08`: `UNIQUE (realm_id,content_release_id,logical_path)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE INDEX ix_artifact_source ON content_artifacts(realm_id,source_path,source_uid)`

### 11. `config_entries`

Artifact provenance bắt buộc thuộc chính release của config; raw source không bị value JSON thay thế.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-11-01`, `RB-11-02`, `RB-11-03` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-11-01`, `RB-11-02`, `RB-11-03`, `RB-11-04` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `content_release_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-11-01`, `RB-11-02`, `RB-11-04` | Release content pin cho bản ghi. |
| 4 | `source_artifact_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-11-02` | Mã `source_artifact` trong `config_entries`; chỉ là FK khi DDL khai báo. |
| 5 | `namespace` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-11-04` | Giá trị `namespace` của bản ghi `config_entries` theo DDL. |
| 6 | `entry_key` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-11-04` | Giá trị `entry_key` của bản ghi `config_entries` theo DDL. |
| 7 | `value` | `jsonb` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Giá trị `value` của bản ghi `config_entries` theo DDL. |
| 8 | `value_sha256` | `bytea` | 32 byte theo CHECK | Có | `NOT NULL CHECK (octet_length(value_sha256)=32)`; Không có `DEFAULT` | Giá trị kiểm chứng `value_sha256` theo DDL. |
| 9 | `source_line` | `integer` | Không khai báo trong SQL | Không | `CHECK (source_line>0)`; Không có `DEFAULT` | Giá trị `source_line` của bản ghi `config_entries` theo DDL. |

**Ràng buộc cấp bảng**

- `RB-11-01`: `FOREIGN KEY (realm_id,content_release_id) REFERENCES content_releases(realm_id,id)`
- `RB-11-02`: `FOREIGN KEY (realm_id,source_artifact_id,content_release_id) REFERENCES content_artifacts(realm_id,id,content_release_id)`
- `RB-11-03`: `UNIQUE (realm_id,id)`
- `RB-11-04`: `UNIQUE (realm_id,content_release_id,namespace,entry_key)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- Không có object bổ sung gắn trực tiếp trong DDL canonical.

### 12. `lua_modules`

stable identity qua release.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-12-01` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-12-01`, `RB-12-02` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `module_key` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-12-02` | Giá trị `module_key` của bản ghi `lua_modules` theo DDL. |
| 4 | `created_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm tạo bản ghi. |
| 5 | `retired_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm `retired` của bản ghi. |

**Ràng buộc cấp bảng**

- `RB-12-01`: `UNIQUE (realm_id,id)`
- `RB-12-02`: `UNIQUE (realm_id,module_key)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- Không có object bổ sung gắn trực tiếp trong DDL canonical.

### 13. `lua_module_versions`

Không ghép source hoặc whitelist release khác; API ngoài whitelist fail closed; chưa approved không activate.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-13-01`, `RB-13-02`, `RB-13-03`, `RB-13-04` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-13-01`, `RB-13-02`, `RB-13-03`, `RB-13-04`, `RB-13-05` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `lua_module_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-13-01`, `RB-13-05` | Mã `lua_module` trong `lua_module_versions`; chỉ là FK khi DDL khai báo. |
| 4 | `content_release_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-13-02`, `RB-13-03`, `RB-13-05` | Release content pin cho bản ghi. |
| 5 | `source_artifact_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-13-03` | Mã `source_artifact` trong `lua_module_versions`; chỉ là FK khi DDL khai báo. |
| 6 | `source_sha256` | `bytea` | 32 byte theo CHECK | Có | `NOT NULL CHECK (octet_length(source_sha256)=32)`; Không có `DEFAULT` | Giá trị kiểm chứng `source_sha256` theo DDL. |
| 7 | `bytecode_sha256` | `bytea` | 32 byte theo CHECK | Không | `CHECK (bytecode_sha256 IS NULL OR octet_length(bytecode_sha256)=32)`; Không có `DEFAULT` | Giá trị kiểm chứng `bytecode_sha256` theo DDL. |
| 8 | `lua_version` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (lua_version='5.1')`; Không có `DEFAULT` | Giá trị `lua_version` của bản ghi `lua_module_versions` theo DDL. |
| 9 | `sandbox_policy_version` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-13-02` | Giá trị `sandbox_policy_version` của bản ghi `lua_module_versions` theo DDL. |
| 10 | `host_api_whitelist_sha256` | `bytea` | 32 byte theo CHECK | Có | `NOT NULL CHECK (octet_length(host_api_whitelist_sha256)=32)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-13-02` | Giá trị kiểm chứng `host_api_whitelist_sha256` theo DDL. |
| 11 | `deterministic` | `boolean` | Không khai báo trong SQL | Có | `NOT NULL CHECK (deterministic=true)`; Không có `DEFAULT` | Giá trị `deterministic` của bản ghi `lua_module_versions` theo DDL. |
| 12 | `instruction_limit` | `integer` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 100000 CHECK (instruction_limit=100000)` | Giá trị `instruction_limit` của bản ghi `lua_module_versions` theo DDL. |
| 13 | `timeout_ms` | `integer` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 5 CHECK (timeout_ms=5)` | Giá trị `timeout_ms` của bản ghi `lua_module_versions` theo DDL. |
| 14 | `memory_limit_bytes` | `integer` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 8388608 CHECK (memory_limit_bytes=8388608)` | Giá trị `memory_limit_bytes` của bản ghi `lua_module_versions` theo DDL. |
| 15 | `approved_by` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Giá trị `approved_by` của bản ghi `lua_module_versions` theo DDL. |
| 16 | `approved_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Thời điểm `approved` của bản ghi. |

**Ràng buộc cấp bảng**

- `RB-13-01`: `FOREIGN KEY (realm_id,lua_module_id) REFERENCES lua_modules(realm_id,id)`
- `RB-13-02`: `FOREIGN KEY (realm_id,content_release_id,sandbox_policy_version,host_api_whitelist_sha256) REFERENCES content_releases(realm_id,id,lua_sandbox_policy_version,lua_host_api_whitelist_sha256)`
- `RB-13-03`: `FOREIGN KEY (realm_id,source_artifact_id,content_release_id) REFERENCES content_artifacts(realm_id,id,content_release_id)`
- `RB-13-04`: `UNIQUE (realm_id,id)`
- `RB-13-05`: `UNIQUE (realm_id,lua_module_id,content_release_id)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- Không có object bổ sung gắn trực tiếp trong DDL canonical.

### 14. `inventory_items`

character+template active; delete là tombstone, không hard-delete trực tiếp.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-14-01`, `RB-14-02`, `RB-14-03` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-14-01`, `RB-14-02`, `RB-14-03` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-14-01` | Mã `character` trong `inventory_items`; chỉ là FK khi DDL khai báo. |
| 4 | `template_id` | `integer` | Không khai báo trong SQL | Có | `NOT NULL CHECK (template_id>0)`; Không có `DEFAULT` | Mã `template` trong `inventory_items`; chỉ là FK khi DDL khai báo. |
| 5 | `content_release_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-14-02` | Release content pin cho bản ghi. |
| 6 | `container` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (container IN ('bag','equipment','bank','mail','escrow'))`; Không có `DEFAULT` | Giá trị `container` của bản ghi `inventory_items` theo DDL. |
| 7 | `slot` | `integer` | Không khai báo trong SQL | Có | `NOT NULL CHECK (slot>=0 AND (container <> 'bag' OR slot BETWEEN 0 AND 59))`; Không có `DEFAULT` | Giá trị `slot` của bản ghi `inventory_items` theo DDL. |
| 8 | `quantity` | `integer` | Không khai báo trong SQL | Có | `NOT NULL CHECK (quantity>0)`; Không có `DEFAULT` | Giá trị `quantity` của bản ghi `inventory_items` theo DDL. |
| 9 | `durability` | `integer` | Không khai báo trong SQL | Không | `CHECK (durability>=0)`; Không có `DEFAULT` | Giá trị `durability` của bản ghi `inventory_items` theo DDL. |
| 10 | `attributes` | `jsonb` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT '{}' CHECK (jsonb_typeof(attributes)='object')` | Giá trị `attributes` của bản ghi `inventory_items` theo DDL. |
| 11 | `bound` | `boolean` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT false` | Giá trị `bound` của bản ghi `inventory_items` theo DDL. |
| 12 | `deleted_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm soft-delete bản ghi. |
| 13 | `version` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (version>0)` | Phiên bản optimistic concurrency. |

**Ràng buộc cấp bảng**

- `RB-14-01`: `FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id)`
- `RB-14-02`: `FOREIGN KEY (realm_id,content_release_id) REFERENCES content_releases(realm_id,id)`
- `RB-14-03`: `UNIQUE (realm_id,id)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE UNIQUE INDEX uq_inventory_slot ON inventory_items(realm_id,character_id,container,slot) WHERE deleted_at IS NULL`

### 15. `wallets`

owner lookup; balance cache chỉ đổi cùng ledger transaction.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-15-01`, `RB-15-02` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL REFERENCES realms(id)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-15-01`, `RB-15-02`, `RB-15-03` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `owner_type` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (owner_type IN ('character','guild','system'))`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-15-03` | Giá trị `owner_type` của bản ghi `wallets` theo DDL. |
| 4 | `owner_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-15-03` | Mã `owner` trong `wallets`; chỉ là FK khi DDL khai báo. |
| 5 | `currency_code` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (currency_code ~ '^[A-Z][A-Z0-9_]{1,15}$')`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-15-02`, `RB-15-03` | Giá trị `currency_code` của bản ghi `wallets` theo DDL. |
| 6 | `balance` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0` | Giá trị `balance` của bản ghi `wallets` theo DDL. |
| 7 | `version` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (version>0)` | Phiên bản optimistic concurrency. |

**Ràng buộc cấp bảng**

- `RB-15-01`: `UNIQUE (realm_id,id)`
- `RB-15-02`: `UNIQUE (realm_id,id,currency_code)`
- `RB-15-03`: `UNIQUE (realm_id,owner_type,owner_id,currency_code)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- Không có object bổ sung gắn trực tiếp trong DDL canonical.

### 16. `economy_transactions`

Payload bất biến từ lúc tạo; `posted` chỉ sau ledger cân bằng; reversal là transaction đối ứng.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-16-01`, `RB-16-02`, `RB-16-03` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL REFERENCES realms(id)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-16-01`, `RB-16-02`, `RB-16-03`, `RB-16-04` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `operation` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-16-04` | Giá trị `operation` của bản ghi `economy_transactions` theo DDL. |
| 4 | `idempotency_key` | `text` | 16..128 ký tự theo CHECK | Có | `NOT NULL CHECK (length(idempotency_key) BETWEEN 16 AND 128)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-16-04` | Giá trị `idempotency_key` của bản ghi `economy_transactions` theo DDL. |
| 5 | `actor_character_id` | `uuid` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-16-01` | Mã `actor_character` trong `economy_transactions`; chỉ là FK khi DDL khai báo. |
| 6 | `status` | `text` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 'pending' CHECK (status IN ('pending','posted','reversed','failed'))`; Ràng buộc cấp bảng: `RB-16-05` | Trạng thái theo miền CHECK của bảng. |
| 7 | `reversal_of_id` | `uuid` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-16-02` | Mã `reversal_of` trong `economy_transactions`; chỉ là FK khi DDL khai báo. |
| 8 | `metadata` | `jsonb` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT '{}'` | Giá trị `metadata` của bản ghi `economy_transactions` theo DDL. |
| 9 | `created_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm tạo bản ghi. |
| 10 | `posted_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-16-05` | Thời điểm `posted` của bản ghi. |
| 11 | `reversed_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-16-05` | Thời điểm `reversed` của bản ghi. |

**Ràng buộc cấp bảng**

- `RB-16-01`: `FOREIGN KEY (realm_id,actor_character_id) REFERENCES characters(realm_id,id)`
- `RB-16-02`: `FOREIGN KEY (realm_id,reversal_of_id) REFERENCES economy_transactions(realm_id,id)`
- `RB-16-03`: `UNIQUE (realm_id,id)`
- `RB-16-04`: `UNIQUE (realm_id,operation,idempotency_key)`
- `RB-16-05`: `CHECK ( (status IN ('pending','failed') AND posted_at IS NULL AND reversed_at IS NULL) OR (status='posted' AND posted_at IS NOT NULL AND reversed_at IS NULL) OR (status='reversed' AND posted_at IS NOT NULL AND reversed_at IS NOT NULL) )`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE UNIQUE INDEX uq_economy_reversal ON economy_transactions(realm_id,reversal_of_id) WHERE reversal_of_id IS NOT NULL`
- `CREATE TRIGGER ck_economy_transaction_guard BEFORE INSERT OR UPDATE OR DELETE ON economy_transactions FOR EACH ROW EXECUTE FUNCTION guard_economy_transaction()`
- `CREATE CONSTRAINT TRIGGER ck_economy_balanced AFTER INSERT OR UPDATE OF status ON economy_transactions DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION assert_balanced_economy()`

### 17. `economy_entries`

wallet history theo descending time; append-only.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-17-01`, `RB-17-02`, `RB-17-03` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-17-01`, `RB-17-02`, `RB-17-03`, `RB-17-04` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `transaction_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-17-01`, `RB-17-04` | Mã `transaction` trong `economy_entries`; chỉ là FK khi DDL khai báo. |
| 4 | `wallet_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-17-02` | Mã `wallet` trong `economy_entries`; chỉ là FK khi DDL khai báo. |
| 5 | `currency_code` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-17-02` | Giá trị `currency_code` của bản ghi `economy_entries` theo DDL. |
| 6 | `delta` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL CHECK (delta<>0)`; Không có `DEFAULT` | Giá trị `delta` của bản ghi `economy_entries` theo DDL. |
| 7 | `balance_after` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Giá trị `balance_after` của bản ghi `economy_entries` theo DDL. |
| 8 | `entry_index` | `smallint` | Không khai báo trong SQL | Có | `NOT NULL CHECK (entry_index>=0)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-17-04` | Giá trị `entry_index` của bản ghi `economy_entries` theo DDL. |
| 9 | `created_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm tạo bản ghi. |

**Ràng buộc cấp bảng**

- `RB-17-01`: `FOREIGN KEY (realm_id,transaction_id) REFERENCES economy_transactions(realm_id,id)`
- `RB-17-02`: `FOREIGN KEY (realm_id,wallet_id,currency_code) REFERENCES wallets(realm_id,id,currency_code)`
- `RB-17-03`: `UNIQUE (realm_id,id)`
- `RB-17-04`: `UNIQUE (realm_id,transaction_id,entry_index)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE TRIGGER ck_economy_entry_guard BEFORE INSERT OR UPDATE OR DELETE ON economy_entries FOR EACH ROW EXECUTE FUNCTION guard_economy_entry()`
- `CREATE INDEX ix_ledger_wallet ON economy_entries(realm_id,wallet_id,created_at DESC,id)`

### 18. `runtime_checkpoints`

current partial UQ; history descending tick; verify hash trước hydrate.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-18-01`, `RB-18-02` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-18-01`, `RB-18-02`, `RB-18-03` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-18-01`, `RB-18-03` | Mã `character` trong `runtime_checkpoints`; chỉ là FK khi DDL khai báo. |
| 4 | `session_epoch` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL CHECK (session_epoch>0)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-18-03` | Giá trị `session_epoch` của bản ghi `runtime_checkpoints` theo DDL. |
| 5 | `server_tick` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL CHECK (server_tick>=0)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-18-03` | Giá trị `server_tick` của bản ghi `runtime_checkpoints` theo DDL. |
| 6 | `last_client_seq` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL CHECK (last_client_seq>=0)`; Không có `DEFAULT` | Giá trị `last_client_seq` của bản ghi `runtime_checkpoints` theo DDL. |
| 7 | `schema_version` | `integer` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1` | Giá trị `schema_version` của bản ghi `runtime_checkpoints` theo DDL. |
| 8 | `state_blob` | `bytea` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Giá trị `state_blob` của bản ghi `runtime_checkpoints` theo DDL. |
| 9 | `state_sha256` | `bytea` | 32 byte theo CHECK | Có | `NOT NULL CHECK (octet_length(state_sha256)=32)`; Không có `DEFAULT` | Giá trị kiểm chứng `state_sha256` theo DDL. |
| 10 | `created_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm tạo bản ghi. |
| 11 | `superseded_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm `superseded` của bản ghi. |

**Ràng buộc cấp bảng**

- `RB-18-01`: `FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id)`
- `RB-18-02`: `UNIQUE (realm_id,id)`
- `RB-18-03`: `UNIQUE (realm_id,character_id,session_epoch,server_tick)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE UNIQUE INDEX uq_current_checkpoint ON runtime_checkpoints(realm_id,character_id) WHERE superseded_at IS NULL`

### 19. `idempotency_keys`

expiry cleanup; response không chứa secret/token login.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL REFERENCES realms(id)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-19-01` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `actor_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-19-01` | Mã `actor` trong `idempotency_keys`; chỉ là FK khi DDL khai báo. |
| 4 | `operation` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-19-01` | Giá trị `operation` của bản ghi `idempotency_keys` theo DDL. |
| 5 | `idempotency_key` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-19-01` | Giá trị `idempotency_key` của bản ghi `idempotency_keys` theo DDL. |
| 6 | `request_hash` | `bytea` | 32 byte theo CHECK | Có | `NOT NULL CHECK (octet_length(request_hash)=32)`; Không có `DEFAULT` | Giá trị kiểm chứng `request_hash` theo DDL. |
| 7 | `state` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (state IN ('in_progress','completed','failed'))`; Không có `DEFAULT` | Trạng thái theo miền CHECK của bảng. |
| 8 | `response_status` | `integer` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Giá trị `response_status` của bản ghi `idempotency_keys` theo DDL. |
| 9 | `response_body` | `bytea` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Giá trị `response_body` của bản ghi `idempotency_keys` theo DDL. |
| 10 | `created_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm tạo bản ghi. |
| 11 | `completed_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm `completed` của bản ghi. |
| 12 | `expires_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Thời điểm `expires` của bản ghi. |

**Ràng buộc cấp bảng**

- `RB-19-01`: `UNIQUE (realm_id,actor_id,operation,idempotency_key)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE INDEX ix_idempotency_expiry ON idempotency_keys(expires_at)`

### 20. `outbox_events`

pending partial index; append in same aggregate transaction.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL REFERENCES realms(id)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-20-01` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `aggregate_type` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-20-01` | Giá trị `aggregate_type` của bản ghi `outbox_events` theo DDL. |
| 4 | `aggregate_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-20-01` | Mã `aggregate` trong `outbox_events`; chỉ là FK khi DDL khai báo. |
| 5 | `aggregate_version` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL CHECK (aggregate_version>0)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-20-01` | Giá trị `aggregate_version` của bản ghi `outbox_events` theo DDL. |
| 6 | `event_type` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-20-01` | Giá trị `event_type` của bản ghi `outbox_events` theo DDL. |
| 7 | `schema_version` | `integer` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1` | Giá trị `schema_version` của bản ghi `outbox_events` theo DDL. |
| 8 | `payload` | `jsonb` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Giá trị `payload` của bản ghi `outbox_events` theo DDL. |
| 9 | `occurred_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm `occurred` của bản ghi. |
| 10 | `available_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm `available` của bản ghi. |
| 11 | `attempts` | `integer` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0` | Giá trị `attempts` của bản ghi `outbox_events` theo DDL. |
| 12 | `published_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm `published` của bản ghi. |
| 13 | `last_error` | `text` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Giá trị `last_error` của bản ghi `outbox_events` theo DDL. |

**Ràng buộc cấp bảng**

- `RB-20-01`: `UNIQUE (realm_id,aggregate_type,aggregate_id,aggregate_version,event_type)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE INDEX ix_outbox_pending ON outbox_events(available_at,occurred_at) WHERE published_at IS NULL`

### 21. `audit_events`

target/actor timelines; không lưu password/token/payload PII thô.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL REFERENCES realms(id)`; Không có `DEFAULT` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `actor_type` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Giá trị `actor_type` của bản ghi `audit_events` theo DDL. |
| 4 | `actor_id` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Mã `actor` trong `audit_events`; chỉ là FK khi DDL khai báo. |
| 5 | `action` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Giá trị `action` của bản ghi `audit_events` theo DDL. |
| 6 | `target_type` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Giá trị `target_type` của bản ghi `audit_events` theo DDL. |
| 7 | `target_id` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Mã `target` trong `audit_events`; chỉ là FK khi DDL khai báo. |
| 8 | `request_id` | `uuid` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Mã `request` trong `audit_events`; chỉ là FK khi DDL khai báo. |
| 9 | `trace_id` | `text` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Mã `trace` trong `audit_events`; chỉ là FK khi DDL khai báo. |
| 10 | `before_hash` | `bytea` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Giá trị kiểm chứng `before_hash` theo DDL. |
| 11 | `after_hash` | `bytea` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Giá trị kiểm chứng `after_hash` theo DDL. |
| 12 | `metadata` | `jsonb` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT '{}'` | Giá trị `metadata` của bản ghi `audit_events` theo DDL. |
| 13 | `occurred_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm `occurred` của bản ghi. |

**Ràng buộc cấp bảng**

- Không có ràng buộc cấp bảng trong `CREATE TABLE`; xem constraint inline.

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE INDEX ix_audit_target ON audit_events(realm_id,target_type,target_id,occurred_at DESC)`

### 22. `password_reset_tokens`

live token theo account/expiry; raw OTP/token không lưu.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-22-01`, `RB-22-02` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-22-01`, `RB-22-02`, `RB-22-03` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `account_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-22-01` | Mã `account` trong `password_reset_tokens`; chỉ là FK khi DDL khai báo. |
| 4 | `token_hash` | `bytea` | 32 byte theo CHECK | Có | `NOT NULL CHECK (octet_length(token_hash)=32)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-22-03` | Hash token; không lưu token thô. |
| 5 | `requested_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()`; Ràng buộc cấp bảng: `RB-22-04` | Thời điểm `requested` của bản ghi. |
| 6 | `expires_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-22-04` | Thời điểm `expires` của bản ghi. |
| 7 | `consumed_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm `consumed` của bản ghi. |
| 8 | `requester_ip_hash` | `bytea` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Giá trị kiểm chứng `requester_ip_hash` theo DDL. |

**Ràng buộc cấp bảng**

- `RB-22-01`: `FOREIGN KEY (realm_id,account_id) REFERENCES accounts(realm_id,id) ON DELETE CASCADE`
- `RB-22-02`: `UNIQUE (realm_id,id)`
- `RB-22-03`: `UNIQUE (realm_id,token_hash)`
- `RB-22-04`: `CHECK (expires_at>requested_at)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE INDEX ix_password_reset_live ON password_reset_tokens(realm_id,account_id,expires_at) WHERE consumed_at IS NULL`

### 23. `world_channels`

admission theo realm/map/status/population.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-23-01` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL REFERENCES realms(id)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-23-01`, `RB-23-02` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `map_id` | `integer` | Không khai báo trong SQL | Có | `NOT NULL CHECK (map_id>=0)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-23-02` | Mã `map` trong `world_channels`; chỉ là FK khi DDL khai báo. |
| 4 | `channel_no` | `integer` | Không khai báo trong SQL | Có | `NOT NULL CHECK (channel_no>0)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-23-02` | Giá trị `channel_no` của bản ghi `world_channels` theo DDL. |
| 5 | `status` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (status IN ('open','draining','closed'))`; Không có `DEFAULT` | Trạng thái theo miền CHECK của bảng. |
| 6 | `capacity` | `integer` | Không khai báo trong SQL | Có | `NOT NULL CHECK (capacity>0)`; Không có `DEFAULT` | Giá trị `capacity` của bản ghi `world_channels` theo DDL. |
| 7 | `population` | `integer` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0 CHECK (population>=0 AND population<=capacity)` | Giá trị `population` của bản ghi `world_channels` theo DDL. |
| 8 | `endpoint_key` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Giá trị `endpoint_key` của bản ghi `world_channels` theo DDL. |
| 9 | `version` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (version>0)` | Phiên bản optimistic concurrency. |

**Ràng buộc cấp bảng**

- `RB-23-01`: `UNIQUE (realm_id,id)`
- `RB-23-02`: `UNIQUE (realm_id,map_id,channel_no)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE INDEX ix_channels_admission ON world_channels(realm_id,map_id,status,population)`

### 24. `character_transfers`

resume/cutover theo token hash, giữ party affinity.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-24-01`, `RB-24-02`, `RB-24-03`, `RB-24-04` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-24-01`, `RB-24-02`, `RB-24-03`, `RB-24-04`, `RB-24-05` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-24-01` | Mã `character` trong `character_transfers`; chỉ là FK khi DDL khai báo. |
| 4 | `source_channel_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-24-02`, `RB-24-06` | Mã `source_channel` trong `character_transfers`; chỉ là FK khi DDL khai báo. |
| 5 | `destination_channel_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-24-03`, `RB-24-06` | Mã `destination_channel` trong `character_transfers`; chỉ là FK khi DDL khai báo. |
| 6 | `party_id` | `uuid` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Mã `party` trong `character_transfers`; chỉ là FK khi DDL khai báo. |
| 7 | `state` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (state IN ('prepared','committed','failed','expired'))`; Không có `DEFAULT` | Trạng thái theo miền CHECK của bảng. |
| 8 | `prepare_tick` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL CHECK (prepare_tick>=0)`; Không có `DEFAULT` | Giá trị `prepare_tick` của bản ghi `character_transfers` theo DDL. |
| 9 | `commit_tick` | `bigint` | Không khai báo trong SQL | Không | `CHECK (commit_tick>=prepare_tick)`; Không có `DEFAULT` | Giá trị `commit_tick` của bản ghi `character_transfers` theo DDL. |
| 10 | `transfer_token_hash` | `bytea` | 32 byte theo CHECK | Có | `NOT NULL CHECK (octet_length(transfer_token_hash)=32)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-24-05` | Giá trị kiểm chứng `transfer_token_hash` theo DDL. |
| 11 | `expires_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Thời điểm `expires` của bản ghi. |
| 12 | `created_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm tạo bản ghi. |
| 13 | `completed_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm `completed` của bản ghi. |

**Ràng buộc cấp bảng**

- `RB-24-01`: `FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id)`
- `RB-24-02`: `FOREIGN KEY (realm_id,source_channel_id) REFERENCES world_channels(realm_id,id)`
- `RB-24-03`: `FOREIGN KEY (realm_id,destination_channel_id) REFERENCES world_channels(realm_id,id)`
- `RB-24-04`: `UNIQUE (realm_id,id)`
- `RB-24-05`: `UNIQUE (realm_id,transfer_token_hash)`
- `RB-24-06`: `CHECK (source_channel_id<>destination_channel_id)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE UNIQUE INDEX uq_character_transfer_active ON character_transfers(realm_id,character_id) WHERE state='prepared'`
- `ALTER TABLE character_transfers ADD CONSTRAINT fk_transfer_party FOREIGN KEY (realm_id,party_id) REFERENCES parties(realm_id,id)`

### 25. `character_quests`

active quest theo character/state.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-25-01`, `RB-25-02`, `RB-25-03` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-25-01`, `RB-25-02`, `RB-25-03`, `RB-25-04` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-25-01`, `RB-25-04` | Mã `character` trong `character_quests`; chỉ là FK khi DDL khai báo. |
| 4 | `quest_id` | `integer` | Không khai báo trong SQL | Có | `NOT NULL CHECK (quest_id>0)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-25-04` | Mã `quest` trong `character_quests`; chỉ là FK khi DDL khai báo. |
| 5 | `content_release_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-25-02` | Release content pin cho bản ghi. |
| 6 | `state` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (state IN ('accepted','active','completable','completed','failed','abandoned'))`; Không có `DEFAULT` | Trạng thái theo miền CHECK của bảng. |
| 7 | `accepted_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm `accepted` của bản ghi. |
| 8 | `completed_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm `completed` của bản ghi. |
| 9 | `revision` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (revision>0)` | Revision của state/offer. |

**Ràng buộc cấp bảng**

- `RB-25-01`: `FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id)`
- `RB-25-02`: `FOREIGN KEY (realm_id,content_release_id) REFERENCES content_releases(realm_id,id)`
- `RB-25-03`: `UNIQUE (realm_id,id)`
- `RB-25-04`: `UNIQUE (realm_id,character_id,quest_id)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE INDEX ix_character_quests_state ON character_quests(realm_id,character_id,state)`

### 26. `quest_objectives`

delta objective chính xác.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-26-01`, `RB-26-02` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-26-01`, `RB-26-02`, `RB-26-03` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `character_quest_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-26-01`, `RB-26-03` | Mã `character_quest` trong `quest_objectives`; chỉ là FK khi DDL khai báo. |
| 4 | `objective_key` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-26-03` | Giá trị `objective_key` của bản ghi `quest_objectives` theo DDL. |
| 5 | `current_value` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0 CHECK (current_value>=0)`; Ràng buộc cấp bảng: `RB-26-04` | Giá trị `current_value` của bản ghi `quest_objectives` theo DDL. |
| 6 | `target_value` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL CHECK (target_value>0)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-26-04` | Giá trị `target_value` của bản ghi `quest_objectives` theo DDL. |
| 7 | `updated_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm cập nhật bản ghi. |

**Ràng buộc cấp bảng**

- `RB-26-01`: `FOREIGN KEY (realm_id,character_quest_id) REFERENCES character_quests(realm_id,id) ON DELETE CASCADE`
- `RB-26-02`: `UNIQUE (realm_id,id)`
- `RB-26-03`: `UNIQUE (realm_id,character_quest_id,objective_key)`
- `RB-26-04`: `CHECK (current_value<=target_value)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- Không có object bổ sung gắn trực tiếp trong DDL canonical.

### 27. `reward_grants`

exactly-once cho quest/event/PvP/boss/rebirth.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-27-01`, `RB-27-02`, `RB-27-03` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-27-01`, `RB-27-02`, `RB-27-03`, `RB-27-04` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-27-01`, `RB-27-04` | Mã `character` trong `reward_grants`; chỉ là FK khi DDL khai báo. |
| 4 | `source_type` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (source_type IN ('quest','event','pvp','boss','rebirth','admin'))`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-27-04` | Giá trị `source_type` của bản ghi `reward_grants` theo DDL. |
| 5 | `source_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-27-04` | Mã `source` trong `reward_grants`; chỉ là FK khi DDL khai báo. |
| 6 | `reward_key` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-27-04` | Giá trị `reward_key` của bản ghi `reward_grants` theo DDL. |
| 7 | `economy_transaction_id` | `uuid` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-27-02` | Mã `economy_transaction` trong `reward_grants`; chỉ là FK khi DDL khai báo. |
| 8 | `status` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (status IN ('pending','granted','reversed','failed'))`; Không có `DEFAULT` | Trạng thái theo miền CHECK của bảng. |
| 9 | `granted_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm `granted` của bản ghi. |
| 10 | `created_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm tạo bản ghi. |

**Ràng buộc cấp bảng**

- `RB-27-01`: `FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id)`
- `RB-27-02`: `FOREIGN KEY (realm_id,economy_transaction_id) REFERENCES economy_transactions(realm_id,id)`
- `RB-27-03`: `UNIQUE (realm_id,id)`
- `RB-27-04`: `UNIQUE (realm_id,source_type,source_id,character_id,reward_key)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- Không có object bổ sung gắn trực tiếp trong DDL canonical.

### 28. `parties`

root roster và transfer affinity.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-28-01`, `RB-28-02` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL REFERENCES realms(id)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-28-01`, `RB-28-02` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `captain_character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-28-01` | Mã `captain_character` trong `parties`; chỉ là FK khi DDL khai báo. |
| 4 | `loot_policy` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (loot_policy IN ('owner','round_robin','random','free_for_all'))`; Không có `DEFAULT` | Giá trị `loot_policy` của bản ghi `parties` theo DDL. |
| 5 | `state` | `text` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 'active' CHECK (state IN ('active','disbanded'))` | Trạng thái theo miền CHECK của bảng. |
| 6 | `created_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm tạo bản ghi. |
| 7 | `disbanded_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm `disbanded` của bản ghi. |
| 8 | `version` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (version>0)` | Phiên bản optimistic concurrency. |

**Ràng buộc cấp bảng**

- `RB-28-01`: `FOREIGN KEY (realm_id,captain_character_id) REFERENCES characters(realm_id,id)`
- `RB-28-02`: `UNIQUE (realm_id,id)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- Không có object bổ sung gắn trực tiếp trong DDL canonical.

### 29. `party_members`

một party active/character.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-29-01`, `RB-29-02`, `RB-29-03` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-29-01`, `RB-29-02`, `RB-29-03` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `party_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-29-01` | Mã `party` trong `party_members`; chỉ là FK khi DDL khai báo. |
| 4 | `character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-29-02` | Mã `character` trong `party_members`; chỉ là FK khi DDL khai báo. |
| 5 | `role` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (role IN ('captain','member'))`; Không có `DEFAULT` | Giá trị `role` của bản ghi `party_members` theo DDL. |
| 6 | `joined_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm `joined` của bản ghi. |
| 7 | `left_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm `left` của bản ghi. |

**Ràng buộc cấp bảng**

- `RB-29-01`: `FOREIGN KEY (realm_id,party_id) REFERENCES parties(realm_id,id)`
- `RB-29-02`: `FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id)`
- `RB-29-03`: `UNIQUE (realm_id,id)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE UNIQUE INDEX uq_party_active_character ON party_members(realm_id,character_id) WHERE left_at IS NULL`
- `CREATE UNIQUE INDEX uq_party_active_captain ON party_members(realm_id,party_id) WHERE left_at IS NULL AND role='captain'`

### 30. `party_invites`

retry/expiry idempotent.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-30-01`, `RB-30-02`, `RB-30-03`, `RB-30-04` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-30-01`, `RB-30-02`, `RB-30-03`, `RB-30-04` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `party_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-30-01` | Mã `party` trong `party_invites`; chỉ là FK khi DDL khai báo. |
| 4 | `inviter_character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-30-02`, `RB-30-05` | Mã `inviter_character` trong `party_invites`; chỉ là FK khi DDL khai báo. |
| 5 | `invitee_character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-30-03`, `RB-30-05` | Mã `invitee_character` trong `party_invites`; chỉ là FK khi DDL khai báo. |
| 6 | `state` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (state IN ('pending','accepted','declined','expired','revoked'))`; Không có `DEFAULT` | Trạng thái theo miền CHECK của bảng. |
| 7 | `created_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()`; Ràng buộc cấp bảng: `RB-30-06` | Thời điểm tạo bản ghi. |
| 8 | `expires_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-30-06` | Thời điểm `expires` của bản ghi. |
| 9 | `responded_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm `responded` của bản ghi. |

**Ràng buộc cấp bảng**

- `RB-30-01`: `FOREIGN KEY (realm_id,party_id) REFERENCES parties(realm_id,id)`
- `RB-30-02`: `FOREIGN KEY (realm_id,inviter_character_id) REFERENCES characters(realm_id,id)`
- `RB-30-03`: `FOREIGN KEY (realm_id,invitee_character_id) REFERENCES characters(realm_id,id)`
- `RB-30-04`: `UNIQUE (realm_id,id)`
- `RB-30-05`: `CHECK (inviter_character_id<>invitee_character_id)`
- `RB-30-06`: `CHECK (expires_at>created_at)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE UNIQUE INDEX uq_party_invite_pending ON party_invites(realm_id,party_id,invitee_character_id) WHERE state='pending'`

### 31. `friendships`

query index cho cả hai đầu.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-31-01`, `RB-31-02`, `RB-31-03` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-31-01`, `RB-31-02`, `RB-31-03`, `RB-31-04` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `character_low_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-31-01`, `RB-31-04`, `RB-31-05` | Mã `character_low` trong `friendships`; chỉ là FK khi DDL khai báo. |
| 4 | `character_high_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-31-02`, `RB-31-04`, `RB-31-05` | Mã `character_high` trong `friendships`; chỉ là FK khi DDL khai báo. |
| 5 | `state` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (state IN ('pending_low_high','pending_high_low','accepted','blocked_low_high','blocked_high_low','removed'))`; Không có `DEFAULT` | Trạng thái theo miền CHECK của bảng. |
| 6 | `requested_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm `requested` của bản ghi. |
| 7 | `updated_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm cập nhật bản ghi. |

**Ràng buộc cấp bảng**

- `RB-31-01`: `FOREIGN KEY (realm_id,character_low_id) REFERENCES characters(realm_id,id)`
- `RB-31-02`: `FOREIGN KEY (realm_id,character_high_id) REFERENCES characters(realm_id,id)`
- `RB-31-03`: `UNIQUE (realm_id,id)`
- `RB-31-04`: `UNIQUE (realm_id,character_low_id,character_high_id)`
- `RB-31-05`: `CHECK (character_low_id<character_high_id)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE INDEX ix_friend_high ON friendships(realm_id,character_high_id,state)`

### 32. `chat_messages`

channel timeline; body không ở log.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-32-01`, `RB-32-02`, `RB-32-03` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-32-01`, `RB-32-02`, `RB-32-03` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `sender_character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-32-01` | Mã `sender_character` trong `chat_messages`; chỉ là FK khi DDL khai báo. |
| 4 | `channel_type` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (channel_type IN ('world','map','party','guild','whisper','system'))`; Không có `DEFAULT` | Giá trị `channel_type` của bản ghi `chat_messages` theo DDL. |
| 5 | `channel_ref_id` | `uuid` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Mã `channel_ref` trong `chat_messages`; chỉ là FK khi DDL khai báo. |
| 6 | `recipient_character_id` | `uuid` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-32-02` | Mã `recipient_character` trong `chat_messages`; chỉ là FK khi DDL khai báo. |
| 7 | `body_ciphertext` | `bytea` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Nội dung chat ở dạng ciphertext. |
| 8 | `body_hash` | `bytea` | 32 byte theo CHECK | Có | `NOT NULL CHECK (octet_length(body_hash)=32)`; Không có `DEFAULT` | Giá trị kiểm chứng `body_hash` theo DDL. |
| 9 | `sent_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm `sent` của bản ghi. |
| 10 | `moderation_state` | `text` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 'visible' CHECK (moderation_state IN ('visible','hidden','deleted'))` | Giá trị `moderation_state` của bản ghi `chat_messages` theo DDL. |

**Ràng buộc cấp bảng**

- `RB-32-01`: `FOREIGN KEY (realm_id,sender_character_id) REFERENCES characters(realm_id,id)`
- `RB-32-02`: `FOREIGN KEY (realm_id,recipient_character_id) REFERENCES characters(realm_id,id)`
- `RB-32-03`: `UNIQUE (realm_id,id)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE INDEX ix_chat_channel_time ON chat_messages(realm_id,channel_type,channel_ref_id,sent_at DESC)`

### 33. `chat_reports`

moderation queue theo state/time.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-33-01`, `RB-33-02`, `RB-33-03` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-33-01`, `RB-33-02`, `RB-33-03`, `RB-33-04` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `message_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-33-01`, `RB-33-04` | Mã `message` trong `chat_reports`; chỉ là FK khi DDL khai báo. |
| 4 | `reporter_character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-33-02`, `RB-33-04` | Mã `reporter_character` trong `chat_reports`; chỉ là FK khi DDL khai báo. |
| 5 | `reason_code` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Giá trị `reason_code` của bản ghi `chat_reports` theo DDL. |
| 6 | `note_ciphertext` | `bytea` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Giá trị `note` ở dạng ciphertext. |
| 7 | `status` | `text` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 'open' CHECK (status IN ('open','reviewing','resolved','dismissed'))` | Trạng thái theo miền CHECK của bảng. |
| 8 | `created_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm tạo bản ghi. |
| 9 | `resolved_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm `resolved` của bản ghi. |

**Ràng buộc cấp bảng**

- `RB-33-01`: `FOREIGN KEY (realm_id,message_id) REFERENCES chat_messages(realm_id,id)`
- `RB-33-02`: `FOREIGN KEY (realm_id,reporter_character_id) REFERENCES characters(realm_id,id)`
- `RB-33-03`: `UNIQUE (realm_id,id)`
- `RB-33-04`: `UNIQUE (realm_id,message_id,reporter_character_id)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE INDEX ix_chat_reports_queue ON chat_reports(realm_id,status,created_at)`

### 34. `trades`

participant/state indexes; ACK sau commit.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-34-01`, `RB-34-02`, `RB-34-03`, `RB-34-04` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-34-01`, `RB-34-02`, `RB-34-03`, `RB-34-04` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `initiator_character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-34-01`, `RB-34-05` | Mã `initiator_character` trong `trades`; chỉ là FK khi DDL khai báo. |
| 4 | `counterparty_character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-34-02`, `RB-34-05` | Mã `counterparty_character` trong `trades`; chỉ là FK khi DDL khai báo. |
| 5 | `state` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (state IN ('open','locked','committed','cancelled','expired','failed'))`; Không có `DEFAULT` | Trạng thái theo miền CHECK của bảng. |
| 6 | `revision` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (revision>0)` | Revision của state/offer. |
| 7 | `initiator_confirmed_revision` | `bigint` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Giá trị `initiator_confirmed_revision` của bản ghi `trades` theo DDL. |
| 8 | `counterparty_confirmed_revision` | `bigint` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Giá trị `counterparty_confirmed_revision` của bản ghi `trades` theo DDL. |
| 9 | `economy_transaction_id` | `uuid` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-34-03` | Mã `economy_transaction` trong `trades`; chỉ là FK khi DDL khai báo. |
| 10 | `created_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()`; Ràng buộc cấp bảng: `RB-34-06` | Thời điểm tạo bản ghi. |
| 11 | `expires_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-34-06` | Thời điểm `expires` của bản ghi. |
| 12 | `completed_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm `completed` của bản ghi. |

**Ràng buộc cấp bảng**

- `RB-34-01`: `FOREIGN KEY (realm_id,initiator_character_id) REFERENCES characters(realm_id,id)`
- `RB-34-02`: `FOREIGN KEY (realm_id,counterparty_character_id) REFERENCES characters(realm_id,id)`
- `RB-34-03`: `FOREIGN KEY (realm_id,economy_transaction_id) REFERENCES economy_transactions(realm_id,id)`
- `RB-34-04`: `UNIQUE (realm_id,id)`
- `RB-34-05`: `CHECK (initiator_character_id<>counterparty_character_id)`
- `RB-34-06`: `CHECK (expires_at>created_at)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE INDEX ix_trades_participant ON trades(realm_id,initiator_character_id,state,created_at DESC)`
- `CREATE INDEX ix_trades_counterparty ON trades(realm_id,counterparty_character_id,state,created_at DESC)`

### 35. `trade_items`

offer change invalidates confirms.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-35-01`, `RB-35-02`, `RB-35-03`, `RB-35-04` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-35-01`, `RB-35-02`, `RB-35-03`, `RB-35-04`, `RB-35-05` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `trade_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-35-01`, `RB-35-05` | Mã `trade` trong `trade_items`; chỉ là FK khi DDL khai báo. |
| 4 | `offered_by_character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-35-02` | Mã `offered_by_character` trong `trade_items`; chỉ là FK khi DDL khai báo. |
| 5 | `inventory_item_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-35-03`, `RB-35-05` | Mã `inventory_item` trong `trade_items`; chỉ là FK khi DDL khai báo. |
| 6 | `quantity` | `integer` | Không khai báo trong SQL | Có | `NOT NULL CHECK (quantity>0)`; Không có `DEFAULT` | Giá trị `quantity` của bản ghi `trade_items` theo DDL. |
| 7 | `offer_revision` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL CHECK (offer_revision>0)`; Không có `DEFAULT` | Giá trị `offer_revision` của bản ghi `trade_items` theo DDL. |

**Ràng buộc cấp bảng**

- `RB-35-01`: `FOREIGN KEY (realm_id,trade_id) REFERENCES trades(realm_id,id) ON DELETE CASCADE`
- `RB-35-02`: `FOREIGN KEY (realm_id,offered_by_character_id) REFERENCES characters(realm_id,id)`
- `RB-35-03`: `FOREIGN KEY (realm_id,inventory_item_id) REFERENCES inventory_items(realm_id,id)`
- `RB-35-04`: `UNIQUE (realm_id,id)`
- `RB-35-05`: `UNIQUE (realm_id,trade_id,inventory_item_id)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- Không có object bổ sung gắn trực tiếp trong DDL canonical.

### 36. `trade_currency_offers`

ledger post atomic.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-36-01`, `RB-36-02`, `RB-36-03` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-36-01`, `RB-36-02`, `RB-36-03`, `RB-36-04` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `trade_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-36-01`, `RB-36-04` | Mã `trade` trong `trade_currency_offers`; chỉ là FK khi DDL khai báo. |
| 4 | `offered_by_character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-36-02`, `RB-36-04` | Mã `offered_by_character` trong `trade_currency_offers`; chỉ là FK khi DDL khai báo. |
| 5 | `currency_code` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-36-04` | Giá trị `currency_code` của bản ghi `trade_currency_offers` theo DDL. |
| 6 | `amount` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL CHECK (amount>0)`; Không có `DEFAULT` | Giá trị `amount` của bản ghi `trade_currency_offers` theo DDL. |
| 7 | `offer_revision` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL CHECK (offer_revision>0)`; Không có `DEFAULT` | Giá trị `offer_revision` của bản ghi `trade_currency_offers` theo DDL. |

**Ràng buộc cấp bảng**

- `RB-36-01`: `FOREIGN KEY (realm_id,trade_id) REFERENCES trades(realm_id,id) ON DELETE CASCADE`
- `RB-36-02`: `FOREIGN KEY (realm_id,offered_by_character_id) REFERENCES characters(realm_id,id)`
- `RB-36-03`: `UNIQUE (realm_id,id)`
- `RB-36-04`: `UNIQUE (realm_id,trade_id,offered_by_character_id,currency_code)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- Không có object bổ sung gắn trực tiếp trong DDL canonical.

### 37. `stalls`

channel ownership và lifecycle.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-37-01`, `RB-37-02`, `RB-37-03` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-37-01`, `RB-37-02`, `RB-37-03` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `owner_character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-37-01` | Mã `owner_character` trong `stalls`; chỉ là FK khi DDL khai báo. |
| 4 | `channel_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-37-02` | Mã `channel` trong `stalls`; chỉ là FK khi DDL khai báo. |
| 5 | `name` | `text` | 1..40 ký tự theo CHECK | Có | `NOT NULL CHECK (length(name) BETWEEN 1 AND 40)`; Không có `DEFAULT` | Giá trị `name` của bản ghi `stalls` theo DDL. |
| 6 | `state` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (state IN ('open','closed','suspended'))`; Không có `DEFAULT` | Trạng thái theo miền CHECK của bảng. |
| 7 | `opened_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm `opened` của bản ghi. |
| 8 | `closed_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm `closed` của bản ghi. |
| 9 | `version` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (version>0)` | Phiên bản optimistic concurrency. |

**Ràng buộc cấp bảng**

- `RB-37-01`: `FOREIGN KEY (realm_id,owner_character_id) REFERENCES characters(realm_id,id)`
- `RB-37-02`: `FOREIGN KEY (realm_id,channel_id) REFERENCES world_channels(realm_id,id)`
- `RB-37-03`: `UNIQUE (realm_id,id)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE UNIQUE INDEX uq_stall_owner_open ON stalls(realm_id,owner_character_id) WHERE state='open'`

### 38. `stall_listings`

partial search state/currency/price.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-38-01`, `RB-38-02`, `RB-38-03` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-38-01`, `RB-38-02`, `RB-38-03`, `RB-38-04` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `stall_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-38-01`, `RB-38-04` | Mã `stall` trong `stall_listings`; chỉ là FK khi DDL khai báo. |
| 4 | `inventory_item_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-38-02`, `RB-38-04` | Mã `inventory_item` trong `stall_listings`; chỉ là FK khi DDL khai báo. |
| 5 | `quantity` | `integer` | Không khai báo trong SQL | Có | `NOT NULL CHECK (quantity>0)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-38-05` | Giá trị `quantity` của bản ghi `stall_listings` theo DDL. |
| 6 | `currency_code` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Giá trị `currency_code` của bản ghi `stall_listings` theo DDL. |
| 7 | `unit_price` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL CHECK (unit_price>0)`; Không có `DEFAULT` | Giá trị `unit_price` của bản ghi `stall_listings` theo DDL. |
| 8 | `remaining_quantity` | `integer` | Không khai báo trong SQL | Có | `NOT NULL CHECK (remaining_quantity>=0)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-38-05` | Giá trị `remaining_quantity` của bản ghi `stall_listings` theo DDL. |
| 9 | `state` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (state IN ('listed','sold','cancelled','expired'))`; Không có `DEFAULT` | Trạng thái theo miền CHECK của bảng. |
| 10 | `created_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm tạo bản ghi. |
| 11 | `expires_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Thời điểm `expires` của bản ghi. |
| 12 | `version` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (version>0)` | Phiên bản optimistic concurrency. |

**Ràng buộc cấp bảng**

- `RB-38-01`: `FOREIGN KEY (realm_id,stall_id) REFERENCES stalls(realm_id,id)`
- `RB-38-02`: `FOREIGN KEY (realm_id,inventory_item_id) REFERENCES inventory_items(realm_id,id)`
- `RB-38-03`: `UNIQUE (realm_id,id)`
- `RB-38-04`: `UNIQUE (realm_id,stall_id,inventory_item_id)`
- `RB-38-05`: `CHECK (remaining_quantity<=quantity)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE INDEX ix_stall_listing_search ON stall_listings(realm_id,state,currency_code,unit_price) WHERE state='listed'`

### 39. `guilds`

stable guild root.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-39-01`, `RB-39-02` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL REFERENCES realms(id)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-39-01`, `RB-39-02` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `name` | `citext` | 2..32 ký tự theo CHECK | Có | `NOT NULL CHECK (length(name) BETWEEN 2 AND 32)`; Không có `DEFAULT` | Giá trị `name` của bản ghi `guilds` theo DDL. |
| 4 | `leader_character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-39-01` | Mã `leader_character` trong `guilds`; chỉ là FK khi DDL khai báo. |
| 5 | `level` | `integer` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (level>0)` | Giá trị `level` của bản ghi `guilds` theo DDL. |
| 6 | `notice` | `text` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT ''` | Giá trị `notice` của bản ghi `guilds` theo DDL. |
| 7 | `state` | `text` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 'active' CHECK (state IN ('active','disbanded'))` | Trạng thái theo miền CHECK của bảng. |
| 8 | `created_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm tạo bản ghi. |
| 9 | `disbanded_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm `disbanded` của bản ghi. |
| 10 | `version` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (version>0)` | Phiên bản optimistic concurrency. |

**Ràng buộc cấp bảng**

- `RB-39-01`: `FOREIGN KEY (realm_id,leader_character_id) REFERENCES characters(realm_id,id)`
- `RB-39-02`: `UNIQUE (realm_id,id)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE UNIQUE INDEX uq_guild_active_name ON guilds(realm_id,name) WHERE state='active'`

### 40. `guild_members`

RBAC và membership persistent.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-40-01`, `RB-40-02`, `RB-40-03` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-40-01`, `RB-40-02`, `RB-40-03` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `guild_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-40-01` | Mã `guild` trong `guild_members`; chỉ là FK khi DDL khai báo. |
| 4 | `character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-40-02` | Mã `character` trong `guild_members`; chỉ là FK khi DDL khai báo. |
| 5 | `role` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (role IN ('leader','officer','member'))`; Không có `DEFAULT` | Giá trị `role` của bản ghi `guild_members` theo DDL. |
| 6 | `permissions` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0 CHECK (permissions>=0)` | Giá trị `permissions` của bản ghi `guild_members` theo DDL. |
| 7 | `contribution` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0 CHECK (contribution>=0)` | Giá trị `contribution` của bản ghi `guild_members` theo DDL. |
| 8 | `joined_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm `joined` của bản ghi. |
| 9 | `left_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm `left` của bản ghi. |
| 10 | `leave_cooldown_until` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Giá trị `leave_cooldown_until` của bản ghi `guild_members` theo DDL. |

**Ràng buộc cấp bảng**

- `RB-40-01`: `FOREIGN KEY (realm_id,guild_id) REFERENCES guilds(realm_id,id)`
- `RB-40-02`: `FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id)`
- `RB-40-03`: `UNIQUE (realm_id,id)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE UNIQUE INDEX uq_guild_active_character ON guild_members(realm_id,character_id) WHERE left_at IS NULL`
- `CREATE UNIQUE INDEX uq_guild_active_leader ON guild_members(realm_id,guild_id) WHERE left_at IS NULL AND role='leader'`

### 41. `character_mounts`

modifier server-side, content pin.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-41-01`, `RB-41-02`, `RB-41-03`, `RB-41-04` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-41-01`, `RB-41-02`, `RB-41-03`, `RB-41-04` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-41-01` | Mã `character` trong `character_mounts`; chỉ là FK khi DDL khai báo. |
| 4 | `mount_template_id` | `integer` | Không khai báo trong SQL | Có | `NOT NULL CHECK (mount_template_id>0)`; Không có `DEFAULT` | Mã `mount_template` trong `character_mounts`; chỉ là FK khi DDL khai báo. |
| 5 | `content_release_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-41-02` | Release content pin cho bản ghi. |
| 6 | `source_item_id` | `uuid` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-41-03` | Mã `source_item` trong `character_mounts`; chỉ là FK khi DDL khai báo. |
| 7 | `level` | `integer` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (level>0)` | Giá trị `level` của bản ghi `character_mounts` theo DDL. |
| 8 | `experience` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0 CHECK (experience>=0)` | Giá trị `experience` của bản ghi `character_mounts` theo DDL. |
| 9 | `equipped` | `boolean` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT false` | Giá trị `equipped` của bản ghi `character_mounts` theo DDL. |
| 10 | `riding` | `boolean` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT false` | Giá trị `riding` của bản ghi `character_mounts` theo DDL. |
| 11 | `created_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm tạo bản ghi. |
| 12 | `version` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (version>0)` | Phiên bản optimistic concurrency. |

**Ràng buộc cấp bảng**

- `RB-41-01`: `FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id)`
- `RB-41-02`: `FOREIGN KEY (realm_id,content_release_id) REFERENCES content_releases(realm_id,id)`
- `RB-41-03`: `FOREIGN KEY (realm_id,source_item_id) REFERENCES inventory_items(realm_id,id)`
- `RB-41-04`: `UNIQUE (realm_id,id)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE UNIQUE INDEX uq_character_equipped_mount ON character_mounts(realm_id,character_id) WHERE equipped`
- `CREATE UNIQUE INDEX uq_character_riding_mount ON character_mounts(realm_id,character_id) WHERE riding`

### 42. `character_pets`

summon/follow persistence.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-42-01`, `RB-42-02`, `RB-42-03` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-42-01`, `RB-42-02`, `RB-42-03` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-42-01` | Mã `character` trong `character_pets`; chỉ là FK khi DDL khai báo. |
| 4 | `pet_template_id` | `integer` | Không khai báo trong SQL | Có | `NOT NULL CHECK (pet_template_id>0)`; Không có `DEFAULT` | Mã `pet_template` trong `character_pets`; chỉ là FK khi DDL khai báo. |
| 5 | `content_release_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-42-02` | Release content pin cho bản ghi. |
| 6 | `name` | `text` | 1..24 ký tự theo CHECK | Có | `NOT NULL CHECK (length(name) BETWEEN 1 AND 24)`; Không có `DEFAULT` | Giá trị `name` của bản ghi `character_pets` theo DDL. |
| 7 | `level` | `integer` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (level>0)` | Giá trị `level` của bản ghi `character_pets` theo DDL. |
| 8 | `experience` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0 CHECK (experience>=0)` | Giá trị `experience` của bản ghi `character_pets` theo DDL. |
| 9 | `mode` | `text` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 'follow' CHECK (mode IN ('follow','assist','passive','stay'))` | Giá trị `mode` của bản ghi `character_pets` theo DDL. |
| 10 | `active` | `boolean` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT false` | Giá trị `active` của bản ghi `character_pets` theo DDL. |
| 11 | `state` | `jsonb` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT '{}' CHECK (jsonb_typeof(state)='object')` | Trạng thái theo miền CHECK của bảng. |
| 12 | `created_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm tạo bản ghi. |
| 13 | `version` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (version>0)` | Phiên bản optimistic concurrency. |

**Ràng buộc cấp bảng**

- `RB-42-01`: `FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id)`
- `RB-42-02`: `FOREIGN KEY (realm_id,content_release_id) REFERENCES content_releases(realm_id,id)`
- `RB-42-03`: `UNIQUE (realm_id,id)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE INDEX ix_character_pets ON character_pets(realm_id,character_id,active)`

### 43. `character_pvp_profiles`

server validates mode/cooldown.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-43-01`, `RB-43-02` | Realm sở hữu bản ghi và ranh giới RLS. |
| 2 | `character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-43-01`, `RB-43-02` | Mã `character` trong `character_pvp_profiles`; chỉ là FK khi DDL khai báo. |
| 3 | `pk_mode` | `text` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 'peace' CHECK (pk_mode IN ('peace','team','guild','all','event'))` | Giá trị `pk_mode` của bản ghi `character_pvp_profiles` theo DDL. |
| 4 | `pk_value` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0 CHECK (pk_value>=0)` | Giá trị `pk_value` của bản ghi `character_pvp_profiles` theo DDL. |
| 5 | `mode_changed_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm `mode_changed` của bản ghi. |
| 6 | `cooldown_until` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Giá trị `cooldown_until` của bản ghi `character_pvp_profiles` theo DDL. |
| 7 | `version` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (version>0)` | Phiên bản optimistic concurrency. |

**Ràng buộc cấp bảng**

- `RB-43-01`: `PRIMARY KEY (realm_id,character_id)`
- `RB-43-02`: `FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- Không có object bổ sung gắn trực tiếp trong DDL canonical.

### 44. `pvp_seasons`

versioned ladder boundary.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-44-01`, `RB-44-02` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL REFERENCES realms(id)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-44-01`, `RB-44-02`, `RB-44-03` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `season_key` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-44-03` | Giá trị `season_key` của bản ghi `pvp_seasons` theo DDL. |
| 4 | `content_release_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-44-01` | Release content pin cho bản ghi. |
| 5 | `starts_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-44-04` | Thời điểm `starts` của bản ghi. |
| 6 | `ends_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-44-04` | Thời điểm `ends` của bản ghi. |
| 7 | `state` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (state IN ('scheduled','active','finalizing','closed'))`; Không có `DEFAULT` | Trạng thái theo miền CHECK của bảng. |

**Ràng buộc cấp bảng**

- `RB-44-01`: `FOREIGN KEY (realm_id,content_release_id) REFERENCES content_releases(realm_id,id)`
- `RB-44-02`: `UNIQUE (realm_id,id)`
- `RB-44-03`: `UNIQUE (realm_id,season_key)`
- `RB-44-04`: `CHECK (ends_at>starts_at)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- Không có object bổ sung gắn trực tiếp trong DDL canonical.

### 45. `pvp_ladder_entries`

rating/rank rebuild index.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-45-01`, `RB-45-02`, `RB-45-03` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-45-01`, `RB-45-02`, `RB-45-03`, `RB-45-04` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `season_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-45-01`, `RB-45-04` | Mã `season` trong `pvp_ladder_entries`; chỉ là FK khi DDL khai báo. |
| 4 | `character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-45-02`, `RB-45-04` | Mã `character` trong `pvp_ladder_entries`; chỉ là FK khi DDL khai báo. |
| 5 | `rating` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0` | Giá trị `rating` của bản ghi `pvp_ladder_entries` theo DDL. |
| 6 | `wins` | `integer` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0 CHECK (wins>=0)` | Giá trị `wins` của bản ghi `pvp_ladder_entries` theo DDL. |
| 7 | `losses` | `integer` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0 CHECK (losses>=0)` | Giá trị `losses` của bản ghi `pvp_ladder_entries` theo DDL. |
| 8 | `rank` | `integer` | Không khai báo trong SQL | Không | `CHECK (rank>0)`; Không có `DEFAULT` | Giá trị `rank` của bản ghi `pvp_ladder_entries` theo DDL. |
| 9 | `updated_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm cập nhật bản ghi. |
| 10 | `version` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (version>0)` | Phiên bản optimistic concurrency. |

**Ràng buộc cấp bảng**

- `RB-45-01`: `FOREIGN KEY (realm_id,season_id) REFERENCES pvp_seasons(realm_id,id)`
- `RB-45-02`: `FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id)`
- `RB-45-03`: `UNIQUE (realm_id,id)`
- `RB-45-04`: `UNIQUE (realm_id,season_id,character_id)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE INDEX ix_pvp_ladder_rank ON pvp_ladder_entries(realm_id,season_id,rating DESC,character_id)`

### 46. `game_events`

schedule/state index; durable checkpoint.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-46-01`, `RB-46-02` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL REFERENCES realms(id)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-46-01`, `RB-46-02`, `RB-46-03` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `event_key` | `text` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-46-03` | Giá trị `event_key` của bản ghi `game_events` theo DDL. |
| 4 | `event_type` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (event_type IN ('pvp','boss','seasonal','world'))`; Không có `DEFAULT` | Giá trị `event_type` của bản ghi `game_events` theo DDL. |
| 5 | `content_release_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-46-01` | Release content pin cho bản ghi. |
| 6 | `starts_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-46-03`, `RB-46-04` | Thời điểm `starts` của bản ghi. |
| 7 | `ends_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-46-04` | Thời điểm `ends` của bản ghi. |
| 8 | `state` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (state IN ('scheduled','enrollment','active','settling','closed','cancelled'))`; Không có `DEFAULT` | Trạng thái theo miền CHECK của bảng. |
| 9 | `checkpoint` | `jsonb` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT '{}' CHECK (jsonb_typeof(checkpoint)='object')` | Giá trị `checkpoint` của bản ghi `game_events` theo DDL. |
| 10 | `version` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 1 CHECK (version>0)` | Phiên bản optimistic concurrency. |

**Ràng buộc cấp bảng**

- `RB-46-01`: `FOREIGN KEY (realm_id,content_release_id) REFERENCES content_releases(realm_id,id)`
- `RB-46-02`: `UNIQUE (realm_id,id)`
- `RB-46-03`: `UNIQUE (realm_id,event_key,starts_at)`
- `RB-46-04`: `CHECK (ends_at>starts_at)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE INDEX ix_game_events_schedule ON game_events(realm_id,state,starts_at)`

### 47. `event_participants`

deterministic ranking index.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-47-01`, `RB-47-02`, `RB-47-03` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-47-01`, `RB-47-02`, `RB-47-03`, `RB-47-04` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `event_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-47-01`, `RB-47-04` | Mã `event` trong `event_participants`; chỉ là FK khi DDL khai báo. |
| 4 | `character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-47-02`, `RB-47-04` | Mã `character` trong `event_participants`; chỉ là FK khi DDL khai báo. |
| 5 | `state` | `text` | Không khai báo trong SQL | Có | `NOT NULL CHECK (state IN ('enrolled','active','finished','forfeited','disqualified'))`; Không có `DEFAULT` | Trạng thái theo miền CHECK của bảng. |
| 6 | `score` | `bigint` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT 0` | Giá trị `score` của bản ghi `event_participants` theo DDL. |
| 7 | `rank` | `integer` | Không khai báo trong SQL | Không | `CHECK (rank>0)`; Không có `DEFAULT` | Giá trị `rank` của bản ghi `event_participants` theo DDL. |
| 8 | `contribution` | `jsonb` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT '{}'` | Giá trị `contribution` của bản ghi `event_participants` theo DDL. |
| 9 | `joined_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm `joined` của bản ghi. |
| 10 | `finished_at` | `timestamptz` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT` | Thời điểm `finished` của bản ghi. |

**Ràng buộc cấp bảng**

- `RB-47-01`: `FOREIGN KEY (realm_id,event_id) REFERENCES game_events(realm_id,id)`
- `RB-47-02`: `FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id)`
- `RB-47-03`: `UNIQUE (realm_id,id)`
- `RB-47-04`: `UNIQUE (realm_id,event_id,character_id)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- `CREATE INDEX ix_event_ranking ON event_participants(realm_id,event_id,score DESC,character_id)`

### 48. `character_rebirths`

append-only rebirth audit.

| **TT** | **Tên thuộc tính (Field name)** | **Kiểu dữ liệu** | **Độ rộng** | **Not NULL** | **Ràng buộc / Miền giá trị** | **Diễn giải** |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | `id` | `uuid` | Không khai báo trong SQL | Có | `PRIMARY KEY DEFAULT gen_random_uuid()`; Ràng buộc cấp bảng: `RB-48-01`, `RB-48-02`, `RB-48-03`, `RB-48-04` | Mã định danh của bản ghi. |
| 2 | `realm_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-48-01`, `RB-48-02`, `RB-48-03`, `RB-48-04`, `RB-48-05` | Realm sở hữu bản ghi và ranh giới RLS. |
| 3 | `character_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-48-01`, `RB-48-05` | Mã `character` trong `character_rebirths`; chỉ là FK khi DDL khai báo. |
| 4 | `rebirth_no` | `integer` | Không khai báo trong SQL | Có | `NOT NULL CHECK (rebirth_no>0)`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-48-05` | Giá trị `rebirth_no` của bản ghi `character_rebirths` theo DDL. |
| 5 | `content_release_id` | `uuid` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-48-02` | Release content pin cho bản ghi. |
| 6 | `previous_level` | `integer` | Không khai báo trong SQL | Có | `NOT NULL CHECK (previous_level BETWEEN 1 AND 200)`; Không có `DEFAULT` | Giá trị `previous_level` của bản ghi `character_rebirths` theo DDL. |
| 7 | `resulting_level` | `integer` | Không khai báo trong SQL | Có | `NOT NULL CHECK (resulting_level BETWEEN 1 AND 200)`; Không có `DEFAULT` | Giá trị `resulting_level` của bản ghi `character_rebirths` theo DDL. |
| 8 | `reward_grant_id` | `uuid` | Không khai báo trong SQL | Không | Không có constraint inline; Không có `DEFAULT`; Ràng buộc cấp bảng: `RB-48-03` | Mã `reward_grant` trong `character_rebirths`; chỉ là FK khi DDL khai báo. |
| 9 | `performed_at` | `timestamptz` | Không khai báo trong SQL | Có | `NOT NULL DEFAULT now()` | Thời điểm `performed` của bản ghi. |
| 10 | `state_before_hash` | `bytea` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Giá trị kiểm chứng `state_before_hash` theo DDL. |
| 11 | `state_after_hash` | `bytea` | Không khai báo trong SQL | Có | `NOT NULL`; Không có `DEFAULT` | Giá trị kiểm chứng `state_after_hash` theo DDL. |

**Ràng buộc cấp bảng**

- `RB-48-01`: `FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id)`
- `RB-48-02`: `FOREIGN KEY (realm_id,content_release_id) REFERENCES content_releases(realm_id,id)`
- `RB-48-03`: `FOREIGN KEY (realm_id,reward_grant_id) REFERENCES reward_grants(realm_id,id)`
- `RB-48-04`: `UNIQUE (realm_id,id)`
- `RB-48-05`: `UNIQUE (realm_id,character_id,rebirth_no)`

**Chỉ mục/constraint bổ sung/trigger gắn trực tiếp**

- Không có object bổ sung gắn trực tiếp trong DDL canonical.

## Invariant ứng dụng không được DDL enforce đầy đủ

- Wallet owner tồn tại đúng bảng theo `owner_type`.
- Content artifact bất biến sau khi release active.
- `economy_entries.balance_after` khớp entry trước và projection wallet.
- Gameplay state transition/ownership được kiểm trong application transaction.

Nguồn quản trị và contract test cho các invariant này là [`03-du-lieu.md`](../../03-du-lieu.md#chi-tiết-các-bảng). Không được tuyên bố PostgreSQL enforce nếu chưa cập nhật DDL canonical.
