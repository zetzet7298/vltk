-- PostgreSQL 16 normative logical schema. Application transactions set app.realm_id.
BEGIN;
CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS citext;

CREATE TABLE realms (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  code citext NOT NULL UNIQUE CHECK (code ~ '^[a-z0-9][a-z0-9-]{1,31}$'),
  name text NOT NULL CHECK (length(name) BETWEEN 1 AND 80),
  status text NOT NULL DEFAULT 'closed' CHECK (status IN ('open','maintenance','closed')),
  active_content_release_id uuid,
  created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
  version bigint NOT NULL DEFAULT 1 CHECK (version > 0)
);

CREATE TABLE accounts (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL REFERENCES realms(id),
  account_name citext NOT NULL CHECK (length(account_name) BETWEEN 3 AND 32),
  password_hash text NOT NULL, status text NOT NULL DEFAULT 'active'
    CHECK (status IN ('active','locked','banned','disabled')),
  email_ciphertext bytea, email_lookup_hmac bytea CHECK (email_lookup_hmac IS NULL OR octet_length(email_lookup_hmac)=32),
  email_verified_at timestamptz, otp_secret_ciphertext bytea,
  token_version integer NOT NULL DEFAULT 1 CHECK (token_version > 0),
  legacy_acc_name text, service_flag integer NOT NULL DEFAULT 0, ext_point bigint NOT NULL DEFAULT 0 CHECK (ext_point >= 0),
  created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
  deleted_at timestamptz, version bigint NOT NULL DEFAULT 1 CHECK (version > 0), UNIQUE (realm_id,id)
);
CREATE UNIQUE INDEX uq_accounts_active_name ON accounts(realm_id,account_name) WHERE deleted_at IS NULL;
CREATE UNIQUE INDEX uq_accounts_active_email ON accounts(realm_id,email_lookup_hmac)
WHERE deleted_at IS NULL AND email_lookup_hmac IS NOT NULL;

CREATE TABLE auth_sessions (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, account_id uuid NOT NULL,
  refresh_token_id uuid NOT NULL UNIQUE, refresh_token_hash bytea NOT NULL CHECK (octet_length(refresh_token_hash)=32),
  token_family_id uuid NOT NULL, device_id text, issued_at timestamptz NOT NULL DEFAULT now(),
  expires_at timestamptz NOT NULL, rotated_at timestamptz, revoked_at timestamptz, revoke_reason text,
  FOREIGN KEY (realm_id,account_id) REFERENCES accounts(realm_id,id) ON DELETE CASCADE,
  CHECK (expires_at > issued_at), UNIQUE (realm_id,id), UNIQUE (realm_id,id,account_id)
);
CREATE INDEX ix_auth_live ON auth_sessions(realm_id,account_id,expires_at) WHERE revoked_at IS NULL;

CREATE TABLE characters (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, account_id uuid NOT NULL,
  legacy_role_id bigint, name citext NOT NULL CHECK (length(name) BETWEEN 2 AND 24),
  gender text NOT NULL CHECK (gender IN ('male','female')),
  homeland_id integer NOT NULL CHECK (homeland_id>0),
  character_slot smallint NOT NULL CHECK (character_slot BETWEEN 1 AND 3),
  faction smallint NOT NULL DEFAULT -1 CHECK (faction BETWEEN -1 AND 9),
  series smallint NOT NULL CHECK (series BETWEEN 0 AND 4), level smallint NOT NULL DEFAULT 1 CHECK (level BETWEEN 1 AND 200),
  appearance jsonb NOT NULL DEFAULT '{}' CHECK (jsonb_typeof(appearance)='object'),
  created_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
  deleted_at timestamptz, purge_after timestamptz, version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
  FOREIGN KEY (realm_id,account_id) REFERENCES accounts(realm_id,id), UNIQUE (realm_id,id),
  UNIQUE (realm_id,id,account_id),
  UNIQUE (realm_id,legacy_role_id),
  CHECK ((deleted_at IS NULL AND purge_after IS NULL) OR purge_after = deleted_at + interval '7 days')
);
CREATE UNIQUE INDEX uq_characters_active_name ON characters(realm_id,name) WHERE deleted_at IS NULL;
CREATE UNIQUE INDEX uq_characters_active_slot ON characters(realm_id,account_id,character_slot) WHERE deleted_at IS NULL;
CREATE INDEX ix_characters_account ON characters(realm_id,account_id,created_at) WHERE deleted_at IS NULL;
CREATE INDEX ix_characters_purge ON characters(purge_after) WHERE purge_after IS NOT NULL;

CREATE TABLE character_stats (
  realm_id uuid NOT NULL, character_id uuid NOT NULL, experience bigint NOT NULL DEFAULT 0 CHECK (experience>=0),
  trans_life integer NOT NULL DEFAULT 0 CHECK (trans_life>=0), free_point integer NOT NULL DEFAULT 0 CHECK (free_point>=0),
  magic_point integer NOT NULL DEFAULT 0 CHECK (magic_point>=0), strength integer NOT NULL CHECK (strength>=0),
  dexterity integer NOT NULL CHECK (dexterity>=0), vitality integer NOT NULL CHECK (vitality>=0), spirit integer NOT NULL CHECK (spirit>=0),
  repute bigint NOT NULL DEFAULT 0 CHECK (repute>=0), current_life bigint NOT NULL DEFAULT 1 CHECK (current_life>=0),
  current_mana bigint NOT NULL DEFAULT 0 CHECK (current_mana>=0), current_stamina bigint NOT NULL DEFAULT 0 CHECK (current_stamina>=0),
  updated_at timestamptz NOT NULL DEFAULT now(), version bigint NOT NULL DEFAULT 1 CHECK (version>0),
  PRIMARY KEY (realm_id,character_id), FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id)
);
CREATE TABLE character_positions (
  realm_id uuid NOT NULL, character_id uuid NOT NULL, map_id integer NOT NULL CHECK (map_id>=0),
  pos_x integer NOT NULL, pos_y integer NOT NULL, facing_millirad integer NOT NULL DEFAULT 0 CHECK (facing_millirad BETWEEN 0 AND 6283),
  server_tick bigint NOT NULL DEFAULT 0 CHECK (server_tick>=0), updated_at timestamptz NOT NULL DEFAULT now(),
  version bigint NOT NULL DEFAULT 1 CHECK (version>0), PRIMARY KEY (realm_id,character_id),
  FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id)
);
CREATE INDEX ix_positions_map ON character_positions(realm_id,map_id);
CREATE TABLE character_skills (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, character_id uuid NOT NULL,
  skill_id integer NOT NULL CHECK (skill_id>0), level smallint NOT NULL CHECK (level>0), is_active boolean NOT NULL DEFAULT true,
  last_cast_tick bigint NOT NULL DEFAULT 0 CHECK (last_cast_tick>=0), version bigint NOT NULL DEFAULT 1 CHECK (version>0),
  FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id), UNIQUE (realm_id,id), UNIQUE (realm_id,character_id,skill_id)
);

