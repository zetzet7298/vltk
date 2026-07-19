BEGIN;

CREATE TABLE IF NOT EXISTS sampled_trace_metadata (
    trace_id bigserial PRIMARY KEY,
    combat_instance_id text NOT NULL,
    server_tick bigint NOT NULL,
    content_release_id text NOT NULL REFERENCES content_releases(release_id),
    content_hash char(64) NOT NULL,
    sample_rate_ppm integer NOT NULL CHECK (sample_rate_ppm BETWEEN 0 AND 1000000),
    rng_audit_token bytea,
    event_kind text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    CHECK (content_hash ~ '^[a-f0-9]{64}$')
);

CREATE INDEX IF NOT EXISTS sampled_trace_metadata_instance_tick_idx
    ON sampled_trace_metadata (combat_instance_id, server_tick DESC);

COMMIT;
