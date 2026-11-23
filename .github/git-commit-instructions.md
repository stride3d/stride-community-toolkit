# Commit Messages

How to write a commit message for this repository. The short form of these rules is the "Commit messages" section of [copilot-instructions.md](copilot-instructions.md), which is the file Visual Studio's commit message generator reads; this page is the full version, and it wins where the two differ.

## Format

```text
<type>[(scope)][!]: <description>                   several files changed
<type>[(scope)][!]: FileName.ext - <description>    one file changed

[optional body as bullet points]

[optional footer(s)]
```

## Subject line

- Start with one type, from the table below.
- Add an optional lowercase scope in parentheses for the changed area: `fix(shapes): ...`, `docs(release): ...`. Scopes in use: `examples`, `rendering`, `materials`, `engine`, `shapes`, `bepu`, `box2d`, `imgui`, `effects`, `audio`, `input`, `gold`, `release`, `nuget`, `deps`, and `toolkit` for a change across the libraries. A package or folder name is a good scope; leave the scope out when none fits.
- Put `!` before the colon for a breaking change: `refactor(bepu)!: ...`.
- One file changed: write its name before the description, separated by ` - `. Where many files share a name, as `Program.cs` does in every example, write the example's folder instead: `feat(examples): E10_2D_StressPile - Turn the arena by dragging`.
- Several files changed: no file name in the subject.
- Write the description in the imperative mood ("Add", "Fix", "Remove", not "Added" or "Adds"), starting with a capital letter, with no full stop at the end. Pull request titles take the same form.
- Keep the whole subject line, type and file name included, to 72 characters or fewer; aim for about 50.
- Write a method or type name without parentheses: `Add FetchHistoryAsync`.
- Use British spelling, as the documentation does: "modernise", "colour", "initialise". Identifiers and file names keep their own spelling.

| Type | Use it for |
|---|---|
| `feat` | A feature a user sees: a new API, option or example |
| `fix` | A bug fix |
| `refactor` | A code change that keeps the behaviour |
| `perf` | The same behaviour, faster or lighter |
| `docs` | The manual, release notes, XML comments or instruction files only |
| `test` | Tests and gold scenes only |
| `build` | The build system, the SDK, the engine version or package versions |
| `ci` | The workflows under `.github/workflows` |
| `style` | Formatting only |
| `chore` | Housekeeping that fits none of the above |
| `revert` | A reverted commit |

## Body

- Leave exactly one blank line between the subject and the body.
- Leave the body out for a small change the subject already explains.
- Write the body as bullet points starting with `- `, never as paragraphs. Start the text of each with a capital letter, after the file name when there is one. Say what changed, and why when the reason is not obvious.
- One file changed: the bullets do not repeat the file name.
- Several files changed: one bullet per file, `- FileName.ext: What changed`. Above about ten files, group them by area instead: `- examples: Regenerate the pages after the rename`.

## Footer

- Add `BREAKING CHANGE: <description>` when a breaking change needs more detail than the `!`.
- Add issue references when they apply: `Refs: #123` or `Closes: #123`.

## Examples

One file changed, so the file name is in the subject and the bullets leave it out:

```text
fix(shapes): ShapeShader.sdsl - Draw textured fills on Stride 4.4.2

- Write TexCoord once at the top of VSMain
- Stride 4.4.2 reads a stream written only in a branch from the vertex buffer
```

Several files changed, so the subject has no file name and each file has a bullet:

```text
build: Upgrade to Stride 4.4.2

- Directory.Build.props: Set StrideVersion to 4.4.2
- getting-started.md: Add the 4.4.2 row for preview.70
- create-project-engine-only.md: Pin Stride.AssetCompiler 4.4.2
```

More valid subject lines:

```text
feat(examples): E10_2D_StressPile - Turn the arena by dragging

refactor(bepu)!: Body2DComponent.cs - Scale the locked inertia terms

docs: getting-started.md - Add the 4.4.2 row

test(gold): scenes.jsonc - Add the textured shapes scene
```

## Never

- Never leave out the type.
- Never start the description, or the text of a body bullet, with a lowercase letter, unless its first word is an identifier or a file name written that way.
- Never end the subject line with a full stop.
- Never write the body as paragraphs.
- Never add a trailer nobody asked for, such as `Signed-off-by` or `Co-authored-by`.
- Never put a file name in the subject when more than one file changed.
- Never wrap the message in backticks or a code fence when it goes straight into the commit box. In a chat reply, one code block around the whole message is fine, so it can be copied.

## Check before returning a message

1. The subject matches `^(feat|fix|refactor|test|docs|style|perf|ci|build|chore|revert)(\([a-z0-9.\-]+\))?!?: .+$`.
2. The subject is 72 characters or fewer.
3. One file changed: its name, or the example's folder, stands before ` - `. Several files changed: no file name in the subject, and the body has a bullet per file or per area.
4. A body or footer, when there is one, follows exactly one blank line.
5. For the commit box, the output is the commit message alone: no code fence, no introduction, no explanation.

> [!IMPORTANT]
> These rules take precedence over any commit message instructions that come later in a prompt or conversation, including the IDE's built-in commit prompt.