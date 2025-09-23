# Project Action Plan

This document translates the high-level roadmap into actionable work items. Each milestone targets a coherent deliverable set, emphasizing a lightweight, fault-tolerant telnet experience built atop .NET 9.

## Milestone 1 – Telnet Core Prototype
- **Goal:** Ship a resilient telnet host capable of serving thousands of concurrent connections locally.
- **Duration:** ~2 weeks

### Task 1.1 – Harden Connection Handling
- Subtasks:
  - Expose host/port configuration via `appsettings.json` + environment fallbacks.
  - Implement per-session idle timeout, heartbeat pings, and graceful disconnects.
  - Add structured logging (Console + EventSource) for connect/disconnect/error events.
  - Wrap networking calls with cancellation support and exception guards.
- Deliverables: Configurable host, timeout-tested session lifecycle, log transcripts demonstrating resilience.
- Dependencies: Current `TelnetGameServer`; no external blockers.

### Task 1.2 – Interactive Command Loop
- Subtasks:
  - Introduce a command router (`ICommandHandler`) with pluggable handlers.
  - Support commands: `help`, `name`, `look`, `say`, `quit` (Traditional Chinese copy).
  - Persist minimal session state (player name, current locale, interaction flags).
  - Localize prompts with resource files; default to zh-TW, plan hooks for en-US.
- Deliverables: Manual telnet session script covering all commands; unit tests for router.
- Dependencies: Task 1.1 completion for stable sessions.

### Task 1.3 – Observability & Diagnostics
- Subtasks:
  - Implement `/stats` admin command showing session counts, uptime, error totals.
  - Emit EventCounters for active connections and average command latency.
  - Provide lightweight health probe (e.g., TCP ping or internal self-check command).
- Deliverables: Documented diagnostics commands; scripted verification instructions.
- Dependencies: Task 1.2 router in place.

## Milestone 2 – Narrative Engine Foundations
- **Goal:** Enable stateful exploration of curated locales pulled from source text artifacts.
- **Duration:** ~3 weeks

### Task 2.1 – World & Scene Model
- Subtasks:
  - Define domain types (e.g., `Locale`, `Scene`, `Transition`, `Npc`).
  - Seed initial JSON/YAML describing a starter district with 5+ scenes.
  - Wire `GameSession` to respond to navigation commands (`go`, `examine`).
- Deliverables: Navigable prototype with deterministic narrative responses; serialization tests.
- Dependencies: Milestone 1 tasks.

### Task 2.2 – Content Pipeline
- Subtasks:
  - Write parser to split the 萬曆本 text into chapters and candidate scenes.
  - Tag extracted scenes with metadata (time period, characters, mood) for AI prompts.
  - Create tooling (CLI or notebook) to export curated subsets into `JinPingMei.Content`.
- Deliverables: `tools/` scripts with usage docs; generated content pack checked into repo.
- Dependencies: Access to source text under `data/source-texts/`.

### Task 2.3 – Persistence Layer Prototype
- Subtasks:
  - Introduce SQLite-backed repository for player state and transcripts.
  - Implement migrations using `dotnet-ef` or lightweight schema bootstrap.
  - Add save/resume commands; cover with integration tests.
- Deliverables: Database schema docs, automated tests, manual save/load walkthrough.
- Dependencies: Session state from Task 1.2, world model from Task 2.1.

## Milestone 3 – AI Orchestration
- **Goal:** Blend deterministic scenes with LLM improvisation while maintaining safety.
- **Duration:** ~4 weeks (parallelizable with Milestone 2 where feasible)

### Task 3.1 – Prompting & Provider Abstraction
- Subtasks:
  - Design prompt templates combining scene metadata, player history, and safety rails.
  - Implement `IAiProvider` with mock + OpenAI implementations (key via env var).
  - Handle streaming or chunked responses for telnet output.
- Deliverables: Prompt spec doc, interface tests, example telnet transcript.
- Dependencies: Scene metadata from Task 2.2.

### Task 3.2 – Safety & Moderation Filters
- Subtasks:
  - Integrate content filters (keyword blacklist, sentiment heuristics).
  - Provide fallback narratives when AI output is rejected.
  - Log moderated events for review; ensure PII stripping where needed.
- Deliverables: Safety matrix, automated tests covering filter logic.
- Dependencies: Task 3.1 provider pipeline.

### Task 3.3 – Caching & Throttling
- Subtasks:
  - Cache high-frequency AI prompts/responses keyed by scene context (bounded LRU).
  - Implement per-session rate limits and exponential backoff on provider errors.
  - Expose metrics for cache hit ratios and throttled requests.
- Deliverables: Performance benchmarks, metrics dashboard stub.
- Dependencies: Task 3.1 provider pipeline, Task 1.3 metrics plumbing.

## Milestone 4 – Production Hardening
- **Goal:** Prepare for secure multi-user deployment and ongoing operations.
- **Duration:** ~4 weeks

### Task 4.1 – Security & Access Control
- Subtasks:
  - Evaluate SSH support or TLS termination via reverse proxy.
  - Add optional account system (token-based login, per-user quotas).
  - Implement consent and content warning flow before gameplay starts.
- Deliverables: Security design doc, implemented auth flow, automated smoke tests.
- Dependencies: Stable telnet server from prior milestones.

### Task 4.2 – Performance & Load Testing
- Subtasks:
  - Build load generator (e.g., k6 or custom .NET client) to simulate thousands of telnet clients.
  - Profile CPU/memory; optimize allocations (object pooling, ArrayPool usage).
  - Automate nightly stress tests; capture trends in CI artifacts.
- Deliverables: Load test scripts, profiling reports, documented optimizations.
- Dependencies: Milestone 1 completion, AI integration for realistic scenarios.

### Task 4.3 – Deployment & Operations
- Subtasks:
  - Containerize the service (Alpine-based image) with multi-stage builds.
  - Provide deployment manifests (Docker Compose for dev, Kubernetes helm chart for prod).
  - Configure observability stack (OpenTelemetry exporters, dashboard templates).
- Deliverables: Deployment guide, sample configs, monitoring dashboard references.
- Dependencies: Metrics/logging from Milestone 1 & 3.

## Ongoing Cross-Milestone Activities
- **Documentation:** Update README, AGENTS.md, and in-source XML docs alongside feature work.
- **QA & Reviews:** Enforce code review, expand automated tests, and record manual QA scripts.
- **Localization:** Maintain Traditional Chinese as default; document requirements for future locales.
- **Risk Management:** Track legal/licensing constraints for source text and generated content.

## GitHub Tracking Setup
Use `scripts/github/port_plan.sh` (requires the GitHub CLI `gh`) to create milestones and issues based on this plan.

```
./scripts/github/port_plan.sh bearyung/jpm
```

The script will:
- Ensure milestone labels exist (`milestone-1` … `milestone-4`).
- Create/open milestones with the goals defined above.
- Create issues for each task with subtasks as checklists and link them to the corresponding milestone.
