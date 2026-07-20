-- PostgreSQL 16 negative contract for game.v1.sql.
-- Prerequisite: load game.v1.sql into an empty disposable database first.
-- Every mutation below must be rejected. The transaction is always rolled back.
\set ON_ERROR_STOP on

BEGIN;

DO $$
BEGIN
  IF current_setting('server_version_num')::integer NOT BETWEEN 160000 AND 169999 THEN
    RAISE EXCEPTION 'game.v1 negative contract requires PostgreSQL 16, got %', version();
  END IF;
  IF to_regclass('public.realms') IS NULL OR to_regclass('public.economy_entries') IS NULL THEN
    RAISE EXCEPTION 'load contracts/sql/game.v1.sql before the negative contract';
  END IF;
END $$;

CREATE FUNCTION pg_temp.expect_failure(case_id text, command text, expected_fragment text)
RETURNS void
LANGUAGE plpgsql
AS $$
DECLARE
  actual_message text;
BEGIN
  BEGIN
    EXECUTE command;
    RAISE EXCEPTION '%: forbidden command unexpectedly succeeded', case_id;
  EXCEPTION WHEN OTHERS THEN
    actual_message := SQLERRM;
    IF position(lower(expected_fragment) IN lower(actual_message)) = 0 THEN
      RAISE EXCEPTION '%: expected error containing %, got %', case_id, expected_fragment, actual_message;
    END IF;
  END;
  RAISE NOTICE 'PASS %', case_id;
END $$;

-- Deterministic fixtures: two realms, two accounts, two characters and two releases.
INSERT INTO realms (id, code, name) VALUES
  ('00000000-0000-0000-0000-000000000001', 'negative-a', 'Negative A'),
  ('00000000-0000-0000-0000-000000000002', 'negative-b', 'Negative B');

