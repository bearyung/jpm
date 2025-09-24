# JinPingMei.Content – Chapter Enrichment Workflow

This project stores hand-authored content bundles. Until the automated pipeline is ready, we enrich each extracted chapter manually so it can act as a gameplay-ready scene packet. Follow the steps below to keep the structure consistent.

## 1. Locate the extracted chapter

Every chapter lives under `Data/chapters/chapter-###.json`. The extractor writes:

```json
{
  "Id": "chapter-001",
  "Number": 1,
  "Titles": [ "景陽岡武松打虎", "潘金蓮嫌夫賣風月" ],
  "Text": "…原文…"
}
```

Do not edit `Id`, `Number`, `Titles`, or `Text`. All gameplay material goes into a new sibling property named `Gameplay`。

另外，每個章節包含 `VolumeId` 用以指向分卷（episode）。分卷資訊集中在 `Data/volumes.json`，可用於一次載入一整卷（例如卷之一涵蓋第 1–10 回）。調整章節所屬卷，只需同步更新該欄位與 `volumes.json` 的 `ChapterIds` 清單。分卷 hub 與任務規則詳見 `docs/gameplay/mission-design.md`。

## 2. Add the gameplay scaffold

Append a `Gameplay` object with the fields below. Copy the schema from `chapter-001.json` as a template:

```json
"Gameplay": {
  "Synopsis": "章節摘要…",
  "PrimaryCharacters": [{"Name": "…", "Role": "…", "Intent": "…"}],
  "SupportingCharacters": [...],
  "Locations": [{"Id": "…", "Name": "…", "Notes": "…"}],
  "EntryState": "…",
  "ExitState": "…",
  "Objectives": [{"Id": "…", "Title": "…", "Description": "…", "Category": "Story|Optional|Bonus", "HostDirectives": {"宿主": "Mandatory|Optional|Unavailable"}, "Completion": [{"Type": "Event", "Id": "…"}]}],
  "KeyItems": [{"Id": "…", "Name": "…", "Usage": "…"}],
  "EligibleHosts": ["…"],
  "HostSettings": {
    "角色名": { "HotCapacity": 10, "Tags": ["martial"], "StartingTraits": ["…"], "Notes": "…" }
  },
  "RecommendedMode": "manual|random|hybrid",
  "Scenes": [{
    "Id": "scene-…",
    "Title": "…",
    "Summary": "…",
    "Beats": [{"Trigger": "…", "Description": "…"}],
    "Hooks": ["…"]
  }],
  "GMNotes": ["…"]
}
```

Write all narrative text in Traditional Chinese. Keep descriptions concise and actionable—treat each field as something a GM or narrative system can plug directly into play.

### Field guidelines

- **Synopsis** – one paragraph explaining the chapter’s conflict and tone.
- **Characters** – separate lead figures (`PrimaryCharacters`) from supporting roles; include intent so designers know how to play them.
- **Locations** – ids should be kebab-case; notes highlight interactive features.
- **Entry/Exit State** – describe world/relationship shifts caused by the chapter.
- **Objectives** – mix main story beats and optional hooks; `Type` is `Main` or `Side`.
- **KeyItems** – anything the player might acquire or spend.
- **EligibleHosts / HostSettings / RecommendedMode** – define which host bodies can anchor the 卷, their inventory limits, trait tags, and whether GM should pick manually, randomly, or via shortlist。
- **Objectives** – `Category` 表示任務類型（Story = 可能成為 Mandatory；Optional、Bonus 為選修/成就）。`HostDirectives` 針對每位宿主標記義務程度：
  - `Mandatory`：該宿主必須完成才能結算本卷。
  - `Optional`：建議完成，常提供資源或彩蛋。
  - `Unavailable`：該宿主不能或不需要處理此任務。
- **Completion** – 列出任務完成所需的事件或狀態條件（如 `Event`、`StateFlag`、`Inventory`）。詳細格式見 `docs/gameplay/mission-design.md`。
- **Scenes** – chunk the chapter into playable moments. For each scene include an id, title, summary, at least one beat (trigger + description), and optional hooks for branching content.
- **GMNotes** – free-form tips, safety reminders, pacing suggestions, etc.

## 3. Validate formatting

After editing, run:

```bash
jq empty Data/chapters/chapter-###.json
```

This ensures the JSON remains valid. (Install `jq` if you don’t have it.)

## 4. Keep the extractor output intact

If you regenerate the chapters with `dotnet run --project ../JinPingMei.ContentPipeline`, your manual `Gameplay` work will be overwritten. Either stash your changes beforehand or copy the enriched files elsewhere before rerunning the extractor.

## 5. Share updates

When you finish a chapter:

1. Commit the updated JSON.
2. Mention the chapter id and notable hooks in your PR/issue update so others know what’s ready.
3. If you adjust the schema (new fields, renamed properties), update this README and `chapter-001.json` so everyone stays aligned.

With this scaffold in place, we can block out gameplay beats quickly while leaving room for a future automated workflow to populate or expand the same fields.
