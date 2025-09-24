# Mission & Episode Design Notes

This document captures the rules for structuring卷 (episode) hubs, chapter missions, and角色專屬義務 so contributors can extend the content set consistently.

## Episode hubs

- Each卷對應一個 hub scene（記錄在 `Data/world.zh-TW.json`）與 `volumes.json` 條目。
- `HubSceneId`：玩家進入該卷時的集散地，供介紹 Mandatory/Optional 任務、調整冷錢包、接收劇情摘要。
- `HostRoster`：建議輪換的宿主。Timewalking 系統可用它來顯示手牌或提供隨機抽選。
- `CompletionRule`：說明離開該卷所需的最低條件，目前約定：
  - 角色必須完成所有 `Category = Story` 且 `HostDirectives[host] = "Mandatory"` 的任務。
  - `Category = Optional` 為支線、`Category = Bonus` 為加分或成就。
- 在世界地圖中，卷一對應 `episode-01-hall`；卷二預留 `episode-02-hall`，可依劇情繼續擴充。

## Chapter JSON expectations

新增欄位：

- `VolumeId`：指向 `volumes.json` 的卷 id。
- `Gameplay.Objectives[*].Category`：`Story`、`Optional`、`Bonus` 之一。
- `Gameplay.Objectives[*].HostDirectives`：字典，key 為可用宿主，value 採以下枚舉：
  - `Mandatory`：該宿主必修，未完成無法結算卷。
  - `Optional`：建議完成，可提供資源或彩蛋。
  - `Unavailable`：無法或不應由該宿主操作（劇情不成立）。
- `Gameplay.Objectives[*].Completion`：條件列表，描述任務如何被視為完成。系統會監聽結構化事件或狀態，當所有條件成立即標記任務完成。條件型別目前定義如下：
  - `Event` – `{"Type": "Event", "Id": "vol1.ch1.oath-sworn"}`；當遊戲內觸發指定事件，即滿足此條件。
  - `StateFlag` – `{"Type": "StateFlag", "Id": "vol1.ch5.poison_plan_state", "AllowedValues": ["neutralized", "evidence_preserved"]}`；用於檢查狀態變數或流程結果。
  - `Inventory` – `{"Type": "Inventory", "Location": "ColdWallet", "MinItems": 1}`；檢查冷/熱錢包或角色背包。
  - 如需新增其他型別（例如 `SkillCheck`、`Relationship`），請在此文件補充格式與語意後再使用。
- `Gameplay.Scenes[*].Hooks`：一系列結構化後續行動，格式為 `{"Type": "UnlockSideQuest", "Payload": {...}, "Description": "…"}`。常見型別：
  - `UnlockSideQuest` – 解鎖支線任務（Payload: `QuestId`）。
  - `UnlockMission` – 啟用同卷額外任務（Payload: `MissionId`）。
  - `GrantStatus` – 賦予或調整角色狀態（Payload: `StatusId`, `Amount`）。
  - `UnlockLore` – 將資料加入圖書/情報庫（Payload: `LoreId`）。
  - `ModifyRelationship` – 變更人物好感（Payload: `Target`, `Delta`）。
  - `GrantReputation` – 影響陣營聲望（Payload: `FactionId`, `Amount`）。
  - `UnlockCraftingRecipe` – 開放製作配方（Payload: `RecipeId`）。
  - `BranchDecision` – 提供多選分支（Payload: `Options` 陣列，含 `OptionId` 與 `Description`）。
  - `DisplayHint` – 展示提示/教學（Payload: `HintId`）。
  - `SetFlag` – 直接設定布林旗標（Payload: `Id`, `Value`）。
  - 其他型別請先在此處說明，以保持後端相容。

> 建議以 `vol{卷}.ch{章}.xxx` 命名事件或旗標，保持可讀且唯一。

其他欄位維持既有定義（Synopsis、Scenes、KeyItems…）。在 `Gameplay.Scenes` 中，可使用下列欄位細化主持人體驗：

- `HostsEligible`：可觸發該場景的宿主陣列。若當前宿主不在其中，故事管理器應跳過此場景。
- `HostParticipation`：字典，說明各宿主在場景中的角色（如 `primary`、`support`、`observer`、`absent`），方便前端或敘事引擎調整描述與可互動性。