INSERT INTO accounts (id, realm_id, account_name, password_hash) VALUES
  ('10000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 'negative_a1', 'not-a-real-hash'),
  ('10000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', 'negative_a2', 'not-a-real-hash');

INSERT INTO characters (id, realm_id, account_id, name, gender, homeland_id, character_slot, series) VALUES
  ('20000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001', 'NegativeOne', 'male', 1, 1, 0),
  ('20000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000002', 'NegativeTwo', 'female', 1, 1, 1);

INSERT INTO content_releases (
  id, realm_id, version, source_snapshot_id, source_root, source_snapshot_sha256,
  catalog_generator_revision, lua_sandbox_policy_version, lua_host_api_whitelist,
  lua_host_api_whitelist_sha256, manifest_sha256, signature, signing_key_id, created_by
) VALUES
  ('30000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 'negative-r1',
   'negative-snapshot-r1', '/negative/r1', decode(repeat('11', 32), 'hex'), 'negative-generator-r1',
   'sandbox-v1', '["game.log"]', decode(repeat('12', 32), 'hex'), decode(repeat('13', 32), 'hex'),
   decode('01', 'hex'), 'negative-key', 'negative-suite'),
  ('30000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', 'negative-r2',
   'negative-snapshot-r2', '/negative/r2', decode(repeat('21', 32), 'hex'), 'negative-generator-r2',
   'sandbox-v1', '["game.log"]', decode(repeat('12', 32), 'hex'), decode(repeat('23', 32), 'hex'),
   decode('02', 'hex'), 'negative-key', 'negative-suite');

-- NEG-SQL-001: a composite FK must reject a row that crosses realm ownership.
SELECT pg_temp.expect_failure(
  'NEG-SQL-001 cross-realm FK',
  $$INSERT INTO character_stats (realm_id, character_id, strength, dexterity, vitality, spirit)
    VALUES ('00000000-0000-0000-0000-000000000002', '20000000-0000-0000-0000-000000000001', 1, 1, 1, 1)$$,
  'foreign key constraint'
);

-- NEG-SQL-002: the mobile bag has exactly slots 0..59; item footprint is one slot.
SELECT pg_temp.expect_failure(
  'NEG-SQL-002 bag slot 60',
  $$INSERT INTO inventory_items (realm_id, character_id, template_id, content_release_id, container, slot, quantity)
    VALUES ('00000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', 1,
      '30000000-0000-0000-0000-000000000001', 'bag', 60, 1)$$,
  'check constraint'
);

-- NEG-SQL-003: a content release cannot select a Lua runtime other than Lua 5.1.
SELECT pg_temp.expect_failure(
  'NEG-SQL-003 Lua version',
  $$INSERT INTO content_releases (
      realm_id, version, source_snapshot_id, source_root, source_snapshot_sha256,
      catalog_generator_revision, lua_runtime, lua_sandbox_policy_version,
      lua_host_api_whitelist, lua_host_api_whitelist_sha256, manifest_sha256,
      signature, signing_key_id, created_by
    ) VALUES (
      '00000000-0000-0000-0000-000000000001', 'negative-lua-5.4', 'negative-snapshot-lua', '/negative/lua',
      decode(repeat('31', 32), 'hex'), 'negative-generator-lua', 'Lua 5.4', 'sandbox-v1', '["game.log"]',
      decode(repeat('32', 32), 'hex'), decode(repeat('33', 32), 'hex'), decode('03', 'hex'), 'negative-key', 'negative-suite'
    )$$,
  'check constraint'
);

INSERT INTO wallets (id, realm_id, owner_type, owner_id, currency_code) VALUES
  ('40000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 'character', '20000000-0000-0000-0000-000000000001', 'GOLD'),
  ('40000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', 'system', '00000000-0000-0000-0000-000000000001', 'GOLD');

INSERT INTO economy_transactions (id, realm_id, operation, idempotency_key) VALUES
  ('50000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', 'negative-balanced', 'negative-balanced-0001'),
  ('50000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', 'negative-unbalanced', 'negative-unbalanced-01');

-- NEG-SQL-004: an entry currency must equal the wallet currency.
SELECT pg_temp.expect_failure(
  'NEG-SQL-004 ledger cross-currency',
  $$INSERT INTO economy_entries (realm_id, transaction_id, wallet_id, currency_code, delta, balance_after, entry_index)
    VALUES ('00000000-0000-0000-0000-000000000001', '50000000-0000-0000-0000-000000000001',
      '40000000-0000-0000-0000-000000000001', 'SILVER', 1, 1, 0)$$,
  'foreign key constraint'
);

INSERT INTO economy_entries (id, realm_id, transaction_id, wallet_id, currency_code, delta, balance_after, entry_index) VALUES
  ('60000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001', '50000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000001', 'GOLD', 10, 10, 0),
  ('60000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000001', '50000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000002', 'GOLD', -10, -10, 1);
UPDATE economy_transactions SET status = 'posted', posted_at = now()
WHERE id = '50000000-0000-0000-0000-000000000001';
SET CONSTRAINTS ck_economy_balanced IMMEDIATE;
SET CONSTRAINTS ck_economy_balanced DEFERRED;

-- NEG-SQL-005: entries cannot be appended after their transaction is posted.
SELECT pg_temp.expect_failure(
  'NEG-SQL-005 entry after posted',
  $$INSERT INTO economy_entries (realm_id, transaction_id, wallet_id, currency_code, delta, balance_after, entry_index)
    VALUES ('00000000-0000-0000-0000-000000000001', '50000000-0000-0000-0000-000000000001',
      '40000000-0000-0000-0000-000000000001', 'GOLD', 1, 11, 2)$$,
  'require a pending transaction'
);

-- NEG-SQL-006: the ledger is append-only for both UPDATE and DELETE.
SELECT pg_temp.expect_failure(
  'NEG-SQL-006a ledger update',
  $$UPDATE economy_entries SET balance_after = 999 WHERE id = '60000000-0000-0000-0000-000000000001'$$,
  'append-only'
);
SELECT pg_temp.expect_failure(
  'NEG-SQL-006b ledger delete',
  $$DELETE FROM economy_entries WHERE id = '60000000-0000-0000-0000-000000000001'$$,
  'append-only'
);

-- NEG-SQL-007: posting a transaction that is not balanced per currency must fail.
INSERT INTO economy_entries (realm_id, transaction_id, wallet_id, currency_code, delta, balance_after, entry_index)
VALUES ('00000000-0000-0000-0000-000000000001', '50000000-0000-0000-0000-000000000002',
  '40000000-0000-0000-0000-000000000001', 'GOLD', 7, 17, 0);
DO $$
DECLARE
  actual_message text;
BEGIN
  BEGIN
    UPDATE economy_transactions SET status = 'posted', posted_at = now()
    WHERE id = '50000000-0000-0000-0000-000000000002';
    SET CONSTRAINTS ck_economy_balanced IMMEDIATE;
    RAISE EXCEPTION 'NEG-SQL-007 unbalanced post: forbidden command unexpectedly succeeded';
  EXCEPTION WHEN OTHERS THEN
    actual_message := SQLERRM;
    IF position('posted economy transaction must balance by currency' IN lower(actual_message)) = 0 THEN
      RAISE EXCEPTION 'NEG-SQL-007: expected balance error, got %', actual_message;
    END IF;
  END;
  SET CONSTRAINTS ck_economy_balanced DEFERRED;
  RAISE NOTICE 'PASS NEG-SQL-007 unbalanced post';
END $$;

-- NEG-SQL-008: two live items cannot occupy one character/container/slot.
INSERT INTO inventory_items (id, realm_id, character_id, template_id, content_release_id, container, slot, quantity)
VALUES ('70000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001',
  '20000000-0000-0000-0000-000000000001', 1, '30000000-0000-0000-0000-000000000001', 'bag', 0, 1);
SELECT pg_temp.expect_failure(
  'NEG-SQL-008 duplicate active slot',
  $$INSERT INTO inventory_items (realm_id, character_id, template_id, content_release_id, container, slot, quantity)
    VALUES ('00000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001', 2,
      '30000000-0000-0000-0000-000000000001', 'bag', 0, 1)$$,
  'duplicate key value violates unique constraint'
);

-- NEG-SQL-009: config provenance and its source artifact must belong to one release.
INSERT INTO content_artifacts (
  id, realm_id, content_release_id, logical_path, kind, media_type, size_bytes, sha256,
  object_uri, source_snapshot_id, source_path, discovery_tool, parser_name, parser_version
) VALUES (
  '80000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001',
  '30000000-0000-0000-0000-000000000001', 'settings/negative.ini', 'config', 'text/plain', 1,
  decode(repeat('41', 32), 'hex'), 's3://negative/negative.ini', 'negative-snapshot-r1',
  'settings/negative.ini', 'manual', 'negative-suite', '1'
);
SELECT pg_temp.expect_failure(
  'NEG-SQL-009 cross-release provenance',
  $$INSERT INTO config_entries (
      realm_id, content_release_id, source_artifact_id, namespace, entry_key, value, value_sha256
    ) VALUES (
      '00000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000002',
      '80000000-0000-0000-0000-000000000001', 'negative', 'cross-release', '1', decode(repeat('42', 32), 'hex')
    )$$,
  'foreign key constraint'
);

-- NEG-SQL-010: an admission account must own both its auth session and character.
INSERT INTO auth_sessions (
  id, realm_id, account_id, refresh_token_id, refresh_token_hash, token_family_id, expires_at
) VALUES (
  '90000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000001',
  '10000000-0000-0000-0000-000000000001', '90000000-0000-0000-0000-000000000002',
  decode(repeat('51', 32), 'hex'), '90000000-0000-0000-0000-000000000003', now() + interval '1 hour'
);
SELECT pg_temp.expect_failure(
  'NEG-SQL-010 admission ownership mismatch',
  $$INSERT INTO admission_tickets (
      realm_id, auth_session_id, account_id, character_id, content_release_id,
      ticket_hash, session_epoch, expires_at
    ) VALUES (
      '00000000-0000-0000-0000-000000000001', '90000000-0000-0000-0000-000000000001',
      '10000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000002',
      '30000000-0000-0000-0000-000000000001', decode(repeat('52', 32), 'hex'), 1, now() + interval '5 minutes'
    )$$,
  'foreign key constraint'
);

ROLLBACK;
