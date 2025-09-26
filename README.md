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

## CLI Design Philosophy
- **Command-Line First**: Embrace the simplicity and power of traditional command-line interfaces
- **No Fixed UI Elements**: Information appears only when requested - no persistent panels or frames cluttering the screen
- **On-Demand Information**: Use commands like `/status`, `/look`, `/help` to get information when needed
- **Clean Text Flow**: Output flows naturally like a traditional terminal, maintaining scroll history
- **Minimal Visual Noise**: Focus on the narrative content, not decorative UI elements
- **Keyboard-Driven**: All interactions through text commands - no mouse required

## Feature Highlights
- **CLI-First Experience**: Pure command-line interface that respects terminal conventions and user expectations
- **Interactive story engine**: Stateful scenes and commands with clean, uncluttered output
- **LLM-powered narration**: GPT-based models expand on authored prompts, generate character dialogue, and react to player intent
- **Choice and agency**: Mix of free-form text input and structured options, with consequences tracked over time
- **Contextual annotations**: Optional footnotes, glossaries, and historical references that ground each scene
- **Extensible modules**: Add or swap storyteller personas, scenarios, and content packs without breaking the core engine
- **Rich Formatting When Needed**: Spectre.Console provides beautiful formatting for status displays and important information

## Project Status
This repository currently serves as the design notebook and planning hub for the upcoming .NET implementation. Engine scaffolding, CI, and content packs are still to be added. Early experimentation will focus on prototyping the narrative loop and evaluating AI prompting strategies.

## Technology Stack (Planned)
- **Runtime**: .NET / C# console application for the game engine and orchestration.
- **UI Framework**: Spectre.Console for rich terminal UI with panels, tables, and formatted text.
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

# run the game
dotnet run --project src/JinPingMei.Game

# The game runs directly in your terminal with a rich CLI interface - no network connection needed!
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

This CLI-first approach provides:
- **User-Friendly CLI**: Clean, uncluttered interface that respects terminal conventions
- **Simple development**: Run and debug directly in your IDE (F5 in Visual Studio/Rider)
- **Rich Formatting**: Spectre.Console provides beautiful formatting only when needed (e.g., `/status` command)
- **No network complexity**: Game logic is separate from transport layer
- **Flexible deployment**: Can run locally, via SSH, or even in Docker
- **Full terminal support**: Works identically whether local or remote
- **Security**: When using SSH deployment, OpenSSH handles all encryption and authentication
- **Accessibility**: Text-based interface works with screen readers and terminal accessibility tools

While infrastructure code is being authored, you can use this README as a reference for design goals, contribute narrative ideas, or help shape the engine architecture.

## Roadmap Ideas
- Bootstrap the core game loop with rooms, NPCs, and command parsing.
- Enhance UI with Spectre.Console components (tables, trees, progress bars for loading).
- Implement prompt templates and safety filters for AI-driven narration.
- Add session persistence, save slots, and analytics hooks.
- Layer in annotations, glossaries, and cultural background modules.
- Package curated scenarios that retell major arcs from the source novel.
- Leverage Spectre.Console's rich formatting for immersive text display.

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
