package catalog

import (
	"bytes"
	"crypto/ed25519"
	"crypto/sha256"
	"encoding/base64"
	"encoding/hex"
	"encoding/json"
	"errors"
	"fmt"
	"os"
	"path/filepath"
	"sort"
	"strings"

	"google.golang.org/protobuf/proto"

	contentv1 "vltk.dev/server-runtime/gen/content/v1"
)

const (
	SkillPortRows            = 242
	SkillPortManifestFile    = "manifest.json"
	SkillPortServerPB        = "skill_port.server.pb"
	SkillPortCatalogPB       = "skill_port.catalog.pb"
	SkillPortClientPB        = "skill_port.client.pb"
	SkillPortManifestVersion = 1

	SkillPortTestOnlyKeyID        = "test-only-skill-port-ed25519-fixture-v1"
	SkillPortTestOnlyPublicKeyB64 = "CyyE9b877r0utcwrxl7nzHGJHZC/yBCBc57ga7RCKP8="
)

var (
	ErrBadManifest     = errors.New("catalog: bad skill port manifest")
	ErrBadSignature    = errors.New("catalog: bad skill port signature")
	ErrUntrustedKey    = errors.New("catalog: untrusted skill port signing key")
	ErrBlockedSkill    = errors.New("catalog: skill blocked for runtime")
	ErrUnsupportedRow  = errors.New("catalog: unsupported skill port row")
	ErrUnknownSkill    = errors.New("catalog: unknown skill")
	ErrContentMismatch = errors.New("catalog: content digest mismatch")
	ErrProductionKey   = errors.New("catalog: production content key policy violation")
)

type ContentDigest struct {
	ContentReleaseID       string
	ManifestSHA256         string
	SourceSnapshotID       string
	CatalogUnionSize       uint32
	CatalogUnionSHA256     string
	RuntimeSkillPolicyID   string
	ClientProjectionSHA256 string
}

type RuntimeSkillPolicy struct {
	PolicyID                      string
	CatalogUnionSize              uint32
	CatalogUnionSHA256            string
	FilesystemFallbackAllowed     bool
	RuntimeParityClaimed          bool
	PCRuntimeEvidenceStatus       string
	AndroidPhysicalEvidenceStatus string
	Blockers                      []string
}

type SkillPortProjection struct {
	Digest ContentDigest
	Policy RuntimeSkillPolicy
	Rows   map[uint32]SkillPortRow
}

type SkillPortRow struct {
	SkillID       uint32
	SkillName     string
	ExposureState string
	Blockers      []string
	StaticFields  map[string]int64
	Relations     []SkillPortRelation
}

type SkillPortRelation struct {
	Type          string
	TargetKind    string
	TargetID      uint32
	SourceSkillID uint32
	ProofState    string
	Blockers      []string
}

type LoadMode string

const (
	LoadModeProduction  LoadMode = "production"
	LoadModeDevelopment LoadMode = "dev"
)

type LoadOption func(*loadOptions)

type loadOptions struct {
	Mode             LoadMode
	TrustedPublicKey map[string]ed25519.PublicKey
	AllowTestOnlyKey bool
}

func WithLoadMode(mode LoadMode) LoadOption {
	return func(o *loadOptions) {
		if mode != "" {
			o.Mode = mode
		}
	}
}

func WithTrustedPublicKey(keyID string, key ed25519.PublicKey) LoadOption {
	return func(o *loadOptions) {
		if o.TrustedPublicKey == nil {
			o.TrustedPublicKey = map[string]ed25519.PublicKey{}
		}
		o.TrustedPublicKey[keyID] = append(ed25519.PublicKey(nil), key...)
	}
}

func WithTrustedPublicKeys(keys map[string]ed25519.PublicKey) LoadOption {
	return func(o *loadOptions) {
		if o.TrustedPublicKey == nil {
			o.TrustedPublicKey = map[string]ed25519.PublicKey{}
		}
		for id, key := range keys {
			o.TrustedPublicKey[id] = append(ed25519.PublicKey(nil), key...)
		}
	}
}

