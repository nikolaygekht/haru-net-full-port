---
name: skill-for-nuget-skills
description: |
  Author a Claude Code skill that ships INSIDE a NuGet package via the diegofrata/nuget-skills tool. Use this skill whenever the user wants to create, embed, bundle, or package a skill into a .nupkg; mentions nuget-skills, the `nuget-skills install/scan/load` commands, the SKILL.md `packages:` frontmatter field, a package's `skills/` folder, or wiring a skill through a .csproj `<None Pack="true">` item so it lands in the package's `skills/` folder. It encodes the constraints that make nuget-embedded skills DIFFERENT from native .claude/skills — one concatenated file, no subdirectories, no progressive disclosure, package-id invocation — so you don't repeat the mistakes that are easy to make here.
---

# Authoring a NuGet-embedded skill

This skill is about producing a skill that travels **inside a NuGet package** and is delivered to
a consuming agent by the [`diegofrata/nuget-skills`](https://github.com/diegofrata/nuget-skills)
tool. That delivery mechanism is fundamentally different from native Claude Code skills, and the
differences dictate how you must structure the files. Get the mental model right first, then follow
the recipe.

## First: pick the right route

There are two unrelated ways to ship a skill. Do not mix them.

| | **Native skill** (`.claude/skills/`) | **NuGet-embedded skill** (this skill) |
|---|---|---|
| Delivery | Installed as files; gets a `/name` command + autonomous triggering | Injected as text by `nuget-skills load <PackageId>` |
| Invocation | `/skill-name` or auto | By **NuGet package id**; no slash command, no name lookup |
| Multiple files | Yes — `SKILL.md` + `references/` loaded **on demand** (progressive disclosure) | **No** — everything is concatenated and injected at once |
| Subdirectories | Supported and useful | **Ignored entirely** |
| Use when | Local dev tooling, repo-specific knowledge | Shipping library knowledge to whoever installs your package |

If the goal is "anyone who installs my NuGet package gets this knowledge," use the NuGet-embedded
route below. If the goal is a local `/command` with on-demand reference files, build a native skill
instead — the rest of this document does not apply.

## How nuget-skills delivers a skill (mental model)

1. The **consumer** runs `nuget-skills install` once in their project. It installs a single
   meta-skill and a session-start hook.
2. On session start, the hook runs **`nuget-skills scan`**, which inspects the project's NuGet
   packages and lists those that carry a skill (showing each skill's `name`/`description`).
3. When the work is relevant, the agent pulls the content with **`nuget-skills load <PackageId>`**,
   which prints the skill text to stdout; that text enters the agent's context.

Consequences you must design around:
- **Invocation is by package id**, e.g. `nuget-skills load Contoso.Data`. The `name:` in frontmatter
  is just a display label in the scan list. There is **no `/slash` command**.
- **Triggering is model judgment**, driven entirely by your `description:`. Write it to fire on
  concrete, unambiguous signals (package names, namespaces, type/attribute names, API calls).
- It only works **after** `nuget-skills install` has run in the consuming project and a new session
  has started.

## The hard constraints (this is where skills go wrong)

The tool's `load` reads the package's `skills/` folder with `Directory.GetFiles(dir, "*.md",
TopDirectoryOnly)`, sorts the files (`OrderBy(f => f)`, case-insensitive), and **concatenates every
top-level `.md` file** into one blob separated by `---`, with **no filenames or headers**. From
this, three rules follow:

1. **Ship exactly ONE file: `SKILL.md`.** Splitting content across multiple files buys you
   *nothing* here — `load` dumps them all into context together, so you pay the full token cost
   regardless. The progressive-disclosure benefit that justifies multiple files in a *native* skill
   does not exist in this route. A single file is simpler and avoids the ordering trap below.
2. **Do not use subdirectories.** A `references/` folder (or any nested dir) is **silently ignored**
   — `TopDirectoryOnly` never descends into it. Content there will never reach the agent. If you
   have reference material, inline it as sections of the one `SKILL.md`.
3. **If you are ever forced to have multiple top-level files**, know that `scan` reads the
   `name`/`description`/`packages` frontmatter from `skillFiles[0]` — the **alphabetically first**
   file. Lowercase reference names like `entity-model.md` sort *before* `SKILL.md`, so the wrong
   frontmatter gets read and your skill shows up unnamed or unmatched. Force the frontmatter file
   first with a numeric prefix (`00-skill.md`, `10-…`). But prefer rule 1 — one file sidesteps this.

Name the file literally **`SKILL.md`**. That matches the tool's expected filename for both
local-bundled discovery and the remote GitHub-repo fallback.

## Write the SKILL.md

Frontmatter (YAML):

```markdown
---
name: your-skill-name
description: |
  One or two sentences that make the agent load this exactly when it should. Name the packages,
  namespaces, attributes, and API types that signal relevance. Avoid vague triggers like "data
  access" that fire in unrelated contexts.
packages: Your.Package, Your.Package.*, Related.Core
---
```

- `name` — short kebab-case label shown in the scan list.
- `description` — the **only** thing that drives triggering. Be specific and concrete.
- `packages` — **optional** glob/comma-separated list controlling which referenced packages cause
  the skill to surface. **Omitting it means the skill applies to all packages from the repo.** Set
  it when one repo produces many packages and the skill is only relevant to some.

Body — content principles:
- **Actionable over informational** — tell the agent what to DO, not what the library IS.
- **Conventions and gotchas** — capture what isn't obvious from the API surface.
- **Code over prose** — show patterns; don't just describe them.
- **Concise** — every line costs context on every load. No filler.

## Where the file lives and how it's discovered

The tool checks two sources, **local first**:

1. **Bundled (preferred):** a `skills/` (or `.skills/`) folder at the **root of the extracted NuGet
   package**, containing `SKILL.md`. This is what you produce by packing the file (next section).
2. **Remote fallback:** `skills/SKILL.md` at the **root of the package's GitHub repo** (resolved
   from the nuspec's project/repository URL, via the `gh` CLI).

Bundling is the reliable path — it works offline and doesn't depend on repo layout. Use remote only
as a convenience fallback.

## Wire it into the package

The `SKILL.md` source can live anywhere in your tree; what matters is that it lands at **`skills/`
in the package root**. With an SDK-style `.csproj`, add a packed item:

```xml
<ItemGroup>
  <None Include="skills\SKILL.md" Pack="true" PackagePath="skills\" />
</ItemGroup>
```

`PackagePath="skills\"` places the file at `skills/SKILL.md` in the nupkg — exactly where `scan`
looks. (`Include="skills\**"` also works if you keep multiple files, subject to the constraints
above.)

If your build uses a custom packaging pipeline instead of the csproj, the rule is unchanged: route
the file so it ends up at `skills/SKILL.md` in the package root.

**Bundle in ONE package only.** If several of your packages depend on a core package, embed the
skill in the core package; dependents pull it in transitively, and the `packages:` glob makes it
surface for them too. Bundling the same skill in multiple packages just duplicates it.

## Always verify the built package

A `.nupkg` is a zip. After building, confirm the skill landed correctly:

```bash
# 1. The skill is at the package root under skills/
unzip -l Your.Package.X.Y.Z.nupkg | grep -i skill
#   expect: skills/SKILL.md

# 2. Content is intact and byte-identical to source
unzip -p Your.Package.X.Y.Z.nupkg skills/SKILL.md > /tmp/in_pkg.md
cmp /tmp/in_pkg.md path/to/source/SKILL.md && echo IDENTICAL

# 3. Scope is correct — skill is ONLY in the intended package(s)
for p in Other.Package.X.Y.Z.nupkg; do
  echo "$p: $(unzip -l "$p" | grep -ic skill) skill entries"   # expect 0
done
```

If `skills/SKILL.md` is missing, the packaging step didn't pick up the file — recheck the
source path and `PackagePath`.

## Checklist

- [ ] Decided this is the NuGet-embedded route (not a native `/command` skill).
- [ ] Exactly one file named `SKILL.md`; no subdirectories.
- [ ] Frontmatter: specific `description`; `packages` set (or deliberately omitted).
- [ ] Body is actionable, code-first, concise.
- [ ] Packed to `skills/` at package root via a csproj `<None Pack="true">` item.
- [ ] Embedded in a single package; dependents covered via dependency + `packages:` glob.
- [ ] Built nupkg verified: `skills/SKILL.md` present, byte-identical, scoped correctly.