CREATE TABLE content_releases (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL REFERENCES realms(id),
  version text NOT NULL CHECK (length(version) BETWEEN 1 AND 64),
  user_facing_locale text NOT NULL DEFAULT 'vi' CHECK (user_facing_locale='vi'),
  hot_reload_allowed boolean NOT NULL DEFAULT false CHECK (hot_reload_allowed=false),
  source_snapshot_id text NOT NULL CHECK (length(source_snapshot_id) BETWEEN 7 AND 128),
  source_root text NOT NULL CHECK (length(source_root) BETWEEN 1 AND 1024),
  source_snapshot_sha256 bytea NOT NULL CHECK (octet_length(source_snapshot_sha256)=32),
  source_vcs_revision text CHECK (source_vcs_revision IS NULL OR length(source_vcs_revision) BETWEEN 7 AND 128),
  catalog_generator_revision text NOT NULL CHECK (length(catalog_generator_revision) BETWEEN 7 AND 128),
  lua_runtime text NOT NULL DEFAULT 'Lua 5.1' CHECK (lua_runtime='Lua 5.1'),
  lua_sandbox_policy_version text NOT NULL CHECK (length(lua_sandbox_policy_version) BETWEEN 1 AND 64),
  lua_host_api_whitelist jsonb NOT NULL CHECK (jsonb_typeof(lua_host_api_whitelist)='array' AND jsonb_array_length(lua_host_api_whitelist)>0),
  lua_host_api_whitelist_sha256 bytea NOT NULL CHECK (octet_length(lua_host_api_whitelist_sha256)=32),
  manifest_sha256 bytea NOT NULL CHECK (octet_length(manifest_sha256)=32),
  content_digest_sha256 bytea NOT NULL CHECK (octet_length(content_digest_sha256)=32),
  catalog_union_size integer NOT NULL DEFAULT 242 CHECK (catalog_union_size=242),
  catalog_union_sha256 bytea NOT NULL CHECK (octet_length(catalog_union_sha256)=32),
  runtime_skill_policy_id text NOT NULL CHECK (length(runtime_skill_policy_id) BETWEEN 1 AND 128),
  runtime_skill_policy jsonb NOT NULL CHECK (jsonb_typeof(runtime_skill_policy)='object'
    AND runtime_skill_policy->>'sourceTool'='vltktool'
    AND runtime_skill_policy->>'filesystemFallbackAllowed'='false'
    AND runtime_skill_policy->>'runtimeParityClaimed'='false'),
  signature bytea NOT NULL, signing_key_id text NOT NULL,
  status text NOT NULL DEFAULT 'staged' CHECK (status IN ('staged','active','retired','rejected')),
  created_at timestamptz NOT NULL DEFAULT now(), activated_at timestamptz, retired_at timestamptz, created_by text NOT NULL,
  UNIQUE (realm_id,id), UNIQUE (realm_id,id,source_snapshot_id),
  UNIQUE (realm_id,id,lua_sandbox_policy_version,lua_host_api_whitelist_sha256),
  UNIQUE (realm_id,version), UNIQUE (realm_id,manifest_sha256)
);
CREATE UNIQUE INDEX uq_content_active ON content_releases(realm_id) WHERE status='active';
ALTER TABLE realms ADD CONSTRAINT fk_active_content
  FOREIGN KEY (id,active_content_release_id) REFERENCES content_releases(realm_id,id)
  DEFERRABLE INITIALLY DEFERRED;
CREATE TABLE admission_tickets (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, auth_session_id uuid NOT NULL,
  account_id uuid NOT NULL, character_id uuid NOT NULL, content_release_id uuid NOT NULL,
  ticket_hash bytea NOT NULL CHECK (octet_length(ticket_hash)=32), protocol_version text NOT NULL DEFAULT 'game.v1',
  session_epoch bigint NOT NULL CHECK (session_epoch>0), issued_at timestamptz NOT NULL DEFAULT now(),
  expires_at timestamptz NOT NULL, consumed_at timestamptz, revoked_at timestamptz,
  reconnect_grace_seconds integer NOT NULL DEFAULT 15 CHECK (reconnect_grace_seconds=15),
  CHECK (expires_at>issued_at), UNIQUE (realm_id,id), UNIQUE (ticket_hash),
  FOREIGN KEY (realm_id,auth_session_id,account_id) REFERENCES auth_sessions(realm_id,id,account_id),
  FOREIGN KEY (realm_id,account_id) REFERENCES accounts(realm_id,id),
  FOREIGN KEY (realm_id,character_id,account_id) REFERENCES characters(realm_id,id,account_id),
  FOREIGN KEY (realm_id,content_release_id) REFERENCES content_releases(realm_id,id)
);
CREATE UNIQUE INDEX uq_outstanding_admission ON admission_tickets(realm_id,account_id)
  WHERE consumed_at IS NULL AND revoked_at IS NULL;