func WithDevelopmentTestKey() LoadOption {
	return func(o *loadOptions) {
		o.AllowTestOnlyKey = true
		key, _ := TestOnlySkillPortPublicKey()
		if o.TrustedPublicKey == nil {
			o.TrustedPublicKey = map[string]ed25519.PublicKey{}
		}
		o.TrustedPublicKey[SkillPortTestOnlyKeyID] = key
	}
}

func TestOnlySkillPortPublicKey() (ed25519.PublicKey, error) {
	data, err := base64.StdEncoding.DecodeString(SkillPortTestOnlyPublicKeyB64)
	if err != nil {
		return nil, err
	}
	if len(data) != ed25519.PublicKeySize {
		return nil, fmt.Errorf("%w: test key size", ErrBadSignature)
	}
	return ed25519.PublicKey(data), nil
}

func ParseTrustedPublicKeysCSV(value string) (map[string]ed25519.PublicKey, error) {
	keys := map[string]ed25519.PublicKey{}
	for _, part := range strings.Split(value, ",") {
		part = strings.TrimSpace(part)
		if part == "" {
			continue
		}
		id, b64, ok := strings.Cut(part, "=")
		if !ok || strings.TrimSpace(id) == "" || strings.TrimSpace(b64) == "" {
			return nil, fmt.Errorf("%w: want key_id=base64", ErrUntrustedKey)
		}
		raw, err := base64.StdEncoding.DecodeString(strings.TrimSpace(b64))
		if err != nil || len(raw) != ed25519.PublicKeySize {
			return nil, fmt.Errorf("%w: bad public key %q", ErrUntrustedKey, id)
		}
		keys[strings.TrimSpace(id)] = ed25519.PublicKey(raw)
	}
	return keys, nil
}

func LoadSkillPortProjection(dir, contentReleaseID string, opts ...LoadOption) (SkillPortProjection, error) {
	options := loadOptions{Mode: LoadModeProduction, TrustedPublicKey: map[string]ed25519.PublicKey{}}
	for _, opt := range opts {
		opt(&options)
	}

	manifestBlob, err := os.ReadFile(filepath.Join(dir, SkillPortManifestFile))
	if err != nil {
		return SkillPortProjection{}, err
	}
	manifest, err := verifySkillPortManifest(dir, manifestBlob, options)
	if err != nil {
		return SkillPortProjection{}, err
	}
	if contentReleaseID != "" && manifest.ContentDigest.ContentReleaseID != contentReleaseID {
		return SkillPortProjection{}, fmt.Errorf("%w: release id %q", ErrBadManifest, manifest.ContentDigest.ContentReleaseID)
	}

	serverBlob, err := os.ReadFile(filepath.Join(dir, SkillPortServerPB))
	if err != nil {
		return SkillPortProjection{}, err
	}
	catalogBlob, err := os.ReadFile(filepath.Join(dir, SkillPortCatalogPB))
	if err != nil {
		return SkillPortProjection{}, err
	}

	var server contentv1.ServerSkillCatalog
	if err := proto.Unmarshal(serverBlob, &server); err != nil {
		return SkillPortProjection{}, err
	}
	var full contentv1.SkillCatalog
	if err := proto.Unmarshal(catalogBlob, &full); err != nil {
		return SkillPortProjection{}, err
	}

	policy := manifest.RuntimeSkillPolicy()
	if err := verifyCatalogProjection(manifest, &server, &full, policy); err != nil {
		return SkillPortProjection{}, err
	}

	rows := make(map[uint32]SkillPortRow, len(server.GetRows()))
	ids := make([]int, 0, len(server.GetRows()))
	for _, row := range server.GetRows() {
		if row.GetSkillId() == 0 || rows[row.GetSkillId()].SkillID != 0 {
			return SkillPortProjection{}, fmt.Errorf("%w: duplicate/zero skill id %d", ErrBadManifest, row.GetSkillId())
		}
		mapped := SkillPortRow{SkillID: row.GetSkillId(), SkillName: row.GetSkillName(), ExposureState: exposureState(row.GetExposureState()), Blockers: append([]string(nil), row.GetBlockers()...), StaticFields: map[string]int64{}, Relations: make([]SkillPortRelation, 0, len(row.GetRelations()))}
		for _, field := range row.GetStaticFields() {
			if field.GetNodeKind() == contentv1.NodeKind_NODE_KIND_UNSPECIFIED || field.GetKind() == contentv1.StaticFieldKind_STATIC_FIELD_KIND_UNSPECIFIED {
				mapped.Blockers = append(mapped.Blockers, "unsupported_static_field_node")
				continue
			}
			if _, ok := field.GetValue().(*contentv1.StaticField_IntValue); !ok {
				mapped.Blockers = append(mapped.Blockers, "unsupported_static_field_value")
				continue
			}
			mapped.StaticFields[field.GetName()] = field.GetIntValue()
		}
		for _, rel := range row.GetRelations() {
			mapped.Relations = append(mapped.Relations, SkillPortRelation{Type: lifecycleRelationType(rel.GetType()), TargetKind: rel.GetTargetKind(), TargetID: rel.GetTargetId(), SourceSkillID: rel.GetSourceSkillId(), ProofState: rel.GetProofState(), Blockers: append([]string(nil), rel.GetBlockers()...)})
			if rel.GetProofState() == "missing" || rel.GetType() == contentv1.LifecycleRelationType_LIFECYCLE_RELATION_TYPE_UNSPECIFIED {
				mapped.Blockers = append(mapped.Blockers, "unsupported_lifecycle_relation")
			}
		}
		rows[mapped.SkillID] = mapped
		ids = append(ids, int(mapped.SkillID))
	}
	if got := catalogUnionHash(ids); got != manifest.ContentDigest.CatalogUnionSHA256 {
		return SkillPortProjection{}, fmt.Errorf("%w: catalog union hash", ErrBadManifest)
	}

	return SkillPortProjection{Digest: manifest.Digest(), Policy: policy, Rows: rows}, nil
}

