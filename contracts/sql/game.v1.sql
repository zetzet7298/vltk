-- PostgreSQL 18+ logical contract for DevHarness game.v1.
-- All application connections MUST set app.realm_id and use RLS in production.
BEGIN;

CREATE EXTENSION IF NOT EXISTS pgcrypto;
CREATE EXTENSION IF NOT EXISTS citext;

CREATE TABLE realms (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    code citext NOT NULL UNIQUE CHECK (code ~ '^[a-z0-9][a-z0-9-]{1,31}$'),
    name text NOT NULL CHECK (length(name) BETWEEN 1 AND 80),
    status text NOT NULL DEFAULT 'closed' CHECK (status IN ('open','maintenance','closed')),
    active_content_release_id uuid,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
    UNIQUE (id, active_content_release_id)
);

CREATE TABLE accounts (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    realm_id uuid NOT NULL REFERENCES realms(id) ON DELETE RESTRICT,
    account_name citext NOT NULL CHECK (length(account_name) BETWEEN 1 AND 32),
    password_hash text NOT NULL CHECK (length(password_hash) BETWEEN 20 AND 512),
    status text NOT NULL DEFAULT 'active' CHECK (status IN ('active','locked','banned','disabled')),
    otp_secret_ciphertext bytea,
    token_version integer NOT NULL DEFAULT 1 CHECK (token_version > 0),
    legacy_acc_name text,
    service_flag integer NOT NULL DEFAULT 0,
    ext_point bigint NOT NULL DEFAULT 0 CHECK (ext_point >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
    UNIQUE (realm_id, id)
);
CREATE UNIQUE INDEX uq_accounts_active_name ON accounts(realm_id, account_name) WHERE deleted_at IS NULL;
CREATE INDEX ix_accounts_status ON accounts(realm_id, status) WHERE deleted_at IS NULL;

CREATE TABLE auth_sessions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    realm_id uuid NOT NULL,
    account_id uuid NOT NULL,
    refresh_token_id uuid NOT NULL UNIQUE,
    refresh_token_hash bytea NOT NULL CHECK (octet_length(refresh_token_hash) = 32),
    token_family_id uuid NOT NULL,
    device_id text CHECK (length(device_id) <= 128),
    issued_at timestamptz NOT NULL DEFAULT now(),
    expires_at timestamptz NOT NULL,
    rotated_at timestamptz,
    revoked_at timestamptz,
    revoke_reason text,
    last_seen_at timestamptz,
    CHECK (expires_at > issued_at),
    FOREIGN KEY (realm_id, account_id) REFERENCES accounts(realm_id, id) ON DELETE CASCADE,
    UNIQUE (realm_id, id)
);
CREATE INDEX ix_auth_sessions_account_live ON auth_sessions(realm_id, account_id, expires_at) WHERE revoked_at IS NULL;
CREATE INDEX ix_auth_sessions_family ON auth_sessions(token_family_id);

CREATE TABLE characters (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    realm_id uuid NOT NULL,
    account_id uuid NOT NULL,
    legacy_role_id bigint,
    name citext NOT NULL CHECK (length(name) BETWEEN 2 AND 24),
    faction smallint NOT NULL DEFAULT -1 CHECK (faction BETWEEN -1 AND 9),
    series smallint NOT NULL CHECK (series BETWEEN 0 AND 4),
    level smallint NOT NULL DEFAULT 1 CHECK (level BETWEEN 1 AND 200),
    appearance jsonb NOT NULL DEFAULT '{}'::jsonb CHECK (jsonb_typeof(appearance) = 'object'),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    purge_after timestamptz,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
    CHECK (purge_after IS NULL OR deleted_at IS NOT NULL),
    FOREIGN KEY (realm_id, account_id) REFERENCES accounts(realm_id, id) ON DELETE RESTRICT,
    UNIQUE (realm_id, id),
    UNIQUE (realm_id, legacy_role_id)
);
CREATE UNIQUE INDEX uq_characters_active_name ON characters(realm_id, name) WHERE deleted_at IS NULL;
CREATE INDEX ix_characters_account ON characters(realm_id, account_id, created_at) WHERE deleted_at IS NULL;
CREATE INDEX ix_characters_purge ON characters(purge_after) WHERE purge_after IS NOT NULL;