## Authoring workflow

1. **定義宿主與 hub**：在 `volumes.json` 更新 `HostRoster`、`HubSceneId`，並確認世界檔已有對應場景。
2. **規劃任務列表**：每章至少一個 `Story` 任務；可加入多個 `Optional`/`Bonus` 任務。
3. **標註宿主義務**：針對 `EligibleHosts` 為每個任務設定 `HostDirectives`。
   - 建議每位宿主有 2–3 項 Mandatory 任務，維持角色差異。
   - 若任務不適合某宿主，標記為 `Unavailable`。
4. **定義 Completion**：為每個任務填入結構化條件。若條件涉及新事件，請在遊戲邏輯中觸發對應 `GameEvent` 或更新狀態旗標。
5. **設計 Hooks**：將場景後續影響轉為結構化 `Hooks`，確保後端能依 `Type`/`Payload` 執行行為。
6. **更新文件與世界**：若新增 hub 或規則，請同步更新本檔、`world.zh-TW.json`、以及 `src/JinPingMei.Content/README.md` 的作者指南。
7. **驗證**：使用 `jq` 或單元測試確保 JSON 仍為合法格式。

## Example (chapter-001)

```json
{
  "Id": "mission-report-hub",
  "Title": "策議堂報到",
  "Category": "Story",
  "Completion": [
    {"Type": "Event", "Id": "vol1.meta.hub-checkin"}
  ],
  "HostDirectives": {
    "武松": "Mandatory",
    "武大郎": "Mandatory",
    "潘金蓮": "Mandatory",
    "王婆": "Mandatory",
    "西門慶": "Mandatory"
  }
}
```

此任務要求任何卷一宿主都必須在卷務司記官處登記，才能解鎖後續章節。

## Runtime guidance

- Episode loader：讀取 `volumes.json`，將玩家傳送到 `HubSceneId`，再根據宿主顯示 Mandatory/Optional 任務。
- Mission state：建議在存檔中以 `(chapterId, missionId)` 作為 key，記錄完成狀態及取得宿主。
- 卷結算：檢查當前宿主的 Mandatory 任務是否全部完成，再允許前往 `volumes.json` 下一卷或開啟時空回廊。
- Completion 條件透過遊戲事件/狀態旗標實現；若採 GM 指令，可暫以 `/mission complete` 方式觸發對應事件以保持紀錄。

保持以上約定，可確保後續作者擴充時彼此對齊，也方便系統層在 PoC 階段快速驗證卷式玩法。

## Runtime example – scene-inn

以下示範 Episode 1、`scene-inn` 的典型呼叫流程，方便系統開發者對照：

1. **Transport/UI**（Telnet/SSH 等）收到玩家輸入，通知遊戲進入望亭酒家。
2. **Scene controller** 依 JSON 判定 `scene-inn` 符合前置條件，載入該場景並播出敘事提示。
3. **Dialogue/命令解析器** 提供互動（例如「保持清醒」與「放任醉酒」選項，或自由輸入）。
4. **Skill resolver** 讀取 `SkillChecks`：
   - 若宿主為武松，使用 `Trait = 醉勇`, `Difficulty = Standard`；
   - 若宿主為武大郎或西門慶，改用對應 trait 與難度；
   - 解析器可透過擲骰、QTE、對話分歧或 GM 判定來決定成功與否。
5. 成功時：
   - Scene controller 觸發 `SuccessEvent = vol1.ch1.inn-toast`，
   - 套用 `OnSuccessFlags`（`vol1.ch1.oath_sworn = true`、`vol1.ch1.wuda_acknowledged_vow = true`），
   - Mission manager 因兩個旗標皆達成，標記 `mission-sworn-oath` 完成。
6. Scene controller 依 `Hooks` 執行後續行動（e.g. `UnlockLore` 新增懸賞情報、`ModifyRelationship` 提升酒保/獵戶好感）。
7. 失敗時則觸發 `FailureEvent = vol1.ch1.inn-toast-fail`，可由引擎決定是否開啟補救流程。

透過此流程，敘事資料只描述「需要什麼」，而遊戲引擎負責「如何」達成，保留靈活性。其他場景可參照同樣的層次分工。***