func (p SkillPortProjection) RuntimeSkill(id uint32) (Skill, error) {
	row, ok := p.Rows[id]
	if !ok {
		return Skill{}, ErrUnknownSkill
	}
	if row.ExposureState != "exposed" || len(row.Blockers) != 0 {
		return Skill{}, ErrBlockedSkill
	}
	return Skill{}, ErrUnsupportedRow
}

func (p SkillPortProjection) Release() Release {
	release := Release{ID: p.Digest.ContentReleaseID, Hash: p.Digest.ManifestSHA256, Policy: FeaturePolicy{Global: PolicyDisabled, Skill: PolicyDisabled, Faction: PolicyDisabled}, Skills: map[SkillKey]Skill{}}
	if p.Policy.AllowsRuntimeContent() {
		release.Policy = FeaturePolicy{Global: PolicyEnabled, Skill: PolicyEnabled, Faction: PolicyEnabled}
		for id := range p.Rows {
			if skill, err := p.RuntimeSkill(id); err == nil {
				release.Skills[SkillKey{ID: skill.ID, Level: skill.Level}] = skill
			}
		}
	}
	return release
}

func (p RuntimeSkillPolicy) AllowsRuntimeContent() bool {
	return !p.FilesystemFallbackAllowed && p.RuntimeParityClaimed && p.PCRuntimeEvidenceStatus == "READY" && p.AndroidPhysicalEvidenceStatus == "READY" && len(p.Blockers) == 0
}

func (p SkillPortProjection) ValidateActiveDigest(got ContentDigest) error {
	if got != p.Digest {
		return ErrContentMismatch
	}
	return nil
}

type skillPortManifestV1 struct {
	SchemaVersion uint32                   `json:"schemaVersion"`
	ReleaseID     string                   `json:"releaseId"`
	Version       string                   `json:"version"`
	SigningKeyID  string                   `json:"signingKeyId"`
	Artifacts     []skillPortArtifactV1    `json:"artifacts"`
	ContentDigest skillPortContentDigestV1 `json:"contentDigest"`
	RuntimePolicy skillPortRuntimePolicyV1 `json:"runtimeSkillPolicy"`
	ManifestSHA   string                   `json:"manifestSha256"`
	Signature     string                   `json:"signature"`
}