CREATE TABLE character_stats (
    realm_id uuid NOT NULL,
    character_id uuid NOT NULL,
    experience bigint NOT NULL DEFAULT 0 CHECK (experience >= 0),
    trans_life integer NOT NULL DEFAULT 0 CHECK (trans_life >= 0),
    free_point integer NOT NULL DEFAULT 0 CHECK (free_point >= 0),
    magic_point integer NOT NULL DEFAULT 0 CHECK (magic_point >= 0),
    strength integer NOT NULL CHECK (strength >= 0),
    dexterity integer NOT NULL CHECK (dexterity >= 0),
    vitality integer NOT NULL CHECK (vitality >= 0),
    spirit integer NOT NULL CHECK (spirit >= 0),
    repute bigint NOT NULL DEFAULT 0 CHECK (repute >= 0),
    current_life bigint NOT NULL DEFAULT 1 CHECK (current_life >= 0),
    current_mana bigint NOT NULL DEFAULT 0 CHECK (current_mana >= 0),
    current_stamina bigint NOT NULL DEFAULT 0 CHECK (current_stamina >= 0),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
    PRIMARY KEY (realm_id, character_id),
    FOREIGN KEY (realm_id, character_id) REFERENCES characters(realm_id, id) ON DELETE RESTRICT
);

CREATE TABLE character_positions (
    realm_id uuid NOT NULL,
    character_id uuid NOT NULL,
    map_id integer NOT NULL CHECK (map_id >= 0),
    pos_x integer NOT NULL,
    pos_y integer NOT NULL,
    facing_millirad integer NOT NULL DEFAULT 0 CHECK (facing_millirad BETWEEN 0 AND 6283),
    server_tick bigint NOT NULL DEFAULT 0 CHECK (server_tick >= 0),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
    PRIMARY KEY (realm_id, character_id),
    FOREIGN KEY (realm_id, character_id) REFERENCES characters(realm_id, id) ON DELETE RESTRICT
);
CREATE INDEX ix_positions_map ON character_positions(realm_id, map_id);

CREATE TABLE character_skills (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    realm_id uuid NOT NULL,
    character_id uuid NOT NULL,
    skill_id integer NOT NULL CHECK (skill_id > 0),
    level smallint NOT NULL DEFAULT 1 CHECK (level > 0),
    is_active boolean NOT NULL DEFAULT true,
    last_cast_tick bigint NOT NULL DEFAULT 0 CHECK (last_cast_tick >= 0),
    learned_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
    FOREIGN KEY (realm_id, character_id) REFERENCES characters(realm_id, id) ON DELETE RESTRICT,
    UNIQUE (realm_id, id),
    UNIQUE (realm_id, character_id, skill_id)
);
CREATE INDEX ix_character_skills_active ON character_skills(realm_id, character_id) WHERE is_active;

CREATE TABLE wallets (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    realm_id uuid NOT NULL REFERENCES realms(id) ON DELETE RESTRICT,
    owner_type text NOT NULL CHECK (owner_type IN ('character','guild','system')),
    owner_id uuid NOT NULL,
    currency_code text NOT NULL CHECK (currency_code ~ '^[A-Z][A-Z0-9_]{1,15}$'),
    balance bigint NOT NULL DEFAULT 0,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
    UNIQUE (realm_id, id),
    UNIQUE (realm_id, owner_type, owner_id, currency_code)
);
CREATE INDEX ix_wallet_owner ON wallets(realm_id, owner_type, owner_id);

