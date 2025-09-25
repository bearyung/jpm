using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace JinPingMei.Engine.Story;

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
                    State.UnlockSideQuest(questElement.GetString()!);
                    return $"取得支線：{questElement.GetString()}";
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
                    State.UnlockMission(missionElement.GetString()!);
                    return $"解鎖任務：{missionElement.GetString()}";
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
        var completedMessages = new List<string>();
        foreach (var mission in _missions.Values)
        {
            if (mission.IsCompleted)
            {
                continue;
            }

            if (CheckMissionCompletion(mission.Definition))
            {
                mission.MarkComplete();
                completedMessages.Add($"任務完成：{mission.Definition.Title}");
            }
        }

        messages = completedMessages;
    }

    private bool CheckMissionCompletion(ObjectiveDefinition objective)
    {
        foreach (var condition in objective.Completion)
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
