package catalog

import (
	"encoding/json"
	"fmt"
	"os"
	"path/filepath"
)

const SkillPortServerJSONShadow = "skill_port.server.json"

// LoadSkillPortJSONShadow is non-production fixture diagnostics only. Runtime loads protobuf.
func LoadSkillPortJSONShadow(dir string) (map[uint32]SkillPortRow, error) {
	blob, err := os.ReadFile(filepath.Join(dir, SkillPortServerJSONShadow))
	if err != nil {
		return nil, err
	}
	var server skillPortServerJSONShadow
	if err := json.Unmarshal(blob, &server); err != nil {
		return nil, err
	}
	if server.Schema != "vltk.skill_port.server_projection/v1" || len(server.Rows) != SkillPortRows {
		return nil, fmt.Errorf("%w: shadow json schema/rows", ErrBadManifest)
	}
	rows := make(map[uint32]SkillPortRow, len(server.Rows))
	for _, row := range server.Rows {
		if row.SkillID == 0 || rows[row.SkillID].SkillID != 0 {
			return nil, fmt.Errorf("%w: shadow duplicate/zero skill id %d", ErrBadManifest, row.SkillID)
		}
		mapped := SkillPortRow{SkillID: row.SkillID, SkillName: row.SkillName, ExposureState: row.ExposureState, Blockers: append([]string(nil), row.Blockers...), StaticFields: map[string]int64{}, Relations: make([]SkillPortRelation, 0, len(row.Relations))}
		for key, value := range row.StaticFields {
			mapped.StaticFields[key] = value
		}
		for _, rel := range row.Relations {
			mapped.Relations = append(mapped.Relations, SkillPortRelation{Type: rel.Type, TargetKind: rel.TargetKind, TargetID: rel.TargetID, SourceSkillID: rel.SourceSkillID, ProofState: rel.ProofState, Blockers: append([]string(nil), rel.Blockers...)})
		}
		rows[row.SkillID] = mapped
	}
	return rows, nil
}

type skillPortServerJSONShadow struct {
	Rows   []skillPortRowJSONShadow `json:"rows"`
	Schema string                   `json:"schema"`
}

type skillPortRowJSONShadow struct {
	Blockers      []string                      `json:"blockers"`
	ExposureState string                        `json:"exposure_state"`
	Relations     []skillPortRelationJSONShadow `json:"relations"`
	SkillID       uint32                        `json:"skill_id"`
	SkillName     string                        `json:"skill_name"`
	StaticFields  map[string]int64              `json:"static_fields"`
}

type skillPortRelationJSONShadow struct {
	Blockers      []string `json:"blockers"`
	ProofState    string   `json:"proof_state"`
	SourceSkillID uint32   `json:"source_skill_id"`
	TargetID      uint32   `json:"target_id"`
	TargetKind    string   `json:"target_kind"`
	Type          string   `json:"type"`
}
