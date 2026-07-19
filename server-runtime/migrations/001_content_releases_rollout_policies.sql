BEGIN;

CREATE TABLE IF NOT EXISTS content_releases (
    release_id text PRIMARY KEY,
    content_hash char(64) NOT NULL,
    compiler_version text NOT NULL,
    signed_manifest jsonb NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    activated_at timestamptz,
    retired_at timestamptz,
    CHECK (content_hash ~ '^[a-f0-9]{64}$')
);

CREATE TABLE IF NOT EXISTS rollout_policies (
    policy_id bigserial PRIMARY KEY,
    release_id text NOT NULL REFERENCES content_releases(release_id),
    realm_id text NOT NULL,
    global_mode text NOT NULL CHECK (global_mode IN ('disabled','enabled','audit')),
    skill_mode text NOT NULL CHECK (skill_mode IN ('disabled','enabled','audit')),
    faction_mode text NOT NULL CHECK (faction_mode IN ('disabled','enabled','audit')),
    starts_at timestamptz NOT NULL DEFAULT now(),
    ends_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (release_id, realm_id, starts_at)
);

CREATE INDEX IF NOT EXISTS rollout_policies_realm_active_idx
    ON rollout_policies (realm_id, starts_at DESC)
    WHERE ends_at IS NULL;

COMMIT;