CREATE TABLE economy_transactions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    realm_id uuid NOT NULL REFERENCES realms(id) ON DELETE RESTRICT,
    operation text NOT NULL CHECK (length(operation) BETWEEN 1 AND 80),
    idempotency_key text NOT NULL CHECK (length(idempotency_key) BETWEEN 16 AND 128),
    actor_account_id uuid,
    actor_character_id uuid,
    status text NOT NULL DEFAULT 'pending' CHECK (status IN ('pending','posted','reversed','failed')),
    reversal_of_id uuid,
    metadata jsonb NOT NULL DEFAULT '{}'::jsonb CHECK (jsonb_typeof(metadata) = 'object'),
    created_at timestamptz NOT NULL DEFAULT now(),
    posted_at timestamptz,
    FOREIGN KEY (realm_id, actor_account_id) REFERENCES accounts(realm_id, id) ON DELETE RESTRICT,
    FOREIGN KEY (realm_id, actor_character_id) REFERENCES characters(realm_id, id) ON DELETE RESTRICT,
    FOREIGN KEY (realm_id, reversal_of_id) REFERENCES economy_transactions(realm_id, id) ON DELETE RESTRICT,
    UNIQUE (realm_id, id),
    UNIQUE (realm_id, operation, idempotency_key),
    CHECK ((status = 'posted') = (posted_at IS NOT NULL) OR status IN ('reversed','failed')),
    CHECK (reversal_of_id IS NULL OR reversal_of_id <> id)
);
CREATE INDEX ix_economy_tx_actor ON economy_transactions(realm_id, actor_character_id, created_at DESC);