type skillPortArtifactV1 struct {
	LogicalPath string `json:"logicalPath"`
	SHA256      string `json:"sha256"`
	SizeBytes   int64  `json:"sizeBytes"`
}

type skillPortContentDigestV1 struct {
	ContentReleaseID       string `json:"contentReleaseId"`
	ManifestSHA256         string `json:"manifestSha256"`
	SourceSnapshotID       string `json:"sourceSnapshotId"`
	CatalogUnionSize       uint32 `json:"catalogUnionSize"`
	CatalogUnionSHA256     string `json:"catalogUnionSha256"`
	ClientProjectionSHA256 string `json:"clientProjectionSha256"`
	RuntimeSkillPolicyID   string `json:"runtimeSkillPolicyId"`
}

type skillPortRuntimePolicyV1 struct {
	PolicyID                      string `json:"policyId"`
	CatalogUnionSize              uint32 `json:"catalogUnionSize"`
	CatalogUnionSHA256            string `json:"catalogUnionSha256"`
	FilesystemFallbackAllowed     bool   `json:"filesystemFallbackAllowed"`
	RuntimeParityClaimed          bool   `json:"runtimeParityClaimed"`
	PCRuntimeEvidenceStatus       string `json:"pcRuntimeEvidenceStatus"`
	AndroidPhysicalEvidenceStatus string `json:"androidPhysicalEvidenceStatus"`
}

func verifySkillPortManifest(dir string, blob []byte, opts loadOptions) (skillPortManifestV1, error) {
	var manifest skillPortManifestV1
	if err := json.Unmarshal(blob, &manifest); err != nil {
		return skillPortManifestV1{}, err
	}
	if manifest.SchemaVersion != SkillPortManifestVersion {
		return skillPortManifestV1{}, fmt.Errorf("%w: schemaVersion=%d", ErrBadManifest, manifest.SchemaVersion)
	}
	if manifest.ManifestSHA == "" || manifest.ManifestSHA != manifest.ContentDigest.ManifestSHA256 {
		return skillPortManifestV1{}, fmt.Errorf("%w: manifest sha mismatch", ErrBadManifest)
	}

	var raw map[string]any
	if err := json.Unmarshal(blob, &raw); err != nil {
		return skillPortManifestV1{}, err
	}
	hashPayload := cloneJSONMap(raw)
	delete(hashPayload, "signature")
	delete(hashPayload, "manifestSha256")
	if digest, ok := hashPayload["contentDigest"].(map[string]any); ok {
		delete(digest, "manifestSha256")
	}
	manifestSHA := sha256.Sum256(canonicalJSON(hashPayload))
	if hex.EncodeToString(manifestSHA[:]) != manifest.ManifestSHA {
		return skillPortManifestV1{}, fmt.Errorf("%w: self hash", ErrBadManifest)
	}

	if err := verifyManifestSignature(raw, manifest, opts); err != nil {
		return skillPortManifestV1{}, err
	}
	if err := verifyArtifactEntries(dir, manifest); err != nil {
		return skillPortManifestV1{}, err
	}
	return manifest, nil
}

func verifyManifestSignature(raw map[string]any, manifest skillPortManifestV1, opts loadOptions) error {
	if opts.Mode == "" {
		opts.Mode = LoadModeProduction
	}
	if manifest.SigningKeyID == "" || manifest.Signature == "" {
		return fmt.Errorf("%w: missing key/signature", ErrBadSignature)
	}
	if opts.Mode == LoadModeProduction && isTestOnlySigningKey(manifest.SigningKeyID) {
		return fmt.Errorf("%w: forbidden key %s", ErrProductionKey, manifest.SigningKeyID)
	}
	if opts.Mode != LoadModeProduction && isTestOnlySigningKey(manifest.SigningKeyID) && !opts.AllowTestOnlyKey {
		return fmt.Errorf("%w: test key not explicitly allowed", ErrProductionKey)
	}
	key := opts.TrustedPublicKey[manifest.SigningKeyID]
	if len(key) != ed25519.PublicKeySize {
		return fmt.Errorf("%w: %s", ErrUntrustedKey, manifest.SigningKeyID)
	}
	sig, err := base64.StdEncoding.DecodeString(manifest.Signature)
	if err != nil || len(sig) != ed25519.SignatureSize {
		return fmt.Errorf("%w: bad signature encoding", ErrBadSignature)
	}
	signingPayload := cloneJSONMap(raw)
	delete(signingPayload, "signature")
	if !ed25519.Verify(key, canonicalJSON(signingPayload), sig) {
		return ErrBadSignature
	}
	return nil
}

