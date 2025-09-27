using System;
using System.Collections.Generic;
using System.Linq;
using JinPingMei.Engine;
using JinPingMei.Engine.Story;

namespace JinPingMei.Engine.Tests;

public class MissionUnlockTests
{
    [Fact]
    public void MissionState_OptionalNoAvailability_LockedByDefault()
    {
        // Arrange
        var objective = new ObjectiveDefinition
        {
            Id = "test-mission",
            Title = "Test Mission",
            Category = "Optional",
            Availability = null // No availability block
        };

        // Act
        var missionState = new MissionState(objective);

        // Assert - Optional missions without Availability are locked by default (backward compatibility)
        Assert.False(missionState.IsUnlocked);
    }

    [Fact]
    public void MissionState_StoryNoAvailability_UnlockedByDefault()
    {
        // Arrange
        var objective = new ObjectiveDefinition
        {
            Id = "test-mission",
            Title = "Test Mission",
            Category = "Story",
            Availability = null // No availability block
        };

        // Act
        var missionState = new MissionState(objective);

        // Assert - Story missions without Availability are unlocked by default
        Assert.True(missionState.IsUnlocked);
    }

    [Fact]
    public void MissionState_AvailabilityUnlocked_UnlockedByDefault()
    {
        // Arrange
        var objective = new ObjectiveDefinition
        {
            Id = "test-mission",
            Title = "Test Mission",
            Category = "Optional",
            Availability = new ObjectiveAvailability
            {
                DefaultState = "Unlocked"
            }
        };

        // Act
        var missionState = new MissionState(objective);

        // Assert - Even Optional missions can be unlocked with explicit Availability
        Assert.True(missionState.IsUnlocked);
    }

    [Fact]
    public void MissionState_BonusWithUnlockedAvailability_StartsUnlocked()
    {
        // Arrange
        var objective = new ObjectiveDefinition
        {
            Id = "mission-coldwallet-setup",
            Title = "Cold Wallet Setup",
            Category = "Bonus",
            Availability = new ObjectiveAvailability
            {
                DefaultState = "Unlocked"
            }
        };

        // Act
        var missionState = new MissionState(objective);

        // Assert - Bonus missions with explicit "Unlocked" are unlocked (for evergreen missions)
        Assert.True(missionState.IsUnlocked);
    }

    [Fact]
    public void MissionState_AvailabilityLocked_LockedByDefault()
    {
        // Arrange
        var objective = new ObjectiveDefinition
        {
            Id = "test-mission",
            Title = "Test Mission",
            Category = "Bonus",
            Availability = new ObjectiveAvailability
            {
                DefaultState = "Locked"
            }
        };

        // Act
        var missionState = new MissionState(objective);

        // Assert
        Assert.False(missionState.IsUnlocked);
    }

    [Fact]
    public void StorySession_LockedMission_UnlocksWhenConditionsMet()
    {
        // Arrange
        var runtime = GameRuntime.CreateDefault();
        var storySession = runtime.CreateStorySession("volume-01");
        storySession.SelectHost("武松"); // Select a host to enable Advance

        // Find the bonus_vol1_progress-board mission which should be locked
        var progressBoardMission = storySession.Missions.FirstOrDefault(m => m.Definition.Id == "bonus_vol1_progress-board");

        Assert.NotNull(progressBoardMission);
        Assert.False(progressBoardMission.IsUnlocked);

        // Act - Set the flag that should unlock the mission and advance the story
        storySession.State.SetFlag("vol1.meta.progress_board_done", true);

        // Keep advancing until we process a scene (which triggers mission evaluation)
        var maxAttempts = 10;
        while (maxAttempts-- > 0 && !progressBoardMission.IsUnlocked)
        {
            storySession.Advance();
        }

        // Assert
        Assert.True(progressBoardMission.IsUnlocked);
    }

    [Fact]
    public void StorySession_StoryMission_CanComplete()
    {
        // Arrange
        var runtime = GameRuntime.CreateDefault();
        var storySession = runtime.CreateStorySession("volume-01");
        storySession.SelectHost("武松"); // Select a host to enable Advance

        // Find a story mission (which is unlocked by default)
        var storyMission = storySession.Missions.FirstOrDefault(m =>
            m.Definition.Id == "mission-sworn-oath" &&
            m.Definition.Category == "Story");

        Assert.NotNull(storyMission);
        Assert.True(storyMission.IsUnlocked);
        Assert.False(storyMission.IsCompleted);

        // Act - Meet the completion conditions (only state flags for this mission)
        storySession.State.SetFlag("vol1.ch1.oath_sworn", true);
        storySession.State.SetFlag("vol1.ch1.wuda_acknowledged_vow", true);

        // Keep advancing until we process a scene (which triggers mission evaluation)
        var maxAttempts = 10;
        while (maxAttempts-- > 0 && !storyMission.IsCompleted)
        {
            storySession.Advance();
        }

        // Assert
        Assert.True(storyMission.IsCompleted);
    }