CREATE TABLE content_artifacts (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, content_release_id uuid NOT NULL,
  logical_path text NOT NULL CHECK (length(logical_path) BETWEEN 1 AND 512 AND logical_path !~ '(^/|\.\.)'), kind text NOT NULL
    CHECK (kind IN ('config','lua','map','sprite','audio','binary','localization')),
  media_type text NOT NULL CHECK (length(media_type) BETWEEN 3 AND 128),
  encoding text CHECK (encoding IS NULL OR length(encoding)<=32), size_bytes bigint NOT NULL CHECK (size_bytes>=0),
  sha256 bytea NOT NULL CHECK (octet_length(sha256)=32), object_uri text NOT NULL CHECK (length(object_uri)>0),
  source_snapshot_id text NOT NULL CHECK (length(source_snapshot_id) BETWEEN 7 AND 128),
  source_path text CHECK (source_path IS NULL OR length(source_path)<=1024),
  source_package text CHECK (source_package IS NULL OR length(source_package)<=256),
  source_uid integer CHECK (source_uid>=0), source_encoding text CHECK (source_encoding IS NULL OR length(source_encoding)<=32),
  discovery_tool text NOT NULL CHECK (discovery_tool IN ('semble','gitnexus','vltktool','manual','runtime','importer')),
  discovery_tool_revision text, query_used text CHECK (query_used IS NULL OR length(query_used)<=2048),
  evidence_note text CHECK (evidence_note IS NULL OR length(evidence_note)<=4096),
  parser_name text NOT NULL CHECK (length(parser_name) BETWEEN 1 AND 128),
  parser_version text NOT NULL CHECK (length(parser_version) BETWEEN 1 AND 64),
  source_package_index integer CHECK (source_package_index>=0), winner_status text,
  logical_path_bytes bytea, raw_sha256 bytea, decoded_sha256 bytea,
  source_locale text, user_facing_locale text, vietnamese_mode text, visual_debt_id text,
  fallback_policy_id text, parity_status text CHECK (parity_status IS NULL OR parity_status IN ('BLOCKED','GOLDEN_READY','PARITY_DONE')),
  normalized_sha256 bytea CHECK (normalized_sha256 IS NULL OR octet_length(normalized_sha256)=32),
  CHECK (raw_sha256 IS NULL OR octet_length(raw_sha256)=32),
  CHECK (decoded_sha256 IS NULL OR octet_length(decoded_sha256)=32),
  CHECK ((source_path IS NOT NULL) OR (source_package IS NOT NULL AND source_uid IS NOT NULL)),
  CHECK (kind <> 'sprite' OR (source_locale IN ('vi','zh','textless') AND user_facing_locale='vi'
      AND ((source_locale='vi' AND vietnamese_mode='native')
        OR (source_locale IN ('zh','textless') AND vietnamese_mode='runtime-text'
          AND visual_debt_id IS NOT NULL AND fallback_policy_id='SPR-FALLBACK-VI-RUNTIME-TEXT-V1'))
    AND source_package_index IS NOT NULL AND winner_status='resolved-first-match'
    AND logical_path_bytes IS NOT NULL AND raw_sha256 IS NOT NULL
    AND decoded_sha256 IS NOT NULL AND discovery_tool='vltktool'
    AND discovery_tool_revision IS NOT NULL AND parity_status IS NOT NULL)),
  FOREIGN KEY (realm_id,content_release_id,source_snapshot_id)
    REFERENCES content_releases(realm_id,id,source_snapshot_id),
  UNIQUE (realm_id,id), UNIQUE (realm_id,id,content_release_id),
  UNIQUE (realm_id,content_release_id,logical_path)
);
CREATE INDEX ix_artifact_source ON content_artifacts(realm_id,source_path,source_uid);
CREATE TABLE config_entries (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, content_release_id uuid NOT NULL,
  source_artifact_id uuid NOT NULL, namespace text NOT NULL, entry_key text NOT NULL, value jsonb NOT NULL,
  value_sha256 bytea NOT NULL CHECK (octet_length(value_sha256)=32), source_line integer CHECK (source_line>0),
  FOREIGN KEY (realm_id,content_release_id) REFERENCES content_releases(realm_id,id),
  FOREIGN KEY (realm_id,source_artifact_id,content_release_id)
    REFERENCES content_artifacts(realm_id,id,content_release_id),
  UNIQUE (realm_id,id), UNIQUE (realm_id,content_release_id,namespace,entry_key)
);
CREATE TABLE lua_modules (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, module_key text NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(), retired_at timestamptz, UNIQUE (realm_id,id), UNIQUE (realm_id,module_key)
);
CREATE TABLE lua_module_versions (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, lua_module_id uuid NOT NULL,
  content_release_id uuid NOT NULL, source_artifact_id uuid NOT NULL,
  source_sha256 bytea NOT NULL CHECK (octet_length(source_sha256)=32),
  bytecode_sha256 bytea CHECK (bytecode_sha256 IS NULL OR octet_length(bytecode_sha256)=32),
  lua_version text NOT NULL CHECK (lua_version='5.1'), sandbox_policy_version text NOT NULL,
  host_api_whitelist_sha256 bytea NOT NULL CHECK (octet_length(host_api_whitelist_sha256)=32),
  deterministic boolean NOT NULL CHECK (deterministic=true),
  instruction_limit integer NOT NULL DEFAULT 100000 CHECK (instruction_limit=100000),
  timeout_ms integer NOT NULL DEFAULT 5 CHECK (timeout_ms=5), memory_limit_bytes integer NOT NULL DEFAULT 8388608 CHECK (memory_limit_bytes=8388608),
  approved_by text NOT NULL, approved_at timestamptz NOT NULL,
  FOREIGN KEY (realm_id,lua_module_id) REFERENCES lua_modules(realm_id,id),
  FOREIGN KEY (realm_id,content_release_id,sandbox_policy_version,host_api_whitelist_sha256)
    REFERENCES content_releases(realm_id,id,lua_sandbox_policy_version,lua_host_api_whitelist_sha256),
  FOREIGN KEY (realm_id,source_artifact_id,content_release_id)
    REFERENCES content_artifacts(realm_id,id,content_release_id),
  UNIQUE (realm_id,id), UNIQUE (realm_id,lua_module_id,content_release_id)
);

CREATE TABLE inventory_items (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, character_id uuid NOT NULL,
  template_id integer NOT NULL CHECK (template_id>0), content_release_id uuid NOT NULL,
  container text NOT NULL CHECK (container IN ('bag','equipment','bank','mail','escrow')),
  slot integer NOT NULL CHECK (slot>=0 AND (container <> 'bag' OR slot BETWEEN 0 AND 59)),
  quantity integer NOT NULL CHECK (quantity>0), durability integer CHECK (durability>=0),
  attributes jsonb NOT NULL DEFAULT '{}' CHECK (jsonb_typeof(attributes)='object'), bound boolean NOT NULL DEFAULT false,
  deleted_at timestamptz, version bigint NOT NULL DEFAULT 1 CHECK (version>0),
  FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id),
  FOREIGN KEY (realm_id,content_release_id) REFERENCES content_releases(realm_id,id), UNIQUE (realm_id,id)
);
CREATE UNIQUE INDEX uq_inventory_slot ON inventory_items(realm_id,character_id,container,slot) WHERE deleted_at IS NULL;

CREATE TABLE wallets (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL REFERENCES realms(id),
  owner_type text NOT NULL CHECK (owner_type IN ('character','guild','system')), owner_id uuid NOT NULL,
  currency_code text NOT NULL CHECK (currency_code ~ '^[A-Z][A-Z0-9_]{1,15}$'), balance bigint NOT NULL DEFAULT 0,
  version bigint NOT NULL DEFAULT 1 CHECK (version>0), UNIQUE (realm_id,id),
  UNIQUE (realm_id,id,currency_code), UNIQUE (realm_id,owner_type,owner_id,currency_code)
);
CREATE TABLE economy_transactions (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL REFERENCES realms(id), operation text NOT NULL,
  idempotency_key text NOT NULL CHECK (length(idempotency_key) BETWEEN 16 AND 128), actor_character_id uuid,
  status text NOT NULL DEFAULT 'pending' CHECK (status IN ('pending','posted','reversed','failed')),
  reversal_of_id uuid, metadata jsonb NOT NULL DEFAULT '{}', created_at timestamptz NOT NULL DEFAULT now(),
  posted_at timestamptz, reversed_at timestamptz,
  FOREIGN KEY (realm_id,actor_character_id) REFERENCES characters(realm_id,id),
    FOREIGN KEY (realm_id,reversal_of_id) REFERENCES economy_transactions(realm_id,id),
  UNIQUE (realm_id,id), UNIQUE (realm_id,operation,idempotency_key),
  CHECK (
    (status IN ('pending','failed') AND posted_at IS NULL AND reversed_at IS NULL)
    OR (status='posted' AND posted_at IS NOT NULL AND reversed_at IS NULL)
    OR (status='reversed' AND posted_at IS NOT NULL AND reversed_at IS NOT NULL)
  )
  );
