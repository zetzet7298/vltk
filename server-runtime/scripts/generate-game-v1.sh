#!/usr/bin/env bash
set -euo pipefail

PROTOC_GEN_GO_VERSION="v1.36.10"
CONTRACT_ROOT="${CONTRACT_ROOT:-../harness/specs/jx-pc-mobile-port/contracts}"
GAME_PROTO_ROOT="${GAME_PROTO_ROOT:-${CONTRACT_ROOT}/proto}"
GAME_PROTO_FILE="${GAME_PROTO_FILE:-game/v1/game.proto}"
CONTENT_PROTO_FILE="${CONTENT_PROTO_FILE:-content/v1/skill_catalog.proto}"

GOBIN="$(go env GOPATH)/bin"
go install "google.golang.org/protobuf/cmd/protoc-gen-go@${PROTOC_GEN_GO_VERSION}"
PATH="${GOBIN}:${PATH}" protoc -I "${GAME_PROTO_ROOT}" -I "${CONTRACT_ROOT}" \
  --go_out=. \
  --go_opt=module=vltk.dev/server-runtime \
  --go_opt=Mcontent/v1/skill_catalog.proto=vltk.dev/server-runtime/gen/content/v1 \
  "${GAME_PROTO_ROOT}/${GAME_PROTO_FILE}" \
  "${CONTRACT_ROOT}/${CONTENT_PROTO_FILE}"