    [Fact]
    public void StorySession_ColdWalletMission_StartsUnlocked()
    {
        // Arrange
        var runtime = GameRuntime.CreateDefault();
        var storySession = runtime.CreateStorySession("volume-01");

        // Find the cold wallet mission which has explicit "Unlocked" availability
        var coldWalletMission = storySession.Missions.FirstOrDefault(m =>
            m.Definition.Id == "mission-coldwallet-setup");

        // Assert - Cold wallet mission should be unlocked due to explicit Availability
        Assert.NotNull(coldWalletMission);
        Assert.True(coldWalletMission.IsUnlocked);
    }

    [Fact]
    public void StorySession_MissionUnlock_EmitsNotification()
    {
        // Arrange
        var runtime = GameRuntime.CreateDefault();
        var storySession = runtime.CreateStorySession("volume-01");
        storySession.SelectHost("武松"); // Select a host to enable Advance

        // Act - Set the flag that should unlock the mission
        storySession.State.SetFlag("vol1.meta.progress_board_done", true);

        // Keep advancing until we get the unlock message
        var maxAttempts = 10;
        var foundUnlock = false;
        while (maxAttempts-- > 0 && !foundUnlock)
        {
            var result = storySession.Advance();
            if (string.Join(" ", result.Messages).Contains("解鎖任務"))
            {
                foundUnlock = true;
            }
        }

        // Assert - Check for unlock notification
        Assert.True(foundUnlock, "Expected to find unlock notification");
    }

    [Fact]
    public void StorySession_MissionCompletion_EmitsNotification()
    {
        // Arrange
        var runtime = GameRuntime.CreateDefault();
        var storySession = runtime.CreateStorySession("volume-01");
        storySession.SelectHost("武松"); // Select a host to enable Advance

        // Find a story mission that's unlocked by default
        var storyMission = storySession.Missions.FirstOrDefault(m =>
            m.Definition.Id == "mission-sworn-oath" &&
            m.Definition.Category == "Story");

        Assert.NotNull(storyMission);
        Assert.True(storyMission.IsUnlocked);

        // Act - Meet the completion conditions
        storySession.State.SetFlag("vol1.ch1.oath_sworn", true);
        storySession.State.SetFlag("vol1.ch1.wuda_acknowledged_vow", true);

        // Keep advancing until we get the completion message
        var maxAttempts = 10;
        var foundCompletion = false;
        while (maxAttempts-- > 0 && !foundCompletion)
        {
            var result = storySession.Advance();
            if (string.Join(" ", result.Messages).Contains("任務完成"))
            {
                foundCompletion = true;
            }
        }

        // Assert - Check for completion notification
        Assert.True(foundCompletion, "Expected to find completion notification");
    }

    [Fact]
    public void MissionState_UnlockMethod_ChangesState()
    {
        // Arrange
        var objective = new ObjectiveDefinition
        {
            Id = "test-mission",
            Title = "Test Mission",
            Availability = new ObjectiveAvailability
            {
                DefaultState = "Locked"
            }
        };
        var missionState = new MissionState(objective);

        Assert.False(missionState.IsUnlocked);

        // Act
        missionState.Unlock();

        // Assert
        Assert.True(missionState.IsUnlocked);
    }

    [Fact]
    public void MissionState_CompleteMethod_SetsTimestamp()
    {
        // Arrange
        var objective = new ObjectiveDefinition
        {
            Id = "test-mission",
            Title = "Test Mission"
        };
        var missionState = new MissionState(objective);

        Assert.False(missionState.IsCompleted);
        Assert.Null(missionState.CompletedAt);

        // Act
        missionState.MarkComplete();

        // Assert
        Assert.True(missionState.IsCompleted);
        Assert.NotNull(missionState.CompletedAt);
        Assert.True((DateTimeOffset.UtcNow - missionState.CompletedAt.Value).TotalSeconds < 5);
    }
}