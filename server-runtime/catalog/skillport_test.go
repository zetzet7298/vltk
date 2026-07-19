package catalog

import (
	"errors"
	"os"
	"path/filepath"
	"strings"
	"testing"
)

func TestLoadSkillPortProjectionDevFixtureFailsClosed(t *testing.T) {
	p, err := LoadSkillPortProjection("testdata/skillport", "00000000-0000-4000-8000-000000000242", WithLoadMode(LoadModeDevelopment), WithDevelopmentTestKey())
	if err != nil {
		t.Fatal(err)
	}
	if p.Digest.CatalogUnionSize != SkillPortRows || p.Digest.ManifestSHA256 != "b3a15de386e3abe1ed91e975afaf6606fbf45b4abede6f50ec25c5acfe4590af" || p.Digest.CatalogUnionSHA256 != "2bea3c7669782d36e04902e31b33f2907b7736c48f2ec351ced14a911a36109d" || p.Digest.ClientProjectionSHA256 != "498e2f3d14d352b7924aaebdff17765aee719f968804fd3c96a54f127286a773" {
		t.Fatalf("bad digest: %+v", p.Digest)
	}
	if len(p.Rows) != SkillPortRows || p.Policy.FilesystemFallbackAllowed || p.Policy.RuntimeParityClaimed || p.Policy.PCRuntimeEvidenceStatus != "BLOCKED" || p.Policy.AndroidPhysicalEvidenceStatus != "BLOCKED" {
		t.Fatalf("bad projection/policy: rows=%d policy=%+v", len(p.Rows), p.Policy)
	}
	if _, err := p.RuntimeSkill(4); !errors.Is(err, ErrBlockedSkill) {
		t.Fatalf("want blocked runtime skill, got %v", err)
	}
	if err := p.Release().Validate(); !errors.Is(err, ErrPolicyDenied) {
		t.Fatalf("blocked release must not enter combat, got %v", err)
	}
	if err := p.ValidateActiveDigest(p.Digest); err != nil {
		t.Fatal(err)
	}
	bad := p.Digest
	bad.ClientProjectionSHA256 = strings.Repeat("f", 64)
	if !errors.Is(p.ValidateActiveDigest(bad), ErrContentMismatch) {
		t.Fatalf("want content mismatch")
	}
}

func TestSkillPortJSONShadowMatchesLoadedRows(t *testing.T) {
	p, err := LoadSkillPortProjection("testdata/skillport", "", WithLoadMode(LoadModeDevelopment), WithDevelopmentTestKey())
	if err != nil {
		t.Fatal(err)
	}
	shadow, err := LoadSkillPortJSONShadow("testdata/skillport")
	if err != nil {
		t.Fatal(err)
	}
	if len(shadow) != len(p.Rows) || shadow[4].SkillName != p.Rows[4].SkillName || shadow[4].ExposureState != p.Rows[4].ExposureState {
		t.Fatalf("shadow mismatch: rows=%d/%d", len(shadow), len(p.Rows))
	}
}

func TestLoadSkillPortProjectionProductionRejectsFixtureKey(t *testing.T) {
	key, err := TestOnlySkillPortPublicKey()
	if err != nil {
		t.Fatal(err)
	}
	_, err = LoadSkillPortProjection("testdata/skillport", "", WithTrustedPublicKey(SkillPortTestOnlyKeyID, key))
	if !errors.Is(err, ErrProductionKey) {
		t.Fatalf("want production key rejection, got %v", err)
	}
}

func TestLoadSkillPortProjectionRejectsUntrustedSignature(t *testing.T) {
	_, err := LoadSkillPortProjection("testdata/skillport", "", WithLoadMode(LoadModeDevelopment))
	if !errors.Is(err, ErrProductionKey) {
		t.Fatalf("want explicit test key policy rejection, got %v", err)
	}
}

func TestLoadSkillPortProjectionRejectsTamper(t *testing.T) {
	for _, tc := range []struct {
		name string
		file string
		edit func([]byte) []byte
	}{
		{name: "manifest-self-hash", file: SkillPortManifestFile, edit: func(b []byte) []byte {
			return []byte(strings.Replace(string(b), "skill-port-dev-19700101", "skill-port-dev-19700102", 1))
		}},
		{name: "artifact-sha", file: SkillPortServerPB, edit: func(b []byte) []byte { return append(b, 0) }},
	} {
		t.Run(tc.name, func(t *testing.T) {
			dir := copySkillPortFixture(t)
			path := filepath.Join(dir, tc.file)
			blob, err := os.ReadFile(path)
			if err != nil {
				t.Fatal(err)
			}
			if err := os.WriteFile(path, tc.edit(blob), 0o666); err != nil {
				t.Fatal(err)
			}
			_, err = LoadSkillPortProjection(dir, "", WithLoadMode(LoadModeDevelopment), WithDevelopmentTestKey())
			if !errors.Is(err, ErrBadManifest) {
				t.Fatalf("want bad manifest, got %v", err)
			}
		})
	}
}

func copySkillPortFixture(t *testing.T) string {
	t.Helper()
	dst := t.TempDir()
	entries, err := os.ReadDir("testdata/skillport")
	if err != nil {
		t.Fatal(err)
	}
	for _, entry := range entries {
		if entry.IsDir() {
			continue
		}
		blob, err := os.ReadFile(filepath.Join("testdata/skillport", entry.Name()))
		if err != nil {
			t.Fatal(err)
		}
		if err := os.WriteFile(filepath.Join(dst, entry.Name()), blob, 0o666); err != nil {
			t.Fatal(err)
		}
	}
	return dst
}