CREATE UNIQUE INDEX uq_economy_reversal ON economy_transactions(realm_id,reversal_of_id)
  WHERE reversal_of_id IS NOT NULL;
CREATE TABLE economy_entries (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, transaction_id uuid NOT NULL, wallet_id uuid NOT NULL,
  currency_code text NOT NULL, delta bigint NOT NULL CHECK (delta<>0), balance_after bigint NOT NULL,
  entry_index smallint NOT NULL CHECK (entry_index>=0), created_at timestamptz NOT NULL DEFAULT now(),
  FOREIGN KEY (realm_id,transaction_id) REFERENCES economy_transactions(realm_id,id),
    FOREIGN KEY (realm_id,wallet_id,currency_code) REFERENCES wallets(realm_id,id,currency_code),
    UNIQUE (realm_id,id), UNIQUE (realm_id,transaction_id,entry_index)
  );
CREATE FUNCTION guard_economy_entry() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF TG_OP <> 'INSERT' THEN
    RAISE EXCEPTION 'economy entries are append-only';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM economy_transactions t WHERE t.realm_id=NEW.realm_id
      AND t.id=NEW.transaction_id AND t.status='pending') THEN
    RAISE EXCEPTION 'economy entries require a pending transaction';
  END IF;
  RETURN NEW;
END $$;
CREATE TRIGGER ck_economy_entry_guard BEFORE INSERT OR UPDATE OR DELETE ON economy_entries
  FOR EACH ROW EXECUTE FUNCTION guard_economy_entry();
CREATE INDEX ix_ledger_wallet ON economy_entries(realm_id,wallet_id,created_at DESC,id);
CREATE FUNCTION guard_economy_transaction() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF TG_OP='INSERT' THEN
    IF NEW.status='pending' AND NEW.posted_at IS NULL AND NEW.reversed_at IS NULL THEN RETURN NEW; END IF;
    RAISE EXCEPTION 'economy transaction must be created pending';
  END IF;
  IF TG_OP='DELETE' THEN
    RAISE EXCEPTION 'economy transactions are append-only';
  END IF;
  IF NEW.id IS DISTINCT FROM OLD.id OR NEW.realm_id IS DISTINCT FROM OLD.realm_id
    OR NEW.operation IS DISTINCT FROM OLD.operation
    OR NEW.idempotency_key IS DISTINCT FROM OLD.idempotency_key
    OR NEW.actor_character_id IS DISTINCT FROM OLD.actor_character_id
    OR NEW.reversal_of_id IS DISTINCT FROM OLD.reversal_of_id
    OR NEW.metadata IS DISTINCT FROM OLD.metadata
    OR NEW.created_at IS DISTINCT FROM OLD.created_at THEN
    RAISE EXCEPTION 'economy transaction identity and payload are immutable';
  END IF;
  IF OLD.status='pending' AND NEW.status='posted'
    AND NEW.posted_at IS NOT NULL AND NEW.reversed_at IS NULL THEN RETURN NEW; END IF;
  IF OLD.status='pending' AND NEW.status='failed'
    AND NEW.posted_at IS NULL AND NEW.reversed_at IS NULL THEN RETURN NEW; END IF;
  IF OLD.status='posted' AND NEW.status='reversed'
    AND NEW.posted_at=OLD.posted_at AND NEW.reversed_at IS NOT NULL THEN RETURN NEW; END IF;
  RAISE EXCEPTION 'invalid or mutable economy transaction state';
END $$;
CREATE TRIGGER ck_economy_transaction_guard BEFORE INSERT OR UPDATE OR DELETE ON economy_transactions
  FOR EACH ROW EXECUTE FUNCTION guard_economy_transaction();
CREATE FUNCTION assert_balanced_economy() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
  IF NEW.status='posted' AND (NOT EXISTS (SELECT 1 FROM economy_entries e WHERE e.realm_id=NEW.realm_id AND e.transaction_id=NEW.id)
    OR EXISTS (SELECT 1 FROM economy_entries e WHERE e.realm_id=NEW.realm_id AND e.transaction_id=NEW.id GROUP BY currency_code HAVING sum(delta)<>0)) THEN
    RAISE EXCEPTION 'posted economy transaction must balance by currency';
  END IF;
  RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER ck_economy_balanced AFTER INSERT OR UPDATE OF status ON economy_transactions
DEFERRABLE INITIALLY DEFERRED FOR EACH ROW EXECUTE FUNCTION assert_balanced_economy();

