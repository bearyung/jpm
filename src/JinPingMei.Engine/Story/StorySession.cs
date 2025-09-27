using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace JinPingMei.Engine.Story;

public enum MissionStatus
{
    Locked,
    InProgress,
    Completed
}

public sealed class MissionProgressInfo
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public MissionStatus Status { get; set; }
}

public sealed class StoryProgressData
{
    public string VolumeTitle { get; set; } = string.Empty;
    public string EpisodeLabel { get; set; } = string.Empty;
    public string? HostId { get; set; }
    public int CurrentChapterIndex { get; set; }
    public int CurrentChapterNumber { get; set; }
    public List<string> ChapterTitles { get; set; } = new();
    public int TotalChapters { get; set; }
    public string? CurrentSceneTitle { get; set; }
    public int CurrentSceneIndex { get; set; }
    public int TotalScenesInChapter { get; set; }
    public string? CurrentBeatProgress { get; set; }
    public List<MissionProgressInfo> CurrentChapterMissions { get; set; } = new();
    public int CompletedMissions { get; set; }
    public int TotalMissions { get; set; }
    public int OverallProgress { get; set; }
    public double ChapterProgress { get; set; }
    public double MissionProgress { get; set; }
}

public sealed class StorySession
{
    private readonly VolumeDefinition _volume;
    private readonly IReadOnlyList<ChapterDefinition> _chapters;
    private readonly Dictionary<string, MissionState> _missions;
    private readonly Dictionary<string, HostSettingsDefinition> _hostProfiles;
    private readonly Random _random = new();

    private int _chapterIndex;
    private int _sceneIndex;
    private int _beatIndex;
    private bool _sceneIntroDelivered;

    public StorySession(VolumeDefinition volume, IReadOnlyList<ChapterDefinition> chapters)
    {
        _volume = volume ?? throw new ArgumentNullException(nameof(volume));
        _chapters = chapters ?? throw new ArgumentNullException(nameof(chapters));
        _missions = BuildMissionStates(chapters);
        _hostProfiles = BuildHostProfiles(chapters);
        State = new StoryState(volume.Id);
    }

    public StoryState State { get; }

    public IReadOnlyList<string> HostRoster => _volume.HostRoster;

    public IReadOnlyCollection<MissionState> Missions => _missions.Values;

    public bool HasSelectedHost => !string.IsNullOrWhiteSpace(State.HostId);

