using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using JinPingMei.ContentPipeline.Parsing;

namespace JinPingMei.ContentPipeline;

internal sealed class ChapterTemplateBuilder
{
    private const string InstructionHeader = "Analyze the provided chapter and return structured JSON with chapter context.";

    private static readonly string[] OutputSections =
    {
        "synopsis",
        "primaryCharacters",
        "supportingCharacters",
        "locations",
        "entryState",
        "exitState",
        "objectives",
        "notableObjects",
        "dependencies"
    };

    public ChapterAnalysisRequest BuildAnalysisRequest(ChapterDraft chapter)
    {
        if (chapter is null)
        {
            throw new ArgumentNullException(nameof(chapter));
        }

        var instructionBuilder = new StringBuilder();
        instructionBuilder.AppendLine(InstructionHeader);
        instructionBuilder.AppendLine();
        instructionBuilder.AppendLine("Return JSON with the following keys:");
        instructionBuilder.AppendLine("- chapterId");
        instructionBuilder.AppendLine("- chapterNumber");
        foreach (var section in OutputSections)
        {
            instructionBuilder.AppendLine(CultureInfo.InvariantCulture, $"- {section}");
        }
        instructionBuilder.AppendLine();
        instructionBuilder.AppendLine("Guidelines:");
        instructionBuilder.AppendLine("- Use Traditional Chinese for narrative summaries and labels.");
        instructionBuilder.AppendLine("- primaryCharacters/supportingCharacters items should include name, role, intent.");
        instructionBuilder.AppendLine("- entryState/exitState describe how the world or main cast changes across the chapter.");
        instructionBuilder.AppendLine("- dependencies highlight any reliance on prior chapters or foreshadowed events.");
        instructionBuilder.AppendLine("- Keep responses concise but specific.");

        return new ChapterAnalysisRequest(
            chapter.Id,
            chapter.Number,
            chapter.Titles,
            instructionBuilder.ToString().TrimEnd(),
            chapter.Body);
    }
}

internal sealed record ChapterAnalysisRequest(
    string ChapterId,
    int ChapterNumber,
    IReadOnlyList<string> Titles,
    string Instruction,
    string ChapterText);
