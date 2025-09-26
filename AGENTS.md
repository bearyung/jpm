# Repository Guidelines

## Project Structure & Modules
- `src/` holds production code: `JinPingMei.Game` (console application), `JinPingMei.Engine` (gameplay orchestration), `JinPingMei.AI` (LLM integration stubs), `JinPingMei.Content` (lore and data providers).
- `tests/` contains unit-test projects; today only `JinPingMei.Engine.Tests` verifies orchestration seams.
- `data/source-texts/` stores reference materials such as the 《金瓶梅詞話（萬曆本）》 transcript. Treat this as read-only input.

## Architecture & Performance Principles
- First principle: keep the runtime lightweight, fast, and responsive as a console application.
- The game is now a pure console application - no embedded server or network code.
- For multi-user deployment, use SSH with ForceCommand to launch the console app per connection.
- Leverage .NET 9 features (async/await, cancellation tokens, `ValueTask`) for responsive gameplay.
- Design for simplicity: Console.ReadLine() for input, Console.WriteLine() for output.
- Terminal capabilities (colors, cursor control) can be detected and used when available.

## Build, Test, and Development Commands
- `dotnet build JinPingMei.sln` compiles every project with the pinned .NET SDK (see `global.json`).
- `dotnet test JinPingMei.sln` runs the xUnit suite and collects coverage via the default runner.
- `dotnet run --project src/JinPingMei.Game` starts the game directly in your terminal as a console application.
- For debugging: Open in Visual Studio/Rider and press F5 to run with debugger attached.

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
- Mirror any task scope amendments back into the owning GitHub issue immediately (use the `gh` CLI or web UI).
- When a subtask is delivered, tick its checkbox directly on the GitHub issue so status stays in sync.

## Tooling Access
- GitHub CLI (`gh`) is installed and authorized for this repository; prefer it for issue updates, reviews, and PR workflows.

## Security & Configuration Notes
- Do not commit API keys; load provider tokens via environment variables (`.env` is ignored by default).
- Sensitive source texts remain in `data/`; avoid exporting or publishing them without confirming licensing obligations.

## Agent Workflow Notes
- The game is now a console application - no network ports or servers involved.
- For testing, you can pipe input: `echo "/help\n/quit" | dotnet run --project src/JinPingMei.Game`
- Console.ReadKey() is only used for "Press any key to exit" - all game input uses Console.ReadLine().
- When running automated tests, input can be redirected from files or scripts.