CREATE TABLE runtime_checkpoints (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, character_id uuid NOT NULL,
  session_epoch bigint NOT NULL CHECK (session_epoch>0), server_tick bigint NOT NULL CHECK (server_tick>=0),
  last_client_seq bigint NOT NULL CHECK (last_client_seq>=0), schema_version integer NOT NULL DEFAULT 1,
  state_blob bytea NOT NULL, state_sha256 bytea NOT NULL CHECK (octet_length(state_sha256)=32),
  created_at timestamptz NOT NULL DEFAULT now(), superseded_at timestamptz,
  FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id), UNIQUE (realm_id,id),
  UNIQUE (realm_id,character_id,session_epoch,server_tick)
);
CREATE UNIQUE INDEX uq_current_checkpoint ON runtime_checkpoints(realm_id,character_id) WHERE superseded_at IS NULL;
CREATE TABLE encounter_preload_acks (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, character_id uuid NOT NULL,
  session_epoch bigint NOT NULL CHECK (session_epoch>0), encounter_id text NOT NULL CHECK (length(encounter_id) BETWEEN 1 AND 128),
  content_release_id uuid NOT NULL, content_digest_sha256 bytea NOT NULL CHECK (octet_length(content_digest_sha256)=32),
  skill_ids integer[] NOT NULL CHECK (array_length(skill_ids,1) IS NOT NULL),
  outcome text NOT NULL CHECK (outcome IN ('ready','content_mismatch','unavailable')),
  failure_code text, client_ready_tick bigint NOT NULL CHECK (client_ready_tick>=0),
  created_at timestamptz NOT NULL DEFAULT now(),
  FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id),
  FOREIGN KEY (realm_id,content_release_id) REFERENCES content_releases(realm_id,id),
  UNIQUE (realm_id,id), UNIQUE (realm_id,character_id,session_epoch,encounter_id)
);
CREATE TABLE combat_lifecycle_events (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, character_id uuid NOT NULL,
  session_epoch bigint NOT NULL CHECK (session_epoch>0), server_tick bigint NOT NULL CHECK (server_tick>=0),
  event_kind text NOT NULL CHECK (event_kind IN ('cast_recovery_started','cast_recovery_ended','missile_fly_started','missile_collided','missile_vanished','status_refreshed','status_expired')),
  combat_event_id text NOT NULL CHECK (length(combat_event_id) BETWEEN 1 AND 128),
  content_release_id uuid NOT NULL, payload jsonb NOT NULL CHECK (jsonb_typeof(payload)='object'),
  created_at timestamptz NOT NULL DEFAULT now(),
  FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id),
  FOREIGN KEY (realm_id,content_release_id) REFERENCES content_releases(realm_id,id),
  UNIQUE (realm_id,id), UNIQUE (realm_id,character_id,session_epoch,combat_event_id)
);
CREATE TABLE idempotency_keys (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL REFERENCES realms(id), actor_id uuid NOT NULL,
  operation text NOT NULL, idempotency_key text NOT NULL, request_hash bytea NOT NULL CHECK (octet_length(request_hash)=32),
  state text NOT NULL CHECK (state IN ('in_progress','completed','failed')), response_status integer,
  response_body bytea, created_at timestamptz NOT NULL DEFAULT now(), completed_at timestamptz, expires_at timestamptz NOT NULL,
  UNIQUE (realm_id,actor_id,operation,idempotency_key)
);
CREATE INDEX ix_idempotency_expiry ON idempotency_keys(expires_at);
CREATE TABLE outbox_events (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL REFERENCES realms(id), aggregate_type text NOT NULL,
  aggregate_id uuid NOT NULL, aggregate_version bigint NOT NULL CHECK (aggregate_version>0), event_type text NOT NULL,
  schema_version integer NOT NULL DEFAULT 1, payload jsonb NOT NULL, occurred_at timestamptz NOT NULL DEFAULT now(),
  available_at timestamptz NOT NULL DEFAULT now(), attempts integer NOT NULL DEFAULT 0, published_at timestamptz, last_error text,
  UNIQUE (realm_id,aggregate_type,aggregate_id,aggregate_version,event_type)
);
CREATE INDEX ix_outbox_pending ON outbox_events(available_at,occurred_at) WHERE published_at IS NULL;
CREATE TABLE audit_events (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL REFERENCES realms(id), actor_type text NOT NULL,
  actor_id text NOT NULL, action text NOT NULL, target_type text NOT NULL, target_id text NOT NULL,
  request_id uuid, trace_id text, before_hash bytea, after_hash bytea, metadata jsonb NOT NULL DEFAULT '{}',
  occurred_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_audit_target ON audit_events(realm_id,target_type,target_id,occurred_at DESC);

-- P1 identity completion: email lookup uses keyed HMAC; raw reset tokens are never stored.
CREATE TABLE password_reset_tokens (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, account_id uuid NOT NULL,
  token_hash bytea NOT NULL CHECK (octet_length(token_hash)=32), requested_at timestamptz NOT NULL DEFAULT now(),
  expires_at timestamptz NOT NULL, consumed_at timestamptz, requester_ip_hash bytea,
  FOREIGN KEY (realm_id,account_id) REFERENCES accounts(realm_id,id) ON DELETE CASCADE,
  UNIQUE (realm_id,id), UNIQUE (realm_id,token_hash), CHECK (expires_at>requested_at)
);
CREATE INDEX ix_password_reset_live ON password_reset_tokens(realm_id,account_id,expires_at) WHERE consumed_at IS NULL;

-- P2 world/channel transfer. Transfer commands are frozen between prepared and committed/failed.
CREATE TABLE world_channels (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL REFERENCES realms(id),
  map_id integer NOT NULL CHECK (map_id>=0), channel_no integer NOT NULL CHECK (channel_no>0),
  status text NOT NULL CHECK (status IN ('open','draining','closed')), capacity integer NOT NULL CHECK (capacity>0),
  population integer NOT NULL DEFAULT 0 CHECK (population>=0 AND population<=capacity),
  endpoint_key text NOT NULL, version bigint NOT NULL DEFAULT 1 CHECK (version>0),
  UNIQUE (realm_id,id), UNIQUE (realm_id,map_id,channel_no)
);
CREATE INDEX ix_channels_admission ON world_channels(realm_id,map_id,status,population);
CREATE TABLE character_transfers (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, character_id uuid NOT NULL,
  source_channel_id uuid NOT NULL, destination_channel_id uuid NOT NULL, party_id uuid,
  state text NOT NULL CHECK (state IN ('prepared','committed','failed','expired')),
  prepare_tick bigint NOT NULL CHECK (prepare_tick>=0), commit_tick bigint CHECK (commit_tick>=prepare_tick),
  transfer_token_hash bytea NOT NULL CHECK (octet_length(transfer_token_hash)=32), expires_at timestamptz NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(), completed_at timestamptz,
  FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id),
  FOREIGN KEY (realm_id,source_channel_id) REFERENCES world_channels(realm_id,id),
  FOREIGN KEY (realm_id,destination_channel_id) REFERENCES world_channels(realm_id,id),
  UNIQUE (realm_id,id), UNIQUE (realm_id,transfer_token_hash), CHECK (source_channel_id<>destination_channel_id)
);
CREATE UNIQUE INDEX uq_character_transfer_active ON character_transfers(realm_id,character_id)
WHERE state='prepared';

-- P1-P2 quest state. Reward grant key makes reconnect/retry exactly-once.
CREATE TABLE character_quests (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, character_id uuid NOT NULL,
  quest_id integer NOT NULL CHECK (quest_id>0), content_release_id uuid NOT NULL,
  state text NOT NULL CHECK (state IN ('accepted','active','completable','completed','failed','abandoned')),
  accepted_at timestamptz NOT NULL DEFAULT now(), completed_at timestamptz, revision bigint NOT NULL DEFAULT 1 CHECK (revision>0),
  FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id),
  FOREIGN KEY (realm_id,content_release_id) REFERENCES content_releases(realm_id,id),
  UNIQUE (realm_id,id), UNIQUE (realm_id,character_id,quest_id)
);
CREATE INDEX ix_character_quests_state ON character_quests(realm_id,character_id,state);
CREATE TABLE quest_objectives (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, character_quest_id uuid NOT NULL,
  objective_key text NOT NULL, current_value bigint NOT NULL DEFAULT 0 CHECK (current_value>=0),
  target_value bigint NOT NULL CHECK (target_value>0), updated_at timestamptz NOT NULL DEFAULT now(),
  FOREIGN KEY (realm_id,character_quest_id) REFERENCES character_quests(realm_id,id) ON DELETE CASCADE,
  UNIQUE (realm_id,id), UNIQUE (realm_id,character_quest_id,objective_key), CHECK (current_value<=target_value)
);
CREATE TABLE reward_grants (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, character_id uuid NOT NULL,
  source_type text NOT NULL CHECK (source_type IN ('quest','event','pvp','boss','rebirth','admin')),
  source_id uuid NOT NULL, reward_key text NOT NULL, economy_transaction_id uuid,
  status text NOT NULL CHECK (status IN ('pending','granted','reversed','failed')),
  granted_at timestamptz, created_at timestamptz NOT NULL DEFAULT now(),
  FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id),
  FOREIGN KEY (realm_id,economy_transaction_id) REFERENCES economy_transactions(realm_id,id),
  UNIQUE (realm_id,id), UNIQUE (realm_id,source_type,source_id,character_id,reward_key)
);