CREATE TABLE economy_entries (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    realm_id uuid NOT NULL,
    transaction_id uuid NOT NULL,
    wallet_id uuid NOT NULL,
    currency_code text NOT NULL CHECK (currency_code ~ '^[A-Z][A-Z0-9_]{1,15}$'),
    delta bigint NOT NULL CHECK (delta <> 0),
    balance_after bigint NOT NULL,
    entry_index smallint NOT NULL CHECK (entry_index >= 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    FOREIGN KEY (realm_id, transaction_id) REFERENCES economy_transactions(realm_id, id) ON DELETE RESTRICT,
    FOREIGN KEY (realm_id, wallet_id) REFERENCES wallets(realm_id, id) ON DELETE RESTRICT,
    UNIQUE (realm_id, id),
    UNIQUE (realm_id, transaction_id, entry_index)
);
CREATE INDEX ix_economy_entries_wallet ON economy_entries(realm_id, wallet_id, created_at DESC, id);

CREATE FUNCTION assert_posted_economy_balanced(p_realm uuid, p_tx_id uuid) RETURNS void LANGUAGE plpgsql AS $$
DECLARE tx_status text;
BEGIN
    SELECT status INTO tx_status FROM economy_transactions WHERE realm_id = p_realm AND id = p_tx_id;
    IF tx_status = 'posted' AND EXISTS (
        SELECT 1 FROM economy_entries
        WHERE realm_id = p_realm AND transaction_id = p_tx_id
        GROUP BY currency_code HAVING sum(delta) <> 0
    ) THEN
        RAISE EXCEPTION 'posted economy transaction % is not balanced', p_tx_id;
    END IF;
    IF tx_status = 'posted' AND NOT EXISTS (
        SELECT 1 FROM economy_entries WHERE realm_id = p_realm AND transaction_id = p_tx_id
    ) THEN
        RAISE EXCEPTION 'posted economy transaction % has no entries', p_tx_id;
    END IF;
END $$;
CREATE FUNCTION check_economy_entry_balance() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    PERFORM assert_posted_economy_balanced(
        COALESCE(NEW.realm_id, OLD.realm_id), COALESCE(NEW.transaction_id, OLD.transaction_id)
    );
    RETURN NULL;
END $$;
CREATE FUNCTION check_economy_transaction_balance() RETURNS trigger LANGUAGE plpgsql AS $$
BEGIN
    PERFORM assert_posted_economy_balanced(NEW.realm_id, NEW.id);
    RETURN NULL;
END $$;
CREATE CONSTRAINT TRIGGER ck_economy_entries_balanced
AFTER INSERT OR UPDATE OR DELETE ON economy_entries DEFERRABLE INITIALLY DEFERRED
FOR EACH ROW EXECUTE FUNCTION check_economy_entry_balance();
CREATE CONSTRAINT TRIGGER ck_economy_tx_balanced
AFTER INSERT OR UPDATE OF status ON economy_transactions DEFERRABLE INITIALLY DEFERRED
FOR EACH ROW EXECUTE FUNCTION check_economy_transaction_balance();

CREATE TABLE inventory_items (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    realm_id uuid NOT NULL,
    character_id uuid NOT NULL,
    template_id integer NOT NULL CHECK (template_id > 0),
    content_release_id uuid NOT NULL,
    container text NOT NULL CHECK (container IN ('bag','equipment','bank','mail','escrow')),
    slot integer NOT NULL CHECK (slot >= 0),
    quantity integer NOT NULL CHECK (quantity > 0),
    durability integer CHECK (durability >= 0),
    attributes jsonb NOT NULL DEFAULT '{}'::jsonb CHECK (jsonb_typeof(attributes) = 'object'),
    bound boolean NOT NULL DEFAULT false,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    deleted_at timestamptz,
    version bigint NOT NULL DEFAULT 1 CHECK (version > 0),
    FOREIGN KEY (realm_id, character_id) REFERENCES characters(realm_id, id) ON DELETE RESTRICT,
    UNIQUE (realm_id, id)
);
CREATE UNIQUE INDEX uq_inventory_active_slot ON inventory_items(realm_id, character_id, container, slot) WHERE deleted_at IS NULL;
CREATE INDEX ix_inventory_template ON inventory_items(realm_id, character_id, template_id) WHERE deleted_at IS NULL;

CREATE TABLE runtime_checkpoints (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    realm_id uuid NOT NULL,
    character_id uuid NOT NULL,
    session_epoch bigint NOT NULL CHECK (session_epoch > 0),
    server_tick bigint NOT NULL CHECK (server_tick >= 0),
    last_client_seq bigint NOT NULL CHECK (last_client_seq >= 0),
    schema_version integer NOT NULL DEFAULT 1 CHECK (schema_version > 0),
    state_blob bytea NOT NULL,
    state_sha256 bytea NOT NULL CHECK (octet_length(state_sha256) = 32),
    created_at timestamptz NOT NULL DEFAULT now(),
    superseded_at timestamptz,
    FOREIGN KEY (realm_id, character_id) REFERENCES characters(realm_id, id) ON DELETE RESTRICT,
    UNIQUE (realm_id, id),
    UNIQUE (realm_id, character_id, session_epoch, server_tick)
);
CREATE UNIQUE INDEX uq_runtime_current_checkpoint ON runtime_checkpoints(realm_id, character_id) WHERE superseded_at IS NULL;
CREATE INDEX ix_runtime_checkpoint_history ON runtime_checkpoints(realm_id, character_id, server_tick DESC);

CREATE TABLE idempotency_keys (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    realm_id uuid NOT NULL REFERENCES realms(id) ON DELETE RESTRICT,
    actor_id uuid NOT NULL,
    operation text NOT NULL CHECK (length(operation) BETWEEN 1 AND 120),
    idempotency_key text NOT NULL CHECK (length(idempotency_key) BETWEEN 16 AND 128),
    request_hash bytea NOT NULL CHECK (octet_length(request_hash) = 32),
    state text NOT NULL CHECK (state IN ('in_progress','completed','failed')),
    response_status integer CHECK (response_status BETWEEN 100 AND 599),
    response_body bytea,
    response_content_type text,
    created_at timestamptz NOT NULL DEFAULT now(),
    completed_at timestamptz,
    expires_at timestamptz NOT NULL,
    UNIQUE (realm_id, actor_id, operation, idempotency_key)
);
CREATE INDEX ix_idempotency_expiry ON idempotency_keys(expires_at);

CREATE TABLE outbox_events (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    realm_id uuid NOT NULL REFERENCES realms(id) ON DELETE RESTRICT,
    aggregate_type text NOT NULL,
    aggregate_id uuid NOT NULL,
    aggregate_version bigint NOT NULL CHECK (aggregate_version > 0),
    event_type text NOT NULL,
    schema_version integer NOT NULL DEFAULT 1 CHECK (schema_version > 0),
    payload jsonb NOT NULL CHECK (jsonb_typeof(payload) = 'object'),
    occurred_at timestamptz NOT NULL DEFAULT now(),
    available_at timestamptz NOT NULL DEFAULT now(),
    attempts integer NOT NULL DEFAULT 0 CHECK (attempts >= 0),
    published_at timestamptz,
    last_error text,
    UNIQUE (realm_id, aggregate_type, aggregate_id, aggregate_version, event_type)
);
CREATE INDEX ix_outbox_pending ON outbox_events(available_at, occurred_at) WHERE published_at IS NULL;

CREATE TABLE content_releases (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    realm_id uuid NOT NULL REFERENCES realms(id) ON DELETE RESTRICT,
    version text NOT NULL CHECK (length(version) BETWEEN 1 AND 64),
    manifest_sha256 bytea NOT NULL CHECK (octet_length(manifest_sha256) = 32),
    signature bytea NOT NULL,
    signing_key_id text NOT NULL,
    status text NOT NULL DEFAULT 'staged' CHECK (status IN ('staged','active','retired','rejected')),
    created_at timestamptz NOT NULL DEFAULT now(),
    activated_at timestamptz,
    retired_at timestamptz,
    created_by text NOT NULL,
    UNIQUE (realm_id, id),
    UNIQUE (realm_id, version),
    UNIQUE (realm_id, manifest_sha256)
);
CREATE UNIQUE INDEX uq_content_active_release ON content_releases(realm_id) WHERE status = 'active';
ALTER TABLE realms ADD CONSTRAINT fk_realms_active_content
    FOREIGN KEY (id, active_content_release_id) REFERENCES content_releases(realm_id, id) DEFERRABLE INITIALLY DEFERRED;

CREATE TABLE content_artifacts (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    realm_id uuid NOT NULL,
    content_release_id uuid NOT NULL,
    logical_path text NOT NULL CHECK (logical_path !~ '(^/|\.\.)'),
    kind text NOT NULL CHECK (kind IN ('config','lua','map','sprite','audio','binary','localization')),
    media_type text NOT NULL,
    encoding text,
    size_bytes bigint NOT NULL CHECK (size_bytes >= 0),
    sha256 bytea NOT NULL CHECK (octet_length(sha256) = 32),
    object_uri text NOT NULL,
    source_path text,
    source_package text,
    source_uid integer,
    source_encoding text,
    discovery_tool text NOT NULL CHECK (discovery_tool IN ('semble','gitnexus','vltktool','manual','runtime','importer')),
    evidence_note text,
    parser_name text,
    parser_version text,
    normalized_sha256 bytea CHECK (normalized_sha256 IS NULL OR octet_length(normalized_sha256) = 32),
    created_at timestamptz NOT NULL DEFAULT now(),
    FOREIGN KEY (realm_id, content_release_id) REFERENCES content_releases(realm_id, id) ON DELETE RESTRICT,
    UNIQUE (realm_id, id),
    UNIQUE (realm_id, content_release_id, logical_path)
);
CREATE INDEX ix_content_artifact_source ON content_artifacts(realm_id, source_path, source_uid);

CREATE TABLE config_entries (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    realm_id uuid NOT NULL,
    content_release_id uuid NOT NULL,
    source_artifact_id uuid NOT NULL,
    namespace text NOT NULL,
    entry_key text NOT NULL,
    value jsonb NOT NULL,
    value_sha256 bytea NOT NULL CHECK (octet_length(value_sha256) = 32),
    source_line integer CHECK (source_line > 0),
    created_at timestamptz NOT NULL DEFAULT now(),
    FOREIGN KEY (realm_id, content_release_id) REFERENCES content_releases(realm_id, id) ON DELETE RESTRICT,
    FOREIGN KEY (realm_id, source_artifact_id) REFERENCES content_artifacts(realm_id, id) ON DELETE RESTRICT,
    UNIQUE (realm_id, id),
    UNIQUE (realm_id, content_release_id, namespace, entry_key)
);
CREATE INDEX ix_config_entries_lookup ON config_entries(realm_id, content_release_id, namespace, entry_key);

CREATE TABLE lua_modules (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    realm_id uuid NOT NULL,
    module_key text NOT NULL CHECK (module_key ~ '^[A-Za-z0-9_./-]+$'),
    created_at timestamptz NOT NULL DEFAULT now(),
    retired_at timestamptz,
    UNIQUE (realm_id, id),
    UNIQUE (realm_id, module_key)
);

CREATE TABLE lua_module_versions (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    realm_id uuid NOT NULL,
    lua_module_id uuid NOT NULL,
    content_release_id uuid NOT NULL,
    source_artifact_id uuid NOT NULL,
    source_sha256 bytea NOT NULL CHECK (octet_length(source_sha256) = 32),
    bytecode_sha256 bytea CHECK (bytecode_sha256 IS NULL OR octet_length(bytecode_sha256) = 32),
    lua_version text NOT NULL,
    sandbox_policy_version text NOT NULL,
    deterministic boolean NOT NULL DEFAULT false,
    approved_by text NOT NULL,
    approved_at timestamptz NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    FOREIGN KEY (realm_id, lua_module_id) REFERENCES lua_modules(realm_id, id) ON DELETE RESTRICT,
    FOREIGN KEY (realm_id, content_release_id) REFERENCES content_releases(realm_id, id) ON DELETE RESTRICT,
    FOREIGN KEY (realm_id, source_artifact_id) REFERENCES content_artifacts(realm_id, id) ON DELETE RESTRICT,
    UNIQUE (realm_id, id),
    UNIQUE (realm_id, lua_module_id, content_release_id)
);
CREATE INDEX ix_lua_versions_release ON lua_module_versions(realm_id, content_release_id);

CREATE TABLE audit_events (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    realm_id uuid NOT NULL REFERENCES realms(id) ON DELETE RESTRICT,
    actor_type text NOT NULL CHECK (actor_type IN ('account','admin','service','system')),
    actor_id text NOT NULL,
    action text NOT NULL,
    target_type text NOT NULL,
    target_id text NOT NULL,
    request_id uuid,
    trace_id text,
    before_hash bytea,
    after_hash bytea,
    metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
    occurred_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ix_audit_target ON audit_events(realm_id, target_type, target_id, occurred_at DESC);
CREATE INDEX ix_audit_actor ON audit_events(realm_id, actor_type, actor_id, occurred_at DESC);

-- RLS policies are intentionally explicit and fail closed when app.realm_id is absent.
DO $$
DECLARE t text;
BEGIN
  FOREACH t IN ARRAY ARRAY[
    'accounts','auth_sessions','characters','character_stats','character_positions',
    'character_skills','wallets','economy_transactions','economy_entries',
    'inventory_items','runtime_checkpoints','idempotency_keys','outbox_events',
    'content_releases','content_artifacts','config_entries','lua_modules',
    'lua_module_versions','audit_events'
  ] LOOP
    EXECUTE format('ALTER TABLE %I ENABLE ROW LEVEL SECURITY', t);
    EXECUTE format(
      'CREATE POLICY realm_isolation ON %I USING (realm_id = nullif(current_setting(''app.realm_id'', true), '''')::uuid) WITH CHECK (realm_id = nullif(current_setting(''app.realm_id'', true), '''')::uuid)', t
    );
  END LOOP;
END $$;

COMMIT;