func verifyArtifactEntries(dir string, manifest skillPortManifestV1) error {
	seen := map[string]skillPortArtifactV1{}
	for _, artifact := range manifest.Artifacts {
		if artifact.LogicalPath == "" || seen[artifact.LogicalPath].LogicalPath != "" {
			return fmt.Errorf("%w: duplicate/empty artifact", ErrBadManifest)
		}
		blob, err := os.ReadFile(filepath.Join(dir, artifact.LogicalPath))
		if err != nil {
			return err
		}
		sum := sha256.Sum256(blob)
		if artifact.SizeBytes != int64(len(blob)) || artifact.SHA256 != hex.EncodeToString(sum[:]) {
			return fmt.Errorf("%w: artifact %s hash/size mismatch", ErrBadManifest, artifact.LogicalPath)
		}
		seen[artifact.LogicalPath] = artifact
	}
	for _, required := range []string{SkillPortServerPB, SkillPortCatalogPB, SkillPortClientPB} {
		if seen[required].LogicalPath == "" {
			return fmt.Errorf("%w: missing %s", ErrBadManifest, required)
		}
	}
	if seen[SkillPortClientPB].SHA256 != manifest.ContentDigest.ClientProjectionSHA256 {
		return fmt.Errorf("%w: client projection digest", ErrBadManifest)
	}
	return nil
}

func verifyCatalogProjection(manifest skillPortManifestV1, server *contentv1.ServerSkillCatalog, full *contentv1.SkillCatalog, policy RuntimeSkillPolicy) error {
	if len(server.GetRows()) != SkillPortRows || len(full.GetRows()) != SkillPortRows {
		return fmt.Errorf("%w: rows server=%d catalog=%d", ErrBadManifest, len(server.GetRows()), len(full.GetRows()))
	}
	if manifest.ContentDigest.CatalogUnionSize != SkillPortRows || manifest.RuntimePolicy.CatalogUnionSize != SkillPortRows {
		return fmt.Errorf("%w: catalog union size", ErrBadManifest)
	}
	if !hashRE.MatchString(manifest.ContentDigest.CatalogUnionSHA256) || manifest.ContentDigest.CatalogUnionSHA256 != manifest.RuntimePolicy.CatalogUnionSHA256 {
		return fmt.Errorf("%w: catalog union hash", ErrBadManifest)
	}
	if server.GetHeader().GetSchemaVersion() != 1 || server.GetHeader().GetProjectionName() != "server" || server.GetHeader().GetCatalogUnionSize() != SkillPortRows || server.GetHeader().GetCatalogUnionSha256() != manifest.ContentDigest.CatalogUnionSHA256 {
		return fmt.Errorf("%w: server header", ErrBadManifest)
	}
	if full.GetHeader().GetSchemaVersion() != 1 || full.GetHeader().GetCatalogUnionSize() != SkillPortRows || full.GetHeader().GetCatalogUnionSha256() != manifest.ContentDigest.CatalogUnionSHA256 || full.GetHeader().GetGoldenReadyCount() != 0 {
		return fmt.Errorf("%w: catalog header", ErrBadManifest)
	}
	sp := server.GetRuntimeSkillPolicy()
	if sp.GetPolicyId() != policy.PolicyID || sp.GetCatalogUnionSize() != policy.CatalogUnionSize || sp.GetCatalogUnionSha256() != policy.CatalogUnionSHA256 || sp.GetFilesystemFallbackAllowed() || sp.GetRuntimeParityClaimed() || sp.GetPcRuntimeEvidenceStatus() != "BLOCKED" || sp.GetAndroidPhysicalEvidenceStatus() != "BLOCKED" {
		return fmt.Errorf("%w: runtime policy", ErrBadManifest)
	}
	return nil
}