-- P2-P3 party/friend/chat moderation.
CREATE TABLE parties (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL REFERENCES realms(id), captain_character_id uuid NOT NULL,
  loot_policy text NOT NULL CHECK (loot_policy IN ('owner','round_robin','random','free_for_all')),
  state text NOT NULL DEFAULT 'active' CHECK (state IN ('active','disbanded')),
  created_at timestamptz NOT NULL DEFAULT now(), disbanded_at timestamptz, version bigint NOT NULL DEFAULT 1 CHECK (version>0),
  FOREIGN KEY (realm_id,captain_character_id) REFERENCES characters(realm_id,id), UNIQUE (realm_id,id)
);
CREATE TABLE party_members (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, party_id uuid NOT NULL, character_id uuid NOT NULL,
  role text NOT NULL CHECK (role IN ('captain','member')), joined_at timestamptz NOT NULL DEFAULT now(), left_at timestamptz,
  FOREIGN KEY (realm_id,party_id) REFERENCES parties(realm_id,id),
  FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id), UNIQUE (realm_id,id)
);
CREATE UNIQUE INDEX uq_party_active_character ON party_members(realm_id,character_id) WHERE left_at IS NULL;
CREATE UNIQUE INDEX uq_party_active_captain ON party_members(realm_id,party_id) WHERE left_at IS NULL AND role='captain';
CREATE TABLE party_invites (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, party_id uuid NOT NULL,
  inviter_character_id uuid NOT NULL, invitee_character_id uuid NOT NULL,
  state text NOT NULL CHECK (state IN ('pending','accepted','declined','expired','revoked')),
  created_at timestamptz NOT NULL DEFAULT now(), expires_at timestamptz NOT NULL, responded_at timestamptz,
  FOREIGN KEY (realm_id,party_id) REFERENCES parties(realm_id,id),
  FOREIGN KEY (realm_id,inviter_character_id) REFERENCES characters(realm_id,id),
  FOREIGN KEY (realm_id,invitee_character_id) REFERENCES characters(realm_id,id),
  UNIQUE (realm_id,id), CHECK (inviter_character_id<>invitee_character_id), CHECK (expires_at>created_at)
);
CREATE UNIQUE INDEX uq_party_invite_pending ON party_invites(realm_id,party_id,invitee_character_id) WHERE state='pending';
CREATE TABLE friendships (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL,
  character_low_id uuid NOT NULL, character_high_id uuid NOT NULL,
  state text NOT NULL CHECK (state IN ('pending_low_high','pending_high_low','accepted','blocked_low_high','blocked_high_low','removed')),
  requested_at timestamptz NOT NULL DEFAULT now(), updated_at timestamptz NOT NULL DEFAULT now(),
  FOREIGN KEY (realm_id,character_low_id) REFERENCES characters(realm_id,id),
  FOREIGN KEY (realm_id,character_high_id) REFERENCES characters(realm_id,id),
  UNIQUE (realm_id,id), UNIQUE (realm_id,character_low_id,character_high_id), CHECK (character_low_id<character_high_id)
);
CREATE INDEX ix_friend_high ON friendships(realm_id,character_high_id,state);
CREATE TABLE chat_messages (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, sender_character_id uuid NOT NULL,
  channel_type text NOT NULL CHECK (channel_type IN ('world','map','party','guild','whisper','system')),
  channel_ref_id uuid, recipient_character_id uuid, body_ciphertext bytea NOT NULL,
  body_hash bytea NOT NULL CHECK (octet_length(body_hash)=32), sent_at timestamptz NOT NULL DEFAULT now(),
  moderation_state text NOT NULL DEFAULT 'visible' CHECK (moderation_state IN ('visible','hidden','deleted')),
  FOREIGN KEY (realm_id,sender_character_id) REFERENCES characters(realm_id,id),
  FOREIGN KEY (realm_id,recipient_character_id) REFERENCES characters(realm_id,id), UNIQUE (realm_id,id)
);
CREATE INDEX ix_chat_channel_time ON chat_messages(realm_id,channel_type,channel_ref_id,sent_at DESC);
CREATE TABLE chat_reports (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, message_id uuid NOT NULL,
  reporter_character_id uuid NOT NULL, reason_code text NOT NULL, note_ciphertext bytea,
  status text NOT NULL DEFAULT 'open' CHECK (status IN ('open','reviewing','resolved','dismissed')),
  created_at timestamptz NOT NULL DEFAULT now(), resolved_at timestamptz,
  FOREIGN KEY (realm_id,message_id) REFERENCES chat_messages(realm_id,id),
  FOREIGN KEY (realm_id,reporter_character_id) REFERENCES characters(realm_id,id),
  UNIQUE (realm_id,id), UNIQUE (realm_id,message_id,reporter_character_id)
);
CREATE INDEX ix_chat_reports_queue ON chat_reports(realm_id,status,created_at);

-- P3 direct trade and stall. Item ownership and money post atomically with ledger/outbox.
CREATE TABLE trades (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL,
  initiator_character_id uuid NOT NULL, counterparty_character_id uuid NOT NULL,
  state text NOT NULL CHECK (state IN ('open','locked','committed','cancelled','expired','failed')),
  revision bigint NOT NULL DEFAULT 1 CHECK (revision>0), initiator_confirmed_revision bigint,
  counterparty_confirmed_revision bigint, economy_transaction_id uuid,
  created_at timestamptz NOT NULL DEFAULT now(), expires_at timestamptz NOT NULL, completed_at timestamptz,
  FOREIGN KEY (realm_id,initiator_character_id) REFERENCES characters(realm_id,id),
  FOREIGN KEY (realm_id,counterparty_character_id) REFERENCES characters(realm_id,id),
  FOREIGN KEY (realm_id,economy_transaction_id) REFERENCES economy_transactions(realm_id,id),
  UNIQUE (realm_id,id), CHECK (initiator_character_id<>counterparty_character_id), CHECK (expires_at>created_at)
);
CREATE INDEX ix_trades_participant ON trades(realm_id,initiator_character_id,state,created_at DESC);
CREATE INDEX ix_trades_counterparty ON trades(realm_id,counterparty_character_id,state,created_at DESC);
CREATE TABLE trade_items (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, trade_id uuid NOT NULL,
  offered_by_character_id uuid NOT NULL, inventory_item_id uuid NOT NULL, quantity integer NOT NULL CHECK (quantity>0),
  offer_revision bigint NOT NULL CHECK (offer_revision>0),
  FOREIGN KEY (realm_id,trade_id) REFERENCES trades(realm_id,id) ON DELETE CASCADE,
  FOREIGN KEY (realm_id,offered_by_character_id) REFERENCES characters(realm_id,id),
  FOREIGN KEY (realm_id,inventory_item_id) REFERENCES inventory_items(realm_id,id),
  UNIQUE (realm_id,id), UNIQUE (realm_id,trade_id,inventory_item_id)
);
CREATE TABLE trade_currency_offers (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, trade_id uuid NOT NULL,
  offered_by_character_id uuid NOT NULL, currency_code text NOT NULL, amount bigint NOT NULL CHECK (amount>0),
  offer_revision bigint NOT NULL CHECK (offer_revision>0),
  FOREIGN KEY (realm_id,trade_id) REFERENCES trades(realm_id,id) ON DELETE CASCADE,
  FOREIGN KEY (realm_id,offered_by_character_id) REFERENCES characters(realm_id,id),
  UNIQUE (realm_id,id), UNIQUE (realm_id,trade_id,offered_by_character_id,currency_code)
);
CREATE TABLE stalls (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, owner_character_id uuid NOT NULL,
  channel_id uuid NOT NULL, name text NOT NULL CHECK (length(name) BETWEEN 1 AND 40),
  state text NOT NULL CHECK (state IN ('open','closed','suspended')),
  opened_at timestamptz NOT NULL DEFAULT now(), closed_at timestamptz, version bigint NOT NULL DEFAULT 1 CHECK (version>0),
  FOREIGN KEY (realm_id,owner_character_id) REFERENCES characters(realm_id,id),
  FOREIGN KEY (realm_id,channel_id) REFERENCES world_channels(realm_id,id), UNIQUE (realm_id,id)
);
CREATE UNIQUE INDEX uq_stall_owner_open ON stalls(realm_id,owner_character_id) WHERE state='open';
CREATE TABLE stall_listings (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, stall_id uuid NOT NULL,
  inventory_item_id uuid NOT NULL, quantity integer NOT NULL CHECK (quantity>0), currency_code text NOT NULL,
  unit_price bigint NOT NULL CHECK (unit_price>0), remaining_quantity integer NOT NULL CHECK (remaining_quantity>=0),
  state text NOT NULL CHECK (state IN ('listed','sold','cancelled','expired')),
  created_at timestamptz NOT NULL DEFAULT now(), expires_at timestamptz NOT NULL, version bigint NOT NULL DEFAULT 1 CHECK (version>0),
  FOREIGN KEY (realm_id,stall_id) REFERENCES stalls(realm_id,id),
  FOREIGN KEY (realm_id,inventory_item_id) REFERENCES inventory_items(realm_id,id),
  UNIQUE (realm_id,id), UNIQUE (realm_id,stall_id,inventory_item_id), CHECK (remaining_quantity<=quantity)
);
CREATE INDEX ix_stall_listing_search ON stall_listings(realm_id,state,currency_code,unit_price) WHERE state='listed';

-- P3 guild membership/RBAC.
CREATE TABLE guilds (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL REFERENCES realms(id),
  name citext NOT NULL CHECK (length(name) BETWEEN 2 AND 32), leader_character_id uuid NOT NULL,
  level integer NOT NULL DEFAULT 1 CHECK (level>0), notice text NOT NULL DEFAULT '',
  state text NOT NULL DEFAULT 'active' CHECK (state IN ('active','disbanded')),
  created_at timestamptz NOT NULL DEFAULT now(), disbanded_at timestamptz, version bigint NOT NULL DEFAULT 1 CHECK (version>0),
  FOREIGN KEY (realm_id,leader_character_id) REFERENCES characters(realm_id,id), UNIQUE (realm_id,id)
);
CREATE UNIQUE INDEX uq_guild_active_name ON guilds(realm_id,name) WHERE state='active';
CREATE TABLE guild_members (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, guild_id uuid NOT NULL, character_id uuid NOT NULL,
  role text NOT NULL CHECK (role IN ('leader','officer','member')), permissions bigint NOT NULL DEFAULT 0 CHECK (permissions>=0),
  contribution bigint NOT NULL DEFAULT 0 CHECK (contribution>=0), joined_at timestamptz NOT NULL DEFAULT now(),
  left_at timestamptz, leave_cooldown_until timestamptz,
  FOREIGN KEY (realm_id,guild_id) REFERENCES guilds(realm_id,id),
  FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id), UNIQUE (realm_id,id)
);
CREATE UNIQUE INDEX uq_guild_active_character ON guild_members(realm_id,character_id) WHERE left_at IS NULL;
CREATE UNIQUE INDEX uq_guild_active_leader ON guild_members(realm_id,guild_id) WHERE left_at IS NULL AND role='leader';

-- P2-P3 companions. Exact templates/rules remain content-release owned.
CREATE TABLE character_mounts (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, character_id uuid NOT NULL,
  mount_template_id integer NOT NULL CHECK (mount_template_id>0), content_release_id uuid NOT NULL,
  source_item_id uuid, level integer NOT NULL DEFAULT 1 CHECK (level>0), experience bigint NOT NULL DEFAULT 0 CHECK (experience>=0),
  equipped boolean NOT NULL DEFAULT false, riding boolean NOT NULL DEFAULT false,
  created_at timestamptz NOT NULL DEFAULT now(), version bigint NOT NULL DEFAULT 1 CHECK (version>0),
  FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id),
  FOREIGN KEY (realm_id,content_release_id) REFERENCES content_releases(realm_id,id),
  FOREIGN KEY (realm_id,source_item_id) REFERENCES inventory_items(realm_id,id), UNIQUE (realm_id,id)
);
CREATE UNIQUE INDEX uq_character_equipped_mount ON character_mounts(realm_id,character_id) WHERE equipped;
CREATE UNIQUE INDEX uq_character_riding_mount ON character_mounts(realm_id,character_id) WHERE riding;
CREATE TABLE character_pets (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, character_id uuid NOT NULL,
  pet_template_id integer NOT NULL CHECK (pet_template_id>0), content_release_id uuid NOT NULL,
  name text NOT NULL CHECK (length(name) BETWEEN 1 AND 24), level integer NOT NULL DEFAULT 1 CHECK (level>0),
  experience bigint NOT NULL DEFAULT 0 CHECK (experience>=0), mode text NOT NULL DEFAULT 'follow'
    CHECK (mode IN ('follow','assist','passive','stay')), active boolean NOT NULL DEFAULT false,
  state jsonb NOT NULL DEFAULT '{}' CHECK (jsonb_typeof(state)='object'),
  created_at timestamptz NOT NULL DEFAULT now(), version bigint NOT NULL DEFAULT 1 CHECK (version>0),
  FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id),
  FOREIGN KEY (realm_id,content_release_id) REFERENCES content_releases(realm_id,id), UNIQUE (realm_id,id)
);
CREATE INDEX ix_character_pets ON character_pets(realm_id,character_id,active);

