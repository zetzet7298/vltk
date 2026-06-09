# VLTK Mobile Harness/Kanban helper commands
# Usage: make help

SHELL := /usr/bin/env bash
.SHELLFLAGS := -eu -o pipefail -c

BOARD ?= vltk-port-wave1
OLD_BOARD ?= vltk-port
BATCH ?= 10
TASK ?=
PROFILE_RE ?= vltk-(audit|impl)-[0-9]+|vltk-reviewer|vltk-integrator|vltk-test
WATCH_N ?= 10
REASON ?= manual

.PHONY: help board stats list diag logs tail show runs dispatch dryrun dispatch-all \
	watch monitor procs count sys kill-workers pause-auto resume-auto config-check \
	repair-old-db old-stats old-diag boards current clean-zombies

help: ## Show this help
	@awk 'BEGIN {FS = ":.*##"; printf "\nVLTK Kanban helper\n\nUsage:\n  make <target> [BOARD=$(BOARD)] [TASK=t_xxx] [BATCH=10]\n\nTargets:\n"} /^[a-zA-Z0-9_.-]+:.*##/ {printf "  \033[36m%-16s\033[0m %s\n", $$1, $$2}' $(MAKEFILE_LIST)
	@printf "\nExamples:\n"
	@printf "  make monitor\n"
	@printf "  make dispatch BATCH=10\n"
	@printf "  make logs TASK=t_c94eae02\n"
	@printf "  make tail TASK=t_c94eae02\n"
	@printf "  make pause-auto\n\n"

board: ## Print active helper board name
	@echo "BOARD=$(BOARD)"

boards: ## List Kanban boards
	hermes kanban boards list

current: ## Show current Hermes Kanban board
	hermes kanban boards current

stats: ## Show board status counts
	hermes kanban --board "$(BOARD)" stats

list: ## List board tasks
	hermes kanban --board "$(BOARD)" list

diag: ## Show board diagnostics/crashes/stale tasks
	hermes kanban --board "$(BOARD)" diagnostics || true

show: ## Show one task: make show TASK=t_xxx
	@test -n "$(TASK)" || { echo "ERROR: pass TASK=t_xxx"; exit 2; }
	hermes kanban --board "$(BOARD)" show "$(TASK)"

runs: ## Show runs for one task: make runs TASK=t_xxx
	@test -n "$(TASK)" || { echo "ERROR: pass TASK=t_xxx"; exit 2; }
	hermes kanban --board "$(BOARD)" runs "$(TASK)"

logs: ## Show log for one task: make logs TASK=t_xxx
	@test -n "$(TASK)" || { echo "ERROR: pass TASK=t_xxx"; exit 2; }
	hermes kanban --board "$(BOARD)" log "$(TASK)"

tail: ## Tail one task log: make tail TASK=t_xxx
	@test -n "$(TASK)" || { echo "ERROR: pass TASK=t_xxx"; exit 2; }
	hermes kanban --board "$(BOARD)" tail "$(TASK)"

dryrun: ## Dry-run dispatch up to BATCH tasks
	hermes kanban --board "$(BOARD)" dispatch --dry-run --max "$(BATCH)"

dispatch: ## Dispatch up to BATCH tasks
	hermes kanban --board "$(BOARD)" dispatch --max "$(BATCH)"

dispatch-all: ## Dispatch up to 30 tasks
	$(MAKE) dispatch BATCH=30 BOARD="$(BOARD)"

watch: ## Watch status/diagnostics/process/system every WATCH_N seconds
	@watch -n "$(WATCH_N)" 'echo "=== Kanban $(BOARD) ==="; hermes kanban --board "$(BOARD)" stats; echo; echo "=== Diagnostics ==="; hermes kanban --board "$(BOARD)" diagnostics || true; echo; echo "=== Worker processes ==="; ps -eo args | awk "/hermes -p $(PROFILE_RE).*work kanban task/ && !/awk/ {n++} END {print n+0}"; echo; echo "=== System ==="; free -h | sed -n "1,3p"; uptime; df -h / | tail -1'

monitor: ## One-shot status + diagnostics + process count + system
	@echo "=== Kanban $(BOARD) ==="
	@hermes kanban --board "$(BOARD)" stats || true
	@echo
	@echo "=== Diagnostics ==="
	@hermes kanban --board "$(BOARD)" diagnostics || true
	@echo
	@echo "=== Tasks ==="
	@hermes kanban --board "$(BOARD)" list | sed -n '1,80p' || true
	@echo
	@echo "=== Worker process count ==="
	@ps -eo args | awk '/hermes -p $(PROFILE_RE).*work kanban task/ && !/awk/ {n++} END {print n+0}'
	@echo
	@echo "=== System ==="
	@free -h
	@uptime
	@df -h /

procs: ## List active Hermes Kanban worker processes
	@ps -eo pid,ppid,stat,etime,%cpu,%mem,rss,args --sort=-%cpu | grep -E "hermes -p $(PROFILE_RE).*work kanban task" | grep -v grep || true

count: ## Count active Hermes Kanban worker processes
	@ps -eo args | awk '/hermes -p $(PROFILE_RE).*work kanban task/ && !/awk/ {n++} END {print n+0}'

sys: ## Show RAM/load/disk snapshot
	@free -h
	@uptime
	@df -h /

kill-workers: ## Kill active audit worker processes only
	@pids=$$(ps -eo pid,args | awk '/hermes -p $(PROFILE_RE).*work kanban task/ && !/awk/ {print $$1}'); \
	if [ -z "$$pids" ]; then echo "No matching worker processes"; else echo "Killing: $$pids"; kill $$pids || true; sleep 2; kill -9 $$pids 2>/dev/null || true; fi

clean-zombies: ## Show defunct Hermes processes; parent must reap them
	@ps -eo pid,ppid,stat,etime,args | awk '/\[hermes\] <defunct>/ {print}' || true

pause-auto: ## Disable gateway auto-dispatch; use manual make dispatch
	hermes config set kanban.dispatch_in_gateway false

resume-auto: ## Enable gateway auto-dispatch; restart gateway after this if needed
	hermes config set kanban.dispatch_in_gateway true
	@echo "If gateway is already running, restart it: hermes gateway restart"

config-check: ## Check Hermes config
	hermes config check

repair-old-db: ## Try REINDEX repair on OLD_BOARD DB after crash-loop corruption
	@DB="/home/zet/.hermes/kanban/boards/$(OLD_BOARD)/kanban.db"; \
	echo "DB=$$DB"; \
	ls -lh "$$DB" "$$DB".* 2>/dev/null || true; \
	echo "Before:"; sqlite3 "$$DB" 'PRAGMA integrity_check;' || true; \
	echo "REINDEX:"; sqlite3 -cmd '.timeout 30000' "$$DB" 'REINDEX; PRAGMA integrity_check;' || true

old-stats: ## Stats for old/corrupted board, default OLD_BOARD=vltk-port
	hermes kanban --board "$(OLD_BOARD)" stats || true

old-diag: ## Diagnostics for old/corrupted board
	hermes kanban --board "$(OLD_BOARD)" diagnostics || true