func (m skillPortManifestV1) Digest() ContentDigest {
	return ContentDigest{ContentReleaseID: m.ContentDigest.ContentReleaseID, ManifestSHA256: m.ContentDigest.ManifestSHA256, SourceSnapshotID: m.ContentDigest.SourceSnapshotID, CatalogUnionSize: m.ContentDigest.CatalogUnionSize, CatalogUnionSHA256: m.ContentDigest.CatalogUnionSHA256, RuntimeSkillPolicyID: m.ContentDigest.RuntimeSkillPolicyID, ClientProjectionSHA256: m.ContentDigest.ClientProjectionSHA256}
}

func (m skillPortManifestV1) RuntimeSkillPolicy() RuntimeSkillPolicy {
	return RuntimeSkillPolicy{PolicyID: m.RuntimePolicy.PolicyID, CatalogUnionSize: m.RuntimePolicy.CatalogUnionSize, CatalogUnionSHA256: m.RuntimePolicy.CatalogUnionSHA256, FilesystemFallbackAllowed: m.RuntimePolicy.FilesystemFallbackAllowed, RuntimeParityClaimed: m.RuntimePolicy.RuntimeParityClaimed, PCRuntimeEvidenceStatus: m.RuntimePolicy.PCRuntimeEvidenceStatus, AndroidPhysicalEvidenceStatus: m.RuntimePolicy.AndroidPhysicalEvidenceStatus, Blockers: []string{"canonical_KPak_provenance_absent", "runtime_skill_formula_effect_evidence_absent", "android_physical_runtime_evidence_absent"}}
}

func isTestOnlySigningKey(keyID string) bool {
	return keyID == SkillPortTestOnlyKeyID || strings.HasPrefix(keyID, "test-only-")
}

func cloneJSONMap(in map[string]any) map[string]any {
	blob, _ := json.Marshal(in)
	var out map[string]any
	_ = json.Unmarshal(blob, &out)
	return out
}

func canonicalJSON(v any) []byte {
	var buf bytes.Buffer
	enc := json.NewEncoder(&buf)
	enc.SetEscapeHTML(false)
	enc.SetIndent("", "  ")
	_ = enc.Encode(v)
	return buf.Bytes()
}

func catalogUnionHash(ids []int) string {
	sort.Ints(ids)
	payload := map[string]any{"schema": "vltk.skill_port.union/v1", "skill_ids": ids}
	sum := sha256.Sum256(canonicalJSON(payload))
	return hex.EncodeToString(sum[:])
}

func exposureState(s contentv1.ExposureState) string {
	switch s {
	case contentv1.ExposureState_EXPOSURE_STATE_EXPOSED:
		return "exposed"
	case contentv1.ExposureState_EXPOSURE_STATE_PC_ONLY:
		return "pc_only"
	case contentv1.ExposureState_EXPOSURE_STATE_EVIDENCE_PENDING:
		return "evidence_pending"
	default:
		return "unsupported"
	}
}

func lifecycleRelationType(t contentv1.LifecycleRelationType) string {
	switch t {
	case contentv1.LifecycleRelationType_LIFECYCLE_RELATION_TYPE_CHILD:
		return "child"
	case contentv1.LifecycleRelationType_LIFECYCLE_RELATION_TYPE_START:
		return "start"
	case contentv1.LifecycleRelationType_LIFECYCLE_RELATION_TYPE_FLY:
		return "fly"
	case contentv1.LifecycleRelationType_LIFECYCLE_RELATION_TYPE_COLLIDE:
		return "collide"
	case contentv1.LifecycleRelationType_LIFECYCLE_RELATION_TYPE_VANISH:
		return "vanish"
	default:
		return "unsupported"
	}
}
