using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace JinPingMei.ContentPipeline.Parsing;

internal sealed class ChapterParser
{
    private static readonly Regex HeadingRegex = new(
        pattern: @"^第(?<number>[^囬回囘\s　]+)[囬回囘][\s　]*(?<titles>.+)$",
        options: RegexOptions.Multiline | RegexOptions.CultureInvariant);

    public ChapterParseResult Parse(string source)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        var matches = HeadingRegex.Matches(source);
        if (matches.Count == 0)
        {
            throw new InvalidOperationException("Unable to locate any chapter headings in the source text.");
        }

        var chapters = new List<ChapterDraft>(matches.Count);

        var frontMatterStart = 0;
        var frontMatterEnd = matches[0].Index;
        var frontMatter = frontMatterEnd > frontMatterStart
            ? source.Substring(frontMatterStart, frontMatterEnd - frontMatterStart).Trim()
            : string.Empty;

        for (var index = 0; index < matches.Count; index++)
        {
            var match = matches[index];
            var headingEnd = match.Index + match.Length;
            var bodyEnd = index + 1 < matches.Count ? matches[index + 1].Index : source.Length;
            var bodyLength = Math.Max(0, bodyEnd - headingEnd);
            var rawBody = bodyLength > 0 ? source.Substring(headingEnd, bodyLength) : string.Empty;

            var numberToken = match.Groups["number"].Value;
            var number = ChineseNumberParser.Parse(numberToken);

            var titlesToken = match.Groups["titles"].Value;
            var titles = titlesToken
                .Split(new[] {'　'}, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.Trim())
                .ToArray();

            if (titles.Length == 0)
            {
                titles = new[] { match.Value.Trim() };
            }

            var chapterId = $"chapter-{number:D3}";
            var body = rawBody.Trim('\r', '\n', ' ', '\t', '　');

            chapters.Add(new ChapterDraft(
                Id: chapterId,
                Number: number,
                Titles: titles,
                Body: body));
        }

        return new ChapterParseResult(frontMatter, chapters);
    }
}

internal sealed record ChapterParseResult(string FrontMatter, IReadOnlyList<ChapterDraft> Chapters);

internal sealed record ChapterDraft(string Id, int Number, IReadOnlyList<string> Titles, string Body);
