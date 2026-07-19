BEGIN;

CREATE TABLE IF NOT EXISTS character_learned_skills (
    character_id text NOT NULL,
    release_id text NOT NULL REFERENCES content_releases(release_id),
    skill_id integer NOT NULL CHECK (skill_id > 0),
    skill_level integer NOT NULL CHECK (skill_level > 0),
    learned_at timestamptz NOT NULL DEFAULT now(),
    migrated_from_release_id text,
    PRIMARY KEY (character_id, release_id, skill_id)
);

CREATE TABLE IF NOT EXISTS skill_refund_ledger (
    refund_id uuid PRIMARY KEY,
    character_id text NOT NULL,
    from_release_id text NOT NULL,
    to_release_id text NOT NULL REFERENCES content_releases(release_id),
    skill_id integer NOT NULL CHECK (skill_id > 0),
    currency text NOT NULL,
    amount bigint NOT NULL CHECK (amount >= 0),
    reason text NOT NULL,
    created_at timestamptz NOT NULL DEFAULT now(),
    applied_at timestamptz,
    UNIQUE (character_id, to_release_id, skill_id, currency, reason)
);

COMMIT;
