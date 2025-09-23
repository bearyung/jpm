#!/usr/bin/env bash

set -euo pipefail

if ! command -v gh >/dev/null 2>&1; then
  echo "[error] GitHub CLI (gh) is required. Install from https://cli.github.com/ and authenticate with 'gh auth login'." >&2
  exit 1
fi

REPO=${1:-}
if [[ -z "$REPO" ]]; then
  if gh repo view --json nameWithOwner >/dev/null 2>&1; then
    REPO=$(gh repo view --json nameWithOwner -q .nameWithOwner)
  else
    ORIGIN_URL=$(git config --get remote.origin.url)
    if [[ "$ORIGIN_URL" =~ github.com[:/](.+/.+)(\.git)?$ ]]; then
      REPO=${BASH_REMATCH[1]}
    fi
  fi
fi

if [[ -z "$REPO" ]]; then
  echo "[error] Unable to determine repository. Pass 'owner/name' as the first argument." >&2
  exit 1
fi

echo "Using repository: $REPO"

ensure_milestone() {
  local title="$1"
  local description="$2"
  local due="$3"

  local id
  id=$(gh api repos/"$REPO"/milestones \
    --paginate \
    --jq ".[] | select(.title == \"$title\") | .number" || true)

  if [[ -n "$id" ]]; then
    echo "  Milestone '$title' already exists (#$id)"
    echo "$id"
    return 0
  fi

  local args=(-f "title=$title" -f "state=open")
  if [[ -n "$description" ]]; then
    args+=(-f "description=$description")
  fi
  if [[ -n "$due" ]]; then
    args+=(-f "due_on=$due")
  fi

  id=$(gh api repos/"$REPO"/milestones -X POST "${args[@]}" --jq '.number')
  echo "  Created milestone '$title' (#$id)"
  echo "$id"
}

create_issue() {
  local title="$1"
  local body="$2"
  local milestone="$3"
  local labels="$4"

  if gh issue list --repo "$REPO" --state all --search "in:title \"$title\"" --json number --jq '.[0].number' >/dev/null 2>&1; then
    local existing
    existing=$(gh issue list --repo "$REPO" --state all --search "in:title \"$title\"" --json number --jq '.[0].number')
    if [[ -n "$existing" ]]; then
      echo "  Issue '$title' already exists (#$existing)"
      return 0
    fi
  fi

  local args=(--title "$title" --body "$body" --repo "$REPO" --milestone "$milestone")

  if [[ -n "$labels" ]]; then
    args+=(--label "$labels")
  fi

  gh issue create "${args[@]}"
}

ensure_label() {
  local name="$1"
  local color="$2"
  local description="$3"

  if gh api repos/"$REPO"/labels/"$name" >/dev/null 2>&1; then
    return 0
  fi

  gh label create "$name" --repo "$REPO" --color "$color" --description "$description" >/dev/null
}

ensure_label "milestone-1" "0F62FE" "Milestone 1 – Telnet Core Prototype"
ensure_label "milestone-2" "A56EFF" "Milestone 2 – Narrative Engine Foundations"
ensure_label "milestone-3" "FF832B" "Milestone 3 – AI Orchestration"
ensure_label "milestone-4" "24A148" "Milestone 4 – Production Hardening"

M1_DESCRIPTION="Goal: Ship a resilient telnet host capable of serving thousands of concurrent connections locally."
M2_DESCRIPTION="Goal: Enable stateful exploration of curated locales drawn from source text artifacts."
M3_DESCRIPTION="Goal: Blend deterministic scenes with LLM improvisation while maintaining safety."
M4_DESCRIPTION="Goal: Prepare for secure multi-user deployment and ongoing operations."

ensure_milestone "Milestone 1 – Telnet Core Prototype" "$M1_DESCRIPTION" ""
ensure_milestone "Milestone 2 – Narrative Engine Foundations" "$M2_DESCRIPTION" ""
ensure_milestone "Milestone 3 – AI Orchestration" "$M3_DESCRIPTION" ""
ensure_milestone "Milestone 4 – Production Hardening" "$M4_DESCRIPTION" ""

create_issue "M1 Task 1.1 – Harden Connection Handling" "## Goal
Stabilize telnet connections under high concurrency.

