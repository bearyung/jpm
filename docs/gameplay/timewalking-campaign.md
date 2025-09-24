# Timewalking Campaign Concept

Players embody a present-day traveler (“the Chronist”) who tumbles back into the Wanli era. Each 卷 (e.g., 卷之一 = chapters 1–10) assigns the Chronist to a host body drawn from the novel’s dramatis personae. Between 卷, the traveler returns to a liminal hub where skills and select artifacts persist. This model lets us experience multiple arcs without turning former PCs into antagonists.

## Campaign Loop

1. **Session Zero**: pick the lifespan of each 卷 (chapter ranges) and decide whether host selection is manual, random, or a shortlist draw。`Data/volumes.json` 已預先將卷之一（Episode 1, 第 1–10 回）與卷之二（Episode 2, 第 11–20 回）列出，可直接引用或調整；任務欄位細節見 `docs/gameplay/mission-design.md`。
2. **During a 卷**: play the host character as normal. Choices affect the timeline, earn skills, and acquire items.
3. **Recall Phase**: conclude the arc, record permanent gains in the cold wallet, resolve fallout for the timeline, and select the next host.
4. **Next 卷**: load the appropriate chapter bundle, instantiate the new host, and continue.
## Host Selection

- `EligibleHosts` list per chapter/卷 will live in content metadata (e.g., [`chapter-001.json`](../src/JinPingMei.Content/Data/chapters/chapter-001.json) eventually gains `Gameplay.HostOptions`).
- Manual mode: players choose their host from the list.
- Random mode: the system rolls from eligible candidates (optionally weighted by tags such as `martial`, `court`, `merchant`).
- Hybrid mode: the system proposes a shortlist of 2–3 hosts suited to upcoming scenes; players pick one.

## Persistent Skills

- Skills, memories, and meta-knowledge belong to the player, not the host. Example: `Skill: 景陽伏虎記憶` grants advantage on predator checks regardless of the future host.
- Some skills may require resonance (e.g., martial techniques only usable when the host has `Martial >= 2`). Document gating requirements in the skill entry.
- Maintain a `PlayerProfile` record with learned skills, resonance tags, and cold-wallet capacity upgrades.

## Inventory Model

| Slot Type   | Owner  | Default Capacity | Persistence | Examples |
|-------------|--------|------------------|-------------|----------|
| Cold Wallet | Player | 5 slots (upgrade via skills/relics) | Carries between hosts | Soul-bound charms, future-tech tools, map overlays |
| Hot Wallet  | Host   | 10 slots (per-host override)        | Resets per host; items stay with the timeline when you depart | Weapons, clothing, letters, household assets |

Usage rules:

- Moving an item into the cold wallet narratively removes it from the era; use sparingly to avoid paradoxes.
- If the cold wallet is full, extra rewards must remain in the host’s hot wallet or be cached publicly (which the GM can turn into plot hooks).
- Host capacity variations (e.g., `HotCapacity`: 8 for 潘金蓮, 12 for 武松) live in the chapter/faction metadata.

## Metadata Hooks

Future chapter data should expose fields like:

```json
"Gameplay": {
  "EligibleHosts": ["武松", "武大郎", "潘金蓮"],
  "HostSettings": {
    "武松": { "HotCapacity": 12, "Tags": ["martial", "heroic"] },
    "潘金蓮": { "HotCapacity": 8, "Tags": ["court", "seduction"] }
  },
  "RecommendedMode": "manual|random|hybrid"
}
```

The session manager uses these to load the right character sheets and enforce inventory limits.

## Difficulty Options

- **Manual host selection**: strategic continuity; players stick to familiar archetypes.
- **Random host selection**: surprise and higher difficulty; players must adapt to new playstyles and inventories.
- **Hybrid**: maintain thematic relevance while adding tension through limited choice.

## Design Benefits

- Avoids the “former PC becomes an enemy” issue—different arcs use different hosts but the same meta-self.
- Encourages replay: run 卷之二 with a different host to explore alternative outcomes.
- Makes good use of broadened chapter metadata (`Gameplay` additions) without requiring the full automated pipeline.

## Next Steps

1. Extend chapter enrichment (`Gameplay`) to include host options and inventory capacities as we prepare each 卷.
2. Add a `PlayerProfile` model in the runtime to track cold wallet contents, skills, and resonance tags.
3. Prototype the recall hub scene where players spend experience, store items, and review timeline changes.
4. Document how skills interact with host tags (e.g., gating advanced martial arts behind `martial` hosts).

Keep this doc updated as we experiment, so the eventual implementation has a clear spec to follow.
