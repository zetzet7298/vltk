package migrations_test

import (
	"os"
	"path/filepath"
	"strings"
	"testing"
)

func TestPostgresMigrationsAreExpandSafeAndOrdered(t *testing.T) {
	files, err := filepath.Glob("*.sql")
	if err != nil {
		t.Fatal(err)
	}
	if len(files) != 4 {
		t.Fatalf("want 4 migrations, got %d", len(files))
	}
	for _, file := range files {
		bodyBytes, err := os.ReadFile(file)
		if err != nil {
			t.Fatal(err)
		}
		body := strings.ToLower(string(bodyBytes))
		if !strings.HasPrefix(body, "begin;") || !strings.Contains(body, "commit;") {
			t.Fatalf("%s must be transactional", file)
		}
		for _, forbidden := range []string{"drop table", "drop column", "alter column", "rename column", "truncate "} {
			if strings.Contains(body, forbidden) {
				t.Fatalf("%s contains contract-unsafe %q", file, forbidden)
			}
		}
		if !strings.Contains(body, "if not exists") {
			t.Fatalf("%s must be idempotent expand migration", file)
		}
	}
}
