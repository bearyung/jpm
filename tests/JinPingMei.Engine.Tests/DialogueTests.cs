using System.Linq;
using JinPingMei.Engine.Dialogue;
using JinPingMei.Content.Dialogue;
using System.Collections.Generic;

namespace JinPingMei.Engine.Tests;

public class DialogueTests
{
    [Fact]
    public void DialogueManager_FirstMeeting_ReturnsCorrectDialogue()
    {
        // Arrange
        var definition = CreateTestDialogueDefinition();
        var manager = new DialogueManager(definition);
        var context = new DialogueContext(
            currentLocaleId: "qinghe",
            currentSceneId: "teahouse",
            currentHost: null);

        // Act
        var result = manager.GetDialogue("madam", context);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("頭一回來我們會仙樓吧", string.Join(" ", result.Lines));
        Assert.True(manager.State.HasFlag("met_madam_qin"));
    }

    [Fact]
    public void DialogueManager_SecondMeeting_ReturnsDifferentDialogue()
    {
        // Arrange
        var definition = CreateTestDialogueDefinition();
        var manager = new DialogueManager(definition);
        var context = new DialogueContext(
            currentLocaleId: "qinghe",
            currentSceneId: "teahouse",
            currentHost: null);

        // Act - First interaction
        var firstResult = manager.GetDialogue("madam", context);

        // Act - Second interaction
        var secondResult = manager.GetDialogue("madam", context);

        // Assert
        Assert.NotNull(firstResult);
        Assert.NotNull(secondResult);
        Assert.NotEqual(firstResult.Lines[0], secondResult.Lines[0]);
        Assert.Contains("客官又來了", string.Join(" ", secondResult.Lines));
    }

    [Fact]
    public void DialogueManager_WithSpecificHost_ReturnsHostSpecificDialogue()
    {
        // Arrange
        var definition = CreateTestDialogueDefinition();
        var manager = new DialogueManager(definition);
        var context = new DialogueContext(
            currentLocaleId: "qinghe",
            currentSceneId: "teahouse",
            currentHost: "西門慶");

        // Act
        var result = manager.GetDialogue("madam", context);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("西門大官人", string.Join(" ", result.Lines));
        Assert.True(manager.State.HasFlag("madam_knows_ximen_well"));
    }

    [Fact]
    public void DialogueManager_NonRepeatableDialogue_OnlyShownOnce()
    {
        // Arrange
        var definition = CreateTestDialogueDefinition();
        var manager = new DialogueManager(definition);
        var context = new DialogueContext(
            currentLocaleId: "qinghe",
            currentSceneId: "teahouse",
            currentHost: null);

        // Act - First interaction
        var firstResult = manager.GetDialogue("madam", context);
        Assert.Contains("頭一回", string.Join(" ", firstResult.Lines));

        // Act - Second and third interactions
        var secondResult = manager.GetDialogue("madam", context);
        var thirdResult = manager.GetDialogue("madam", context);

        // Assert - Should not get the first-meeting dialogue again
        Assert.DoesNotContain("頭一回", string.Join(" ", secondResult.Lines));
        Assert.DoesNotContain("頭一回", string.Join(" ", thirdResult.Lines));
    }

    private static DialogueDefinition CreateTestDialogueDefinition()
    {
        return new DialogueDefinition
        {
            DefaultLocaleId = "qinghe",
            DialogueSets = new[]
            {
                new DialogueSet
                {
                    Id = "madam-teahouse",
                    NpcId = "madam",
                    LocaleId = "qinghe",
                    SceneId = "teahouse",
                    Entries = new[]
                    {
                        new DialogueEntry
                        {
                            Id = "first-meeting",
                            Priority = 10,
                            Conditions = new DialogueConditions
                            {
                                MaxInteractionCount = 0
                            },
                            Lines = new[]
                            {
                                "秦媽媽笑容可掬地看著你：",
                                "「哎呀，新面孔！是頭一回來我們會仙樓吧？」"
                            },
                            Effects = new DialogueEffects
                            {
                                SetFlags = new[] { "met_madam_qin" }
                            },
                            Repeatable = false
                        },
                        new DialogueEntry
                        {
                            Id = "regular-greeting",
                            Priority = 5,
                            Conditions = new DialogueConditions
                            {
                                RequiredFlags = new[] { "met_madam_qin" }
                            },
                            Lines = new[]
                            {
                                "秦媽媽認出了你，熱情招呼：",
                                "「客官又來了！今日要聽什麼段子？」"
                            }
                        },
                        new DialogueEntry
                        {
                            Id = "host-ximen",
                            Priority = 15,
                            Conditions = new DialogueConditions
                            {
                                RequiredHost = "西門慶"
                            },
                            Lines = new[]
                            {
                                "秦媽媽眼睛一亮，殷勤上前：",
                                "「西門大官人！許久未見，您可是稀客啊！」"
                            },
                            Effects = new DialogueEffects
                            {
                                SetFlags = new[] { "madam_knows_ximen_well" },
                                ModifyRelationships = new Dictionary<string, int> { ["madam"] = 10 }
                            }
                        }
                    }
                }
            }
        };
    }
}