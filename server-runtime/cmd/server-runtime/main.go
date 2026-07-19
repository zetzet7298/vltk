package main

import (
	"log"
	"net/http"
	"os"
	"strconv"

	"vltk.dev/server-runtime/catalog"
	"vltk.dev/server-runtime/service"
)

func main() {
	addr := env("SERVER_RUNTIME_ADDR", ":8080")
	mux := http.NewServeMux()
	mux.HandleFunc("/healthz", service.HealthHandler)
	if dir := os.Getenv("SERVER_RUNTIME_SKILLPORT_DIR"); dir != "" {
		loadOpts := []catalog.LoadOption{catalog.WithLoadMode(catalog.LoadMode(env("SERVER_RUNTIME_CONTENT_MODE", string(catalog.LoadModeProduction))))}
		if keysEnv := os.Getenv("SERVER_RUNTIME_CONTENT_TRUSTED_PUBLIC_KEYS"); keysEnv != "" {
			keys, err := catalog.ParseTrustedPublicKeysCSV(keysEnv)
			if err != nil {
				log.Fatalf("bad SERVER_RUNTIME_CONTENT_TRUSTED_PUBLIC_KEYS: %v", err)
			}
			loadOpts = append(loadOpts, catalog.WithTrustedPublicKeys(keys))
		}
		if os.Getenv("SERVER_RUNTIME_ALLOW_TEST_CONTENT_KEY") == "1" {
			loadOpts = append(loadOpts, catalog.WithDevelopmentTestKey())
		}
		projection, err := catalog.LoadSkillPortProjection(dir, env("SERVER_RUNTIME_CONTENT_RELEASE_ID", ""), loadOpts...)
		if err != nil {
			log.Fatalf("load skillport: %v", err)
		}
		path := env("SERVER_RUNTIME_WSS_PATH", "/game/v1")
		mux.Handle(path, service.GameV1Handler{ActiveContent: projection.Digest, SkillPolicy: projection.Policy, MaxFrameBytes: uint32(envInt("SERVER_RUNTIME_MAX_FRAME_BYTES", service.DefaultMaxFrameBytes)), MaxInputBatch: uint32(envInt("SERVER_RUNTIME_MAX_INPUT_BATCH", service.DefaultMaxInputBatch)), Verifier: service.RejectingTicketVerifier{}})
		log.Printf("server-runtime game.v1 WSS mounted at %s; ticket verifier rejects until auth integration injects real verifier", path)
	}
	log.Printf("server-runtime listening on %s", addr)
	cert, key := os.Getenv("SERVER_RUNTIME_TLS_CERT_FILE"), os.Getenv("SERVER_RUNTIME_TLS_KEY_FILE")
	if cert != "" || key != "" {
		if cert == "" || key == "" {
			log.Fatal("TLS cert/key must be configured together")
		}
		log.Fatal(http.ListenAndServeTLS(addr, cert, key, mux))
	}
	log.Fatal(http.ListenAndServe(addr, mux))
}

func env(key, fallback string) string {
	if value := os.Getenv(key); value != "" {
		return value
	}
	return fallback
}

func envInt(key string, fallback uint32) int {
	value := os.Getenv(key)
	if value == "" {
		return int(fallback)
	}
	n, err := strconv.Atoi(value)
	if err != nil || n <= 0 {
		log.Fatalf("bad %s", key)
	}
	return n
}