    public void SelectHost(string hostId)
    {
        if (string.IsNullOrWhiteSpace(hostId))
        {
            throw new ArgumentException("Host id cannot be empty.", nameof(hostId));
        }

        if (_volume.HostRoster.Count > 0 && !_volume.HostRoster.Contains(hostId, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Host '{hostId}' is not available in volume '{_volume.Id}'.");
        }

        State.HostId = hostId;
        ResetProgress();
    }

    public StoryAdvanceResult Advance()
    {
        if (!HasSelectedHost)
        {
            return StoryAdvanceResult.FromMessage("請先使用 /host 選擇本卷宿主。");
        }

        while (true)
        {
            if (_chapterIndex >= _chapters.Count)
            {
                return StoryAdvanceResult.FromMessage("卷一行動已全部完成。", storyCompleted: true);
            }

            var chapter = _chapters[_chapterIndex];

            if (_sceneIndex >= chapter.Gameplay.Scenes.Count)
            {
                _chapterIndex++;
                _sceneIndex = 0;
                _beatIndex = 0;
                _sceneIntroDelivered = false;
                continue;
            }

            var scene = chapter.Gameplay.Scenes[_sceneIndex];
            if (!IsSceneEligible(scene))
            {
                _sceneIndex++;
                _beatIndex = 0;
                _sceneIntroDelivered = false;
                continue;
            }

            if (!_sceneIntroDelivered)
            {
                _sceneIntroDelivered = true;
                return StoryAdvanceResult.FromMessage($"【{scene.Title}】{scene.Summary}");
            }

            if (scene.Beats.Count == 0)
            {
                var hooksMessage = ApplyHooks(scene);
                _sceneIndex++;
                _beatIndex = 0;
                _sceneIntroDelivered = false;
                EvaluateMissions(out var missionUpdates);

                return new StoryAdvanceResult(CombineMessages(hooksMessage, missionUpdates), false);
            }

            var beat = scene.Beats[_beatIndex];
            var messages = new List<string>
            {
                $"→ {beat.Trigger}",
                beat.Description
            };

            var success = ResolveSkillCheck(scene, beat);
            if (success)
            {
                messages.Add("判定結果：成功。");
                ApplyBeatSuccess(beat);
            }
            else
            {
                messages.Add("判定結果：失敗。");
            }

            EvaluateMissions(out var missionMessages);

            _beatIndex++;
            if (_beatIndex >= scene.Beats.Count)
            {
                var hookMessages = ApplyHooks(scene);
                _sceneIndex++;
                _beatIndex = 0;
                _sceneIntroDelivered = false;
                messages.AddRange(hookMessages);
            }

            messages.AddRange(missionMessages);
            return new StoryAdvanceResult(messages, false);
        }
    }

    public string DescribeStatus()
    {
        var builder = new StringBuilder();
        builder.AppendLine($"卷別：{_volume.Title} ({_volume.EpisodeLabel})");
        builder.AppendLine($"宿主：{State.HostId ?? "未選擇"}");
        builder.AppendLine("任務狀態：");
        foreach (var mission in _missions.Values)
        {
            builder.Append(" - ");
            builder.Append(mission.Definition.Title);
            builder.Append("：");
            builder.AppendLine(mission.IsCompleted ? "已完成" : "進行中");
        }

        if (State.UnlockedLore.Count > 0)
        {
            builder.AppendLine("已獲得情報：" + string.Join("、", State.UnlockedLore));
        }

        if (State.Statuses.Count > 0)
        {
            builder.AppendLine("當前狀態：" + string.Join("、", State.Statuses));
        }

        return builder.ToString();
    }

    public string DescribeProgress()
    {
        var builder = new StringBuilder();

        // Episode and Volume Info
        builder.AppendLine($"【當前進度】");
        builder.AppendLine($"卷別：{_volume.Title} ({_volume.EpisodeLabel})");
        builder.AppendLine($"宿主：{State.HostId ?? "未選擇"}");
        builder.AppendLine();

        // Chapter Progress
        if (_chapterIndex < _chapters.Count)
        {
            var currentChapter = _chapters[_chapterIndex];
            builder.AppendLine($"【章節進度】");
            builder.AppendLine($"當前章節：第{currentChapter.Number}回");
            if (currentChapter.Titles.Count > 0)
            {
                builder.AppendLine($"章節標題：{string.Join(" / ", currentChapter.Titles)}");
            }
            builder.AppendLine();

            // Scene Progress
            if (_sceneIndex < currentChapter.Gameplay.Scenes.Count)
            {
                var currentScene = currentChapter.Gameplay.Scenes[_sceneIndex];
                builder.AppendLine($"【場景進度】");
                builder.AppendLine($"當前場景：{currentScene.Title} ({_sceneIndex + 1}/{currentChapter.Gameplay.Scenes.Count})");

                if (currentScene.Beats.Count > 0)
                {
                    builder.AppendLine($"節拍進度：{_beatIndex + 1}/{currentScene.Beats.Count}");
                }
                builder.AppendLine();
            }

            // Mission Progress for Current Chapter
            if (currentChapter.Gameplay.Objectives.Count > 0)
            {
                builder.AppendLine($"【本章任務】");
                foreach (var objective in currentChapter.Gameplay.Objectives)
                {
                    if (_missions.TryGetValue(objective.Id, out var mission))
                    {
                        string status;
                        if (!mission.IsUnlocked)
                        {
                            status = "🔒 未解鎖";
                        }
                        else if (mission.IsCompleted)
                        {
                            status = "✓ 已完成";
                        }
                        else
                        {
                            status = "○ 進行中";
                        }

                        builder.AppendLine($"{status} {objective.Title}");
                        if (mission.IsUnlocked && !mission.IsCompleted && !string.IsNullOrWhiteSpace(objective.Description))
                        {
                            builder.AppendLine($"   {objective.Description}");
                        }
                    }
                }
                builder.AppendLine();
            }
        }

        // Overall Mission Statistics
        var completedMissions = _missions.Values.Count(m => m.IsCompleted);
        var totalMissions = _missions.Count;
        builder.AppendLine($"【總體進度】");
        builder.AppendLine($"章節進度：第{_chapterIndex + 1}章 / 共{_chapters.Count}章");
        builder.AppendLine($"任務完成：{completedMissions}/{totalMissions}");

        // Calculate percentage
        if (totalMissions > 0)
        {
            var percentage = (completedMissions * 100) / totalMissions;
            builder.AppendLine($"完成度：{percentage}%");
        }

        return builder.ToString();
    }

    public StoryProgressData GetProgressData()
    {
        var data = new StoryProgressData
        {
            VolumeTitle = _volume.Title,
            EpisodeLabel = _volume.EpisodeLabel,
            HostId = State.HostId,
            CurrentChapterIndex = _chapterIndex,
            TotalChapters = _chapters.Count
        };

        // Current chapter details
        if (_chapterIndex < _chapters.Count)
        {
            var currentChapter = _chapters[_chapterIndex];
            data.CurrentChapterNumber = currentChapter.Number;
            data.ChapterTitles = currentChapter.Titles.ToList();
            data.TotalScenesInChapter = currentChapter.Gameplay.Scenes.Count;

            // Current scene details
            if (_sceneIndex < currentChapter.Gameplay.Scenes.Count)
            {
                var currentScene = currentChapter.Gameplay.Scenes[_sceneIndex];
                data.CurrentSceneTitle = currentScene.Title;
                data.CurrentSceneIndex = _sceneIndex;

                if (currentScene.Beats.Count > 0)
                {
                    data.CurrentBeatProgress = $"{_beatIndex + 1}/{currentScene.Beats.Count}";
                }
            }

            // Chapter missions
            foreach (var objective in currentChapter.Gameplay.Objectives)
            {
                if (_missions.TryGetValue(objective.Id, out var mission))
                {
                    var missionInfo = new MissionProgressInfo
                    {
                        Id = objective.Id,
                        Title = objective.Title,
                        Description = objective.Description
                    };

                    if (!mission.IsUnlocked)
                    {
                        missionInfo.Status = MissionStatus.Locked;
                    }
                    else if (mission.IsCompleted)
                    {
                        missionInfo.Status = MissionStatus.Completed;
                    }
                    else
                    {
                        missionInfo.Status = MissionStatus.InProgress;
                    }

                    data.CurrentChapterMissions.Add(missionInfo);
                }
            }
        }

        // Overall statistics
        data.CompletedMissions = _missions.Values.Count(m => m.IsCompleted);
        data.TotalMissions = _missions.Count;

        if (data.TotalMissions > 0)
        {
            data.OverallProgress = (data.CompletedMissions * 100) / data.TotalMissions;
            data.MissionProgress = (double)data.CompletedMissions / data.TotalMissions * 100;
        }

        if (data.TotalChapters > 0)
        {
            data.ChapterProgress = ((double)_chapterIndex / data.TotalChapters) * 100;
        }

        return data;
    }

    private void ResetProgress()
    {
        _chapterIndex = 0;
        _sceneIndex = 0;
        _beatIndex = 0;
        _sceneIntroDelivered = false;
    }

    private bool IsSceneEligible(ChapterSceneDefinition scene)
    {
        if (scene.HostsEligible is null || scene.HostsEligible.Count == 0)
        {
            return true;
        }

        if (!HasSelectedHost)
        {
            return false;
        }

        return scene.HostsEligible.Contains(State.HostId!, StringComparer.OrdinalIgnoreCase);
    }

    private bool ResolveSkillCheck(ChapterSceneDefinition scene, SceneBeatDefinition beat)
    {
        if (scene.SkillChecks is null || scene.SkillChecks.Count == 0 || !HasSelectedHost)
        {
            return true;
        }

        var hostId = State.HostId!;
        var check = scene.SkillChecks.FirstOrDefault(s => string.Equals(s.Host, hostId, StringComparison.OrdinalIgnoreCase));
        if (check is null)
        {
            return true;
        }

        var profile = TryGetHostProfile(hostId);
        var hasTrait = profile?.StartingTraits?.Contains(check.Trait, StringComparer.OrdinalIgnoreCase) ?? false;

        var success = hasTrait;
        if (!success)
        {
            success = check.Difficulty.ToLowerInvariant() switch
            {
                "easy" => _random.NextDouble() >= 0.25,
                "standard" => _random.NextDouble() >= 0.5,
                "hard" => _random.NextDouble() >= 0.75,
                _ => false
            };
        }

        if (success)
        {
            if (!string.IsNullOrWhiteSpace(check.SuccessEvent))
            {
                State.SetFlag($"event.{check.SuccessEvent}", true);
            }

            if (check.SuccessFlags is not null)
            {
                foreach (var kvp in check.SuccessFlags)
                {
                    State.SetFlag(kvp.Key, kvp.Value);
                }
            }
        }
        else if (!string.IsNullOrWhiteSpace(check.FailureEvent))
        {
            State.SetFlag($"event.{check.FailureEvent}", true);
        }

        return success;
    }

    private void ApplyBeatSuccess(SceneBeatDefinition beat)
    {
        if (!string.IsNullOrWhiteSpace(beat.EventId))
        {
            State.SetFlag($"event.{beat.EventId}", true);
        }

        if (beat.OnSuccessFlags is null)
        {
            return;
        }

        foreach (var kvp in beat.OnSuccessFlags)
        {
            State.SetFlag(kvp.Key, kvp.Value);
        }
    }

    private IReadOnlyList<string> ApplyHooks(ChapterSceneDefinition scene)
    {
        if (scene.Hooks is null || scene.Hooks.Count == 0)
        {
            return Array.Empty<string>();
        }

        var messages = new List<string>();
        foreach (var hook in scene.Hooks)
        {
            var message = RunHook(hook);
            if (!string.IsNullOrWhiteSpace(message))
            {
                messages.Add(message!);
            }
        }

        return messages;
    }

    private string? RunHook(SceneHookDefinition hook)
    {
        var type = hook.Type?.ToLowerInvariant() ?? string.Empty;

        switch (type)
        {
            case "setflag":
            {
                if (hook.Payload is null)
                {
                    break;
                }

                var payload = hook.Payload.Value;
                if (payload.TryGetProperty("Id", out var idElement) && idElement.ValueKind == JsonValueKind.String)
                {
                    var id = idElement.GetString()!;
                    object value = true;
                    if (payload.TryGetProperty("Value", out var valueElement))
                    {
                        value = valueElement.ValueKind switch
                        {
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.Number => valueElement.TryGetInt64(out var longValue) ? longValue : valueElement.GetDouble(),
                            JsonValueKind.String => valueElement.GetString()!,
                            _ => true
                        };
                    }

                    State.SetFlag(id, value);
                    return $"狀態更新：{id} = {value}";
                }
                break;
            }
            case "unlocklore":
            {
                if (hook.Payload is null)
                {
                    break;
                }

                var payload = hook.Payload.Value;
                if (payload.TryGetProperty("LoreId", out var loreElement) && loreElement.ValueKind == JsonValueKind.String)
                {
                    var loreId = loreElement.GetString()!;
                    State.UnlockLore(loreId);
                    return $"獲得情報：{loreId}";
                }
                break;
            }
            case "addinventory":
            {
                if (hook.Payload is null)
                {
                    break;
                }

                var payload = hook.Payload.Value;
                if (payload.TryGetProperty("Location", out var locationElement) && locationElement.ValueKind == JsonValueKind.String)
                {
                    var location = locationElement.GetString()!;
                    if (payload.TryGetProperty("Items", out var itemsElement) && itemsElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in itemsElement.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String)
                            {
                                State.AddInventoryItem(location, item.GetString()!);
                            }
                        }

                        return $"更新物資：{location} 已存入 {itemsElement.GetArrayLength()} 件物品";
                    }
                }
                break;
            }
            case "modifyrelationship":
            {
                if (hook.Payload is null)
                {
                    break;
                }

                var payload = hook.Payload.Value;
                if (payload.TryGetProperty("Target", out var targetElement) &&
                    payload.TryGetProperty("Delta", out var deltaElement) &&
                    deltaElement.TryGetInt32(out var delta))
                {
                    if (targetElement.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in targetElement.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String)
                            {
                                State.ModifyRelationship(item.GetString()!, delta);
                            }
                        }
                    }
                    else if (targetElement.ValueKind == JsonValueKind.String)
                    {
                        State.ModifyRelationship(targetElement.GetString()!, delta);
                    }

                    return $"人際變動：影響 {delta}";
                }
                break;
            }
            case "grantreputation":
            {
                if (hook.Payload is null)
                {
                    break;
                }

                var payload = hook.Payload.Value;
                if (payload.TryGetProperty("FactionId", out var factionElement) &&
                    payload.TryGetProperty("Amount", out var amountElement) &&
                    amountElement.TryGetInt32(out var amount))
                {
                    var factionId = factionElement.GetString() ?? string.Empty;
                    State.ModifyReputation(factionId, amount);
                    return $"聲望更新：{factionId} {(amount >= 0 ? "+" : string.Empty)}{amount}";
                }
                break;
            }
            case "grantstatus":
            {
                if (hook.Payload is null)
                {
                    break;
                }

                var payload = hook.Payload.Value;
                if (payload.TryGetProperty("StatusId", out var statusElement) && statusElement.ValueKind == JsonValueKind.String)
                {
                    State.GrantStatus(statusElement.GetString()!);
                    return $"獲得狀態：{statusElement.GetString()}";
                }
                break;
            }
            case "unlocksidequest":
            {
                if (hook.Payload is null)
                {
                    break;
                }

                var payload = hook.Payload.Value;
                if (payload.TryGetProperty("QuestId", out var questElement) && questElement.ValueKind == JsonValueKind.String)
                {
                    var questId = questElement.GetString()!;
                    State.UnlockSideQuest(questId);
                    // Also unlock the corresponding mission state
                    if (_missions.TryGetValue(questId, out var mission))
                    {
                        mission.Unlock();
                        return $"取得支線：{mission.Definition.Title}";
                    }
                    return $"取得支線：{questId}";
                }
                break;
            }
            case "unlockmission":
            {
                if (hook.Payload is null)
                {
                    break;
                }

                var payload = hook.Payload.Value;
                if (payload.TryGetProperty("MissionId", out var missionElement) && missionElement.ValueKind == JsonValueKind.String)
                {
                    var missionId = missionElement.GetString()!;
                    State.UnlockMission(missionId);
                    // Also unlock the corresponding mission state
                    if (_missions.TryGetValue(missionId, out var mission))
                    {
                        mission.Unlock();
                        return $"解鎖任務：{mission.Definition.Title}";
                    }
                    return $"解鎖任務：{missionId}";
                }
                break;
            }
            case "unlockcraftingrecipe":
            {
                if (hook.Payload is null)
                {
                    break;
                }

                var payload = hook.Payload.Value;
                if (payload.TryGetProperty("RecipeId", out var recipeElement) && recipeElement.ValueKind == JsonValueKind.String)
                {
                    State.UnlockRecipe(recipeElement.GetString()!);
                    return $"習得配方：{recipeElement.GetString()}";
                }
                break;
            }
            case "displayhint":
            {
                if (hook.Payload is null)
                {
                    break;
                }

                var payload = hook.Payload.Value;
                if (payload.TryGetProperty("HintId", out var hintElement) && hintElement.ValueKind == JsonValueKind.String)
                {
                    return $"提示：{hintElement.GetString()}";
                }
                break;
            }
            case "branchdecision":
            {
                if (hook.Payload is null)
                {
                    break;
                }

                var payload = hook.Payload.Value;
                if (payload.TryGetProperty("Options", out var optionsElement) && optionsElement.ValueKind == JsonValueKind.Array)
                {
                    var options = optionsElement.EnumerateArray()
                        .Where(o => o.ValueKind == JsonValueKind.Object && o.TryGetProperty("Description", out _))
                        .Select(o => o.GetProperty("Description").GetString())
                        .Where(desc => !string.IsNullOrWhiteSpace(desc))
                        .ToArray();

                    if (options.Length > 0)
                    {
                        return "可選分支：" + string.Join(" / ", options);
                    }
                }
                break;
            }
            case "legacynote":
            case "note":
                return !string.IsNullOrWhiteSpace(hook.Description) ? hook.Description : null;
        }

        return !string.IsNullOrWhiteSpace(hook.Description) ? hook.Description : null;
    }

    private void EvaluateMissions(out IReadOnlyList<string> messages)
    {
        var outputMessages = new List<string>();

        foreach (var mission in _missions.Values)
        {
            // First check if locked missions should be unlocked
            if (!mission.IsUnlocked && mission.Definition.Availability?.UnlockConditions?.Count > 0)
            {
                if (CheckUnlockConditions(mission.Definition.Availability.UnlockConditions))
                {
                    mission.Unlock();
                    outputMessages.Add($"解鎖任務：{mission.Definition.Title}");
                }
            }

            // Then check if unlocked missions are completed
            if (mission.IsUnlocked && !mission.IsCompleted)
            {
                if (CheckMissionCompletion(mission.Definition))
                {
                    mission.MarkComplete();
                    outputMessages.Add($"任務完成：{mission.Definition.Title}");
                }
            }
        }

        messages = outputMessages;
    }

    private bool CheckMissionCompletion(ObjectiveDefinition objective)
    {
        return CheckConditions(objective.Completion);
    }

    private bool CheckUnlockConditions(IReadOnlyList<ObjectiveCompletionCondition> conditions)
    {
        return CheckConditions(conditions);
    }

    private bool CheckConditions(IReadOnlyList<ObjectiveCompletionCondition> conditions)
    {
        foreach (var condition in conditions)
        {
            switch (condition.Type.ToLowerInvariant())
            {
                case "stateflag":
                    if (!EvaluateFlagCondition(condition))
                    {
                        return false;
                    }
                    break;
                case "inventory":
                    if (!EvaluateInventoryCondition(condition))
                    {
                        return false;
                    }
                    break;
                case "mission":
                    if (!EvaluateMissionCondition(condition))
                    {
                        return false;
                    }
                    break;
                default:
                    return false;
            }
        }

        return true;
    }

    private bool EvaluateFlagCondition(ObjectiveCompletionCondition condition)
    {
        if (string.IsNullOrWhiteSpace(condition.Id))
        {
            return false;
        }

        var value = State.GetFlagValue(condition.Id);
        if (condition.AllowedValues is { Count: > 0 })
        {
            if (value is string strValue)
            {
                return condition.AllowedValues.Contains(strValue, StringComparer.OrdinalIgnoreCase);
            }

            return false;
        }

        if (condition.Value is { } jsonValue)
        {
            var expected = jsonValue.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number => jsonValue.TryGetInt64(out var longValue) ? (object)longValue : jsonValue.GetDouble(),
                JsonValueKind.String => jsonValue.GetString()!,
                _ => jsonValue.GetRawText()
            };

            return Equals(expected, value);
        }

        if (!string.IsNullOrWhiteSpace(condition.Comparison))
        {
            var comparison = condition.Comparison.ToLowerInvariant();
            if (value is IComparable comparable && condition.Value is { } compareTo)
            {
                object? operand = compareTo.ValueKind switch
                {
                    JsonValueKind.Number => compareTo.TryGetInt64(out var longValue) ? (object)longValue : compareTo.GetDouble(),
                    JsonValueKind.String => (object?)compareTo.GetString(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => compareTo.GetRawText()
                };

                var result = ComparableCompare(comparable, operand);
                return comparison switch
                {
                    "==" => result == 0,
                    "!=" => result != 0,
                    ">" => result > 0,
                    ">=" => result >= 0,
                    "<" => result < 0,
                    "<=" => result <= 0,
                    _ => false
                };
            }
        }

        return State.GetFlag(condition.Id);
    }

    private static int ComparableCompare(IComparable value, object? operand)
    {
        if (operand is null)
        {
            return value.CompareTo(null);
        }

        if (operand is double dbl)
        {
            if (value is double vdbl)
            {
                return vdbl.CompareTo(dbl);
            }

            if (value is IConvertible convertible)
            {
                return convertible.ToDouble(null).CompareTo(dbl);
            }
        }

        if (operand is long lng)
        {
            if (value is long vlng)
            {
                return vlng.CompareTo(lng);
            }

            if (value is IConvertible convertible)
            {
                return convertible.ToInt64(null).CompareTo(lng);
            }
        }

        if (value is string vstr && operand is string ostr)
        {
            return string.Compare(vstr, ostr, StringComparison.OrdinalIgnoreCase);
        }

        return value.CompareTo(operand);
    }

    private bool EvaluateMissionCondition(ObjectiveCompletionCondition condition)
    {
        if (string.IsNullOrWhiteSpace(condition.Id))
        {
            return false;
        }

        // Check if the specified mission is completed
        if (_missions.TryGetValue(condition.Id, out var mission))
        {
            return mission.IsCompleted;
        }

        return false;
    }

    private bool EvaluateInventoryCondition(ObjectiveCompletionCondition condition)
    {
        if (string.IsNullOrWhiteSpace(condition.Location))
        {
            return false;
        }

        if (!State.TryGetInventoryItems(condition.Location, out var items))
        {
            return false;
        }

        if (condition.RequiredItems is { Count: > 0 })
        {
            foreach (var required in condition.RequiredItems)
            {
                if (!items.Contains(required))
                {
                    return false;
                }
            }
        }

        if (condition.MinItems is int minItems && items.Count < minItems)
        {
            return false;
        }

        if (condition.MinDistinct is int minDistinct && items.Count < minDistinct)
        {
            return false;
        }

        return true;
    }

    private HostSettingsDefinition? TryGetHostProfile(string hostId)
    {
        if (_hostProfiles.TryGetValue(hostId, out var profile))
        {
            return profile;
        }

        return null;
    }

    private static Dictionary<string, MissionState> BuildMissionStates(IReadOnlyList<ChapterDefinition> chapters)
    {
        var missions = new Dictionary<string, MissionState>(StringComparer.OrdinalIgnoreCase);
        foreach (var chapter in chapters)
        {
            foreach (var objective in chapter.Gameplay.Objectives)
            {
                if (!missions.ContainsKey(objective.Id))
                {
                    missions[objective.Id] = new MissionState(objective);
                }
            }
        }

        return missions;
    }

    private static Dictionary<string, HostSettingsDefinition> BuildHostProfiles(IReadOnlyList<ChapterDefinition> chapters)
    {
        var profiles = new Dictionary<string, HostSettingsDefinition>(StringComparer.OrdinalIgnoreCase);
        foreach (var chapter in chapters)
        {
            foreach (var pair in chapter.Gameplay.HostSettings)
            {
                if (!profiles.ContainsKey(pair.Key))
                {
                    profiles[pair.Key] = pair.Value;
                }
            }
        }

        return profiles;
    }

    private static IReadOnlyList<string> CombineMessages(IReadOnlyList<string> first, IReadOnlyList<string> second)
    {
        if (first.Count == 0)
        {
            return second;
        }

        if (second.Count == 0)
        {
            return first;
        }

        var combined = new List<string>(first.Count + second.Count);
        combined.AddRange(first);
        combined.AddRange(second);
        return combined;
    }
}