-- P4 PK, seasons, events and rebirth. Thresholds/formulas are content references, never guessed in DDL.
CREATE TABLE character_pvp_profiles (
  realm_id uuid NOT NULL, character_id uuid NOT NULL, pk_mode text NOT NULL DEFAULT 'peace'
    CHECK (pk_mode IN ('peace','team','guild','all','event')), pk_value bigint NOT NULL DEFAULT 0 CHECK (pk_value>=0),
  mode_changed_at timestamptz NOT NULL DEFAULT now(), cooldown_until timestamptz, version bigint NOT NULL DEFAULT 1 CHECK (version>0),
  PRIMARY KEY (realm_id,character_id), FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id)
);
CREATE TABLE pvp_seasons (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL REFERENCES realms(id), season_key text NOT NULL,
  content_release_id uuid NOT NULL, starts_at timestamptz NOT NULL, ends_at timestamptz NOT NULL,
  state text NOT NULL CHECK (state IN ('scheduled','active','finalizing','closed')),
  FOREIGN KEY (realm_id,content_release_id) REFERENCES content_releases(realm_id,id),
  UNIQUE (realm_id,id), UNIQUE (realm_id,season_key), CHECK (ends_at>starts_at)
);
CREATE TABLE pvp_ladder_entries (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, season_id uuid NOT NULL, character_id uuid NOT NULL,
  rating bigint NOT NULL DEFAULT 0, wins integer NOT NULL DEFAULT 0 CHECK (wins>=0), losses integer NOT NULL DEFAULT 0 CHECK (losses>=0),
  rank integer CHECK (rank>0), updated_at timestamptz NOT NULL DEFAULT now(), version bigint NOT NULL DEFAULT 1 CHECK (version>0),
  FOREIGN KEY (realm_id,season_id) REFERENCES pvp_seasons(realm_id,id),
  FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id),
  UNIQUE (realm_id,id), UNIQUE (realm_id,season_id,character_id)
);
CREATE INDEX ix_pvp_ladder_rank ON pvp_ladder_entries(realm_id,season_id,rating DESC,character_id);
CREATE TABLE game_events (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL REFERENCES realms(id), event_key text NOT NULL,
  event_type text NOT NULL CHECK (event_type IN ('pvp','boss','seasonal','world')),
  content_release_id uuid NOT NULL, starts_at timestamptz NOT NULL, ends_at timestamptz NOT NULL,
  state text NOT NULL CHECK (state IN ('scheduled','enrollment','active','settling','closed','cancelled')),
  checkpoint jsonb NOT NULL DEFAULT '{}' CHECK (jsonb_typeof(checkpoint)='object'), version bigint NOT NULL DEFAULT 1 CHECK (version>0),
  FOREIGN KEY (realm_id,content_release_id) REFERENCES content_releases(realm_id,id),
  UNIQUE (realm_id,id), UNIQUE (realm_id,event_key,starts_at), CHECK (ends_at>starts_at)
);
CREATE INDEX ix_game_events_schedule ON game_events(realm_id,state,starts_at);
CREATE TABLE event_participants (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, event_id uuid NOT NULL, character_id uuid NOT NULL,
  state text NOT NULL CHECK (state IN ('enrolled','active','finished','forfeited','disqualified')),
  score bigint NOT NULL DEFAULT 0, rank integer CHECK (rank>0), contribution jsonb NOT NULL DEFAULT '{}',
  joined_at timestamptz NOT NULL DEFAULT now(), finished_at timestamptz,
  FOREIGN KEY (realm_id,event_id) REFERENCES game_events(realm_id,id),
  FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id),
  UNIQUE (realm_id,id), UNIQUE (realm_id,event_id,character_id)
);
CREATE INDEX ix_event_ranking ON event_participants(realm_id,event_id,score DESC,character_id);
CREATE TABLE character_rebirths (
  id uuid PRIMARY KEY DEFAULT gen_random_uuid(), realm_id uuid NOT NULL, character_id uuid NOT NULL,
  rebirth_no integer NOT NULL CHECK (rebirth_no>0), content_release_id uuid NOT NULL,
  previous_level integer NOT NULL CHECK (previous_level BETWEEN 1 AND 200), resulting_level integer NOT NULL CHECK (resulting_level BETWEEN 1 AND 200),
  reward_grant_id uuid, performed_at timestamptz NOT NULL DEFAULT now(), state_before_hash bytea NOT NULL,
  state_after_hash bytea NOT NULL, FOREIGN KEY (realm_id,character_id) REFERENCES characters(realm_id,id),
  FOREIGN KEY (realm_id,content_release_id) REFERENCES content_releases(realm_id,id),
  FOREIGN KEY (realm_id,reward_grant_id) REFERENCES reward_grants(realm_id,id),
  UNIQUE (realm_id,id), UNIQUE (realm_id,character_id,rebirth_no)
);
ALTER TABLE character_transfers ADD CONSTRAINT fk_transfer_party
  FOREIGN KEY (realm_id,party_id) REFERENCES parties(realm_id,id);

DO $$ DECLARE t text; BEGIN FOREACH t IN ARRAY ARRAY[
 'accounts','auth_sessions','admission_tickets','password_reset_tokens','characters','character_stats','character_positions','character_skills',
 'content_releases','content_artifacts','config_entries','lua_modules','lua_module_versions','inventory_items',
 'wallets','economy_transactions','economy_entries','runtime_checkpoints','idempotency_keys','outbox_events','audit_events',
 'world_channels','character_transfers','character_quests','quest_objectives','reward_grants',
 'parties','party_members','party_invites','friendships','chat_messages','chat_reports',
 'trades','trade_items','trade_currency_offers','stalls','stall_listings','guilds','guild_members',
 'character_mounts','character_pets','character_pvp_profiles','pvp_seasons','pvp_ladder_entries',
 'game_events','event_participants','character_rebirths'
] LOOP
 EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY',t);
 EXECUTE format('CREATE POLICY realm_isolation ON %I USING (realm_id=nullif(current_setting(''app.realm_id'',true),'''')::uuid) WITH CHECK (realm_id=nullif(current_setting(''app.realm_id'',true),'''')::uuid)',t);
END LOOP; END $$;
COMMIT;