## Subtasks
- [ ] Expose host/port configuration via \`appsettings.json\` with environment overrides.
- [ ] Implement per-session idle timeout, heartbeat pings, and graceful disconnects.
- [ ] Add structured logging for connection lifecycle and error events.
- [ ] Wrap networking calls with cancellation and exception guards.
" "Milestone 1 – Telnet Core Prototype" "milestone-1"

create_issue "M1 Task 1.2 – Interactive Command Loop" "## Goal
Introduce a modular command loop with localized responses.

## Subtasks
- [ ] Add command router abstraction and handler registration.
- [ ] Implement commands: help, name, look, say, quit (Traditional Chinese copy).
- [ ] Persist minimal session state (player name, locale, interaction flags).
- [ ] Localize prompts using resource files with zh-TW default.
" "Milestone 1 – Telnet Core Prototype" "milestone-1"

create_issue "M1 Task 1.3 – Observability & Diagnostics" "## Goal
Expose operational insights for live telnet sessions.

## Subtasks
- [ ] Add admin diagnostics command showing session counts, uptime, error totals.
- [ ] Emit EventCounters for active connections and command latency.
- [ ] Provide lightweight health probe or self-check command.
" "Milestone 1 – Telnet Core Prototype" "milestone-1"

create_issue "M2 Task 2.1 – World & Scene Model" "## Goal
Model locales and deterministic scene traversal.

## Subtasks
- [ ] Define domain types for locales, scenes, transitions, and NPCs.
- [ ] Seed starter district data set (5+ scenes) in structured files.
- [ ] Wire GameSession to support navigation commands (go, examine).
" "Milestone 2 – Narrative Engine Foundations" "milestone-2"

create_issue "M2 Task 2.2 – Content Pipeline" "## Goal
Transform the 萬曆本 transcript into structured content packs.

## Subtasks
- [ ] Parse source text into chapters and candidate scenes with metadata tags.
- [ ] Build tooling to curate and export lore bundles into JinPingMei.Content.
- [ ] Document pipeline usage and regeneration workflow.
" "Milestone 2 – Narrative Engine Foundations" "milestone-2"

create_issue "M2 Task 2.3 – Persistence Layer Prototype" "## Goal
Persist player progress and transcripts.

## Subtasks
- [ ] Introduce SQLite-backed repositories with migration strategy.
- [ ] Implement save/resume commands and integration tests.
- [ ] Document schema and backup considerations.
" "Milestone 2 – Narrative Engine Foundations" "milestone-2"

create_issue "M3 Task 3.1 – Prompting & Provider Abstraction" "## Goal
Define prompt flows and provider interfaces for AI narration.

## Subtasks
- [ ] Design prompt templates that blend scene metadata and player history.
- [ ] Implement IAiProvider with mock and OpenAI-backed versions.
- [ ] Handle streaming/chunked responses for telnet delivery.
" "Milestone 3 – AI Orchestration" "milestone-3"

create_issue "M3 Task 3.2 – Safety & Moderation Filters" "## Goal
Enforce content safety before presenting AI output.

## Subtasks
- [ ] Integrate keyword/sentiment filters for AI responses.
- [ ] Provide deterministic fallbacks when content is rejected.
- [ ] Log moderated events with PII stripping.
" "Milestone 3 – AI Orchestration" "milestone-3"

create_issue "M3 Task 3.3 – Caching & Throttling" "## Goal
Control AI usage costs while maintaining responsiveness.

## Subtasks
- [ ] Cache common prompts/responses using bounded LRU cache.
- [ ] Apply per-session rate limits and error backoff.
- [ ] Surface cache/throttle metrics via diagnostics.
" "Milestone 3 – AI Orchestration" "milestone-3"

create_issue "M4 Task 4.1 – Security & Access Control" "## Goal
Secure external access and consent flows.

## Subtasks
- [ ] Evaluate SSH support or TLS reverse proxy strategy.
- [ ] Add token-based login / quota enforcement.
- [ ] Implement consent and content warning flow.
" "Milestone 4 – Production Hardening" "milestone-4"

create_issue "M4 Task 4.2 – Performance & Load Testing" "## Goal
Validate scalability targets and optimize runtime resource usage.

## Subtasks
- [ ] Build load generator to simulate thousands of telnet clients.
- [ ] Profile CPU/memory and optimize allocations.
- [ ] Automate stress tests and report trends.
" "Milestone 4 – Production Hardening" "milestone-4"

create_issue "M4 Task 4.3 – Deployment & Operations" "## Goal
Prepare containerized deployment and observability tooling.

## Subtasks
- [ ] Containerize the service with minimal base image.
- [ ] Provide deployment manifests (Docker Compose / Kubernetes).
- [ ] Configure observability stack and monitoring dashboards.
" "Milestone 4 – Production Hardening" "milestone-4"

echo "All milestones and issues have been processed."
