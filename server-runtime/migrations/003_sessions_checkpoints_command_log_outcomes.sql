BEGIN;

CREATE TABLE IF NOT EXISTS runtime_sessions (
    session_epoch bigint PRIMARY KEY,
    realm_id text NOT NULL,
    character_id text NOT NULL,
    combat_instance_id text NOT NULL,
    content_release_id text NOT NULL REFERENCES content_releases(release_id),
    content_hash char(64) NOT NULL,
    expected_client_seq bigint NOT NULL DEFAULT 1,
    last_processed_client_seq bigint NOT NULL DEFAULT 0,
    last_server_seq bigint NOT NULL DEFAULT 0,
    disconnected_at_tick bigint,
    replaced_by_epoch bigint,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now(),
    CHECK (content_hash ~ '^[a-f0-9]{64}$')
);

CREATE TABLE IF NOT EXISTS combat_checkpoints (
    checkpoint_id bigserial PRIMARY KEY,
    combat_instance_id text NOT NULL,
    content_release_id text NOT NULL REFERENCES content_releases(release_id),
    content_hash char(64) NOT NULL,
    server_tick bigint NOT NULL,
    checksum char(64) NOT NULL,
    payload bytea NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (combat_instance_id, server_tick),
    CHECK (content_hash ~ '^[a-f0-9]{64}$'),
    CHECK (checksum ~ '^[a-f0-9]{64}$')
);

CREATE TABLE IF NOT EXISTS command_log (
    combat_instance_id text NOT NULL,
    session_epoch bigint NOT NULL,
    client_seq bigint NOT NULL,
    command_id text NOT NULL,
    at_tick bigint NOT NULL,
    content_release_id text NOT NULL,
    content_hash char(64) NOT NULL,
    payload bytea NOT NULL,
    received_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (combat_instance_id, session_epoch, client_seq),
    UNIQUE (combat_instance_id, session_epoch, command_id),
    CHECK (content_hash ~ '^[a-f0-9]{64}$')
);

CREATE TABLE IF NOT EXISTS command_outcomes (
    combat_instance_id text NOT NULL,
    command_id text NOT NULL,
    client_seq bigint NOT NULL,
    outcome text NOT NULL CHECK (outcome IN ('scheduled','committed','rejected')),
    code text NOT NULL,
    committed_server_tick bigint NOT NULL,
    durable_revision bigint NOT NULL DEFAULT 0,
    payload bytea NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (combat_instance_id, command_id)
);

CREATE INDEX IF NOT EXISTS command_log_replay_idx
    ON command_log (combat_instance_id, at_tick, client_seq);

CREATE INDEX IF NOT EXISTS combat_checkpoints_latest_idx
    ON combat_checkpoints (combat_instance_id, server_tick DESC);

COMMIT;
