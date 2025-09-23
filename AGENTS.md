# Repository Guidelines

## Project Structure & Modules
- `src/` holds production code: `JinPingMei.Game` (console host), `JinPingMei.Engine` (gameplay orchestration), `JinPingMei.AI` (LLM integration stubs), `JinPingMei.Content` (lore and data providers).
- `tests/` contains unit-test projects; today only `JinPingMei.Engine.Tests` verifies orchestration seams.
- `data/source-texts/` stores reference materials such as the 《金瓶梅詞話（萬曆本）》 transcript. Treat this as read-only input.

## Architecture & Performance Principles
- First principle: keep the runtime lightweight, fast, and fault tolerant so one host can serve thousands of telnet users with minimal memory and CPU.
- Target a lightweight, event-driven architecture tuned for high concurrency; prefer async/await and `Socket` APIs that minimize allocations.
- Leverage .NET 9 server features (e.g., improved `TcpListener.AcceptTcpClientAsync`, cancellation tokens, `ValueTask`) to keep the telnet host responsive under load.
- Design for graceful degradation: enforce per-session timeouts, guard critical sections, and fail fast on malformed input so one client cannot exhaust system resources.

## Build, Test, and Development Commands
- `dotnet build JinPingMei.sln` compiles every project with the pinned .NET SDK (see `global.json`).
- `dotnet test JinPingMei.sln` runs the xUnit suite and collects coverage via the default runner.
- `dotnet run --project src/JinPingMei.Game` starts the telnet server on `127.0.0.1:2323`; connect via `telnet 127.0.0.1 2323` from another terminal.

## Coding Style & Naming Conventions
- Follow .NET conventions: PascalCase for public types/methods, camelCase for locals and private fields (prefix with `_` only for private fields, e.g., `_director`).
- Indent with four spaces; avoid tabs.
- Keep files UTF-8; Traditional Chinese strings are welcome where relevant but document intent in comments sparingly.

## Testing Guidelines
- xUnit is the primary unit-testing framework; place new suites under `tests/` mirroring the namespace of the code under test.
- Name tests using `MethodUnderTest_Expectation` (e.g., `RenderIntro_ReturnsContent`).
- Practice test-driven development: design the necessary tests for every milestone, task, and sub-task before implementation, then run them after the work to confirm behaviour.
- Ensure tests run via `dotnet test` before submitting changes; add targeted coverage when extending orchestration or AI integration.

## Commit & Pull Request Practices
- Start each task on a dedicated branch; ensure the branch builds and tests cleanly before opening a PR.
- Use concise, imperative subject lines (~50 chars) similar to `Bootstrap solution structure and add source text`.
- Squash obvious fix-up commits locally when possible; each commit should remain buildable.
- PRs should explain the narrative or technical impact, link to discussion issues, and include test evidence (`dotnet test` output or screenshots for console flows).
- Expect a smoke test during review; once it passes, merge the PR back to `main`.

## Planning & Tracking
- Project milestones, task issues, and status updates are managed directly in GitHub (Milestones + Issues).
- Before starting work, assign yourself to the relevant GitHub issue and update checklists as you progress.
- Discuss scope changes or new tasks by commenting on or creating GitHub issues instead of editing local docs.

## Security & Configuration Notes
- Do not commit API keys; load provider tokens via environment variables (`.env` is ignored by default).
- Sensitive source texts remain in `data/`; avoid exporting or publishing them without confirming licensing obligations.
