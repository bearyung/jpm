# JinPingMei AI Game

Experimental text-based narrative experience inspired by the Ming dynasty novel *Jin Ping Mei* (《金瓶梅》, "The Plum in the Golden Vase"). The project blends a classic MUD-style interface with large language models to deliver improvisational storytelling around themes of desire, power, and consequence.

## 語言 / Language
本遊戲的首發版本將以繁體中文敘事與介面為核心，並在後續版本逐步加入其他語言與在地化內容。
The first release focuses on Traditional Chinese narration and interface, with additional languages and localization rolling out in future updates.

## Vision
- Invite players to step inside the world of *Jin Ping Mei* as active participants instead of passive readers.
- Combine authored story arcs with AI improvisation for replayable narrative paths.
- Provide historical and cultural context so modern players can appreciate the source material.
- Explore responsible ways of adapting mature classical literature into interactive media.

## Feature Highlights
- **Interactive story engine**: Pure console application with stateful scenes and commands.
- **LLM-powered narration**: GPT-based models expand on authored prompts, generate character dialogue, and react to player intent.
- **Choice and agency**: Mix of free-form text input and structured options, with consequences tracked over time.
- **Contextual annotations**: Optional footnotes, glossaries, and historical references that ground each scene.
- **Extensible modules**: Add or swap storyteller personas, scenarios, and content packs without breaking the core engine.

## Project Status
This repository currently serves as the design notebook and planning hub for the upcoming .NET implementation. Engine scaffolding, CI, and content packs are still to be added. Early experimentation will focus on prototyping the narrative loop and evaluating AI prompting strategies.

## Technology Stack (Planned)
- **Runtime**: .NET / C# console application for the game engine and orchestration.
- **AI integration**: OpenAI GPT-based APIs (model selection TBD as the project matures).
- **Persistence**: SQLite for local playtesting, with a path to PostgreSQL in shared deployments.
- **Deployment**: Console application that can run locally or be deployed via SSH (using OpenSSH's ForceCommand for remote play).

## High-Level Architecture
1. **Narrative engine**: Manages world state, room descriptions, player inventory, and scripted beats.
2. **AI orchestrator**: Crafts prompts, interprets responses, and blends AI output with deterministic rules.
3. **Content library**: Structured lore, characters, and scene templates inspired by the novel.
4. **Data layer**: Persists sessions, players, and branching history for analysis and replay.

## Getting Started

### Running Locally (Development)

```bash
# clone the repository
git clone https://github.com/your-username/jinpingmei-ai-game.git
cd jinpingmei-ai-game

# install prerequisites
# - .NET SDK 9.0+
# - Access token for the chosen LLM provider (set in .env file)

# run the game in plain console mode
dotnet run --project src/JinPingMei.Game

# run the game with Terminal UI (TUI) mode
dotnet run --project src/JinPingMei.Game -- --tui
# or
dotnet run --project src/JinPingMei.Game -- --terminal-gui

# The game runs directly in your terminal - no network connection needed!
```

### Production Deployment (Optional SSH Access)

For remote/multi-user deployment, the console application can be served over SSH without any code changes:

```bash
# On the server, configure SSH to launch the game
# Edit /etc/ssh/sshd_config:
Match User gameuser
    ForceCommand /usr/bin/dotnet /path/to/JinPingMei.Game.dll
    PasswordAuthentication yes
    PermitTTY yes

# Players connect via SSH:
ssh gameuser@yourserver.com
# The game launches automatically!
```

### Architecture Benefits

This console-first approach provides:
- **Simple development**: Run and debug directly in your IDE (F5 in Visual Studio/Rider)
- **No network complexity**: Game logic is separate from transport layer
- **Flexible deployment**: Can run locally, via SSH, or even in Docker
- **Full terminal support**: Console.ReadLine() and WriteLine() work identically whether local or remote
- **Security**: When using SSH deployment, OpenSSH handles all encryption and authentication

While infrastructure code is being authored, you can use this README as a reference for design goals, contribute narrative ideas, or help shape the engine architecture.

## Roadmap Ideas
- Bootstrap the core game loop with rooms, NPCs, and command parsing.
- Implement prompt templates and safety filters for AI-driven narration.
- Add session persistence, save slots, and analytics hooks.
- Layer in annotations, glossaries, and cultural background modules.
- Package curated scenarios that retell major arcs from the source novel.
- Add terminal capability detection for advanced TUI features (colors, cursor positioning).

## Contributing
Contributions are welcome once the initial scaffold is live. Suggested ways to help today:
- Propose story beats, character sheets, or cultural references in issues.
- Document AI prompting experiments and share findings.
- Sketch interaction flows or UI mockups for the terminal client.
- Discuss ethical considerations for adapting mature material.

Before opening a pull request, please make sure any additions align with the vision outlined above. Coding standards, CI expectations, and content guidelines will be documented as the codebase takes shape.

## Cultural and Content Notes
*Jin Ping Mei* examines adult themes including sexuality, corruption, and power imbalance. The game will handle this material through a critical lens, aiming to surface context, consequences, and historical framing instead of sensationalism. Player safety tools and content filters are planned to let each group tune the experience to their comfort level.

## License
License terms have not yet been selected. All contributions will need to comply with the final license once it is published.

## Acknowledgements
- Readers, translators, and scholars who have kept *Jin Ping Mei* accessible to modern audiences.
- The open-source MUD community for decades of knowledge on interactive fiction engines.
- Researchers exploring human-AI co-creative storytelling.

Stay tuned for development updates and feel free to share ideas via issues or discussions.
