# Content Pipeline

The content pipeline now focuses on deterministic chapter extraction. It slices the 萬曆本 transcript into discrete chapter JSON files and provides LLM-ready templates so downstream tooling can analyse each chapter independently. Nothing in this step depends on the narrative style of *Jin Ping Mei*; any long-form source can reuse the same workflow.

## Chapter extraction

```bash
dotnet run --project src/JinPingMei.ContentPipeline
```

Flags:

- `--input <path>` – override the source transcript (defaults to `data/source-texts/full_version_story.txt`).
- `--output-dir <path>` – override the folder that will receive the extracted chapters (defaults to `src/JinPingMei.Content/Data/chapters`).

Running the extractor produces:

- `chapter-###.json` files containing `id`, `number`, `titles`, and the chapter body.
- `index.json` summarising the extracted chapters.
- `front-matter.txt` where the source includes preface or introductory material.

## LLM analysis template

Once the chapters exist as standalone assets, generate a structured prompt skeleton for any chapter by switching to template mode:

```bash
dotnet run --project src/JinPingMei.ContentPipeline --mode template --chapter chapter-015 --output build/templates/chapter-015.json
```

Template fields:

- `chapterId`, `chapterNumber`, `titles` – identifiers wired to the extracted material.
- `instruction` – guidance for the model to produce JSON describing synopsis, characters, entry/exit state, objectives, notable objects, and dependencies, in Traditional Chinese.
- `chapterText` – the raw chapter text bundled for direct analysis.

Feed the template to your provider of choice and persist the returned JSON alongside the deterministic artifacts. For a different book, point `--input` at the new transcript and reuse the same process.

## Applying the workflow to other texts

1. Drop the new transcript into `data/source-texts/` (or anywhere on disk) and run the extractor.
2. Inspect `index.json` to confirm headings were parsed correctly; adjust `ChapterParser` only if the heading pattern differs materially.
3. Use template mode to scaffold chapter-level analysis prompts, or author your own schema by modifying `ChapterTemplateBuilder`.
4. Store the LLM output together with the extracted chapters to create a reproducible content pack for the runtime.

Because the pipeline stops at plain chapter artifacts, future titles only need light tweaks to the heading regex or prompt wording—the heavy lifting lives with the LLM that interprets each chapter.
