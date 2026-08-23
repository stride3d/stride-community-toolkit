# Cubicle Calamity

A small collapse game (in the SameGame family) built entirely in code with the Stride Community
Toolkit: a platform of coloured cubes stacks itself up, and you take it apart by clicking groups
of matching colours. Levels grow the board - 5 × 5 × 5 to begin with, one cube larger per side
each level, up to the full 10 × 10 × 10. It doubles as the toolkit's most complete worked example - the
same project appears throughout
[docs/manual/components-and-scripts.md](../../../docs/manual/components-and-scripts.md) as the
living illustration of when to use components, scripts and physics-derived components.

## Running it

From the repository root:

```bash
dotnet run --project examples/code-only/Example_CubicleCalamity
```

or pick **Cubicle Calamity** in the examples launcher.

## How to play

The platform builds itself layer by layer, then drops onto the physics. From then on:

- **Click a cube** to clear the whole group of same-coloured cubes connected to it (touching by
  faces, in all three dimensions - groups continue *into* the pile, not just across its surface).
- A group needs **at least 2 cubes**. A lone cube with no matching neighbour can never be cleared -
  every game ends with some of these stranded.
- Everything above a cleared group falls to fill the gap, which merges and splits the remaining
  groups - the board you are reading is always one clear away from a different one.
- The game ends when no clearable group remains. The final score rains down in solid 3D letters,
  then a menu offers **N** - next level, **R** - restart, and **Q** - quit.
- **N** moves to a larger board and your score carries over - climbing levels is how big totals are
  made. **R** replays the current level from zero. Progress is fresh each launch for now; the code
  has a ready `JsonProgressStore` (see `Gameplay/GameProgress.cs`) that makes the next launch
  resume where you left off, one line to wire in.

### Reading the board

Hover before you click - the board answers:

- The whole group under the mouse **lights up**: this is what a click would clear.
- A hovered cube **fades**: it is a stranded single, and clicking it does nothing (a dull note
  confirms it if you try).

### Controls

| Input | Effect |
|---|---|
| Left click | Clear the group under the cursor |
| Shift + hold left button | Clear repeatedly, for taking a board apart quickly |
| Z / C | Orbit the camera around the platform (hold Shift to sprint) |
| WASD, Q / E, right-drag | Free camera movement and look (F2 shows the full overlay) |
| H | Reset the camera to its starting view |
| P, then 1 / 2 / 3 | Switch the colour palette - Classic, Soft, or High visibility (colour-blind friendly). Repaints the standing board in place |
| N (after game over) | Advance to the next, larger level - score carries over |
| R (after game over) | Restart the current level from zero |
| Q (after game over) | Quit |

## Scoring

Every number below lives in one place - `Shared/GameSettings.cs` and
`Gameplay/ScoreRules.cs` - so this section names the rules and the code holds the current values.

**A clear of `n` cubes is worth `n² × 10` points** (before any combo). That is a base of 10 per
cube plus a group bonus of `n × (n − 1) × 10` - written that way so a lone cube's bonus is zero by
construction rather than by special case. The bonus is quadratic, which is the whole strategy of
the game: one clear of 20 (4,000 points) beats ten clears of 2 (400 points) ten times over, so
engineering big groups - for example nibbling other colours away so one colour merges into a
monster - is what high scores are made of.

Big clears also get a shout:

| Group size | Label |
|---|---|
| 5+ | NICE! |
| 10+ | GREAT! |
| 18+ | HUGE! |
| 30+ | CALAMITY! |

### Combos

Each clear opens a short **combo window** (`GameSettings.ComboWindowSeconds`). Clear again before
it closes and the streak grows, multiplying each successive clear through
**×1 → ×1.5 → ×2 → ×3 → ×5** (staying at ×5 from there). Hesitate past the window and the streak
resets to ×1. A misjudged click does **not** break the streak - only time does - so the combo
punishes stalling, not the occasional bad read.

A worked example: your third clear inside the window (×2) takes a group of 6.
Base `6 × 10 = 60`, bonus `6 × 5 × 10 = 300`, total `360 × 2 = 720` points - and the window
reopens for the next one.

The running total in the corner counts up toward the real score. While a streak is alive, the
combo line under it shows the current multiplier, and the bar beneath that drains as the window
runs out - clear again before it empties to keep the streak. Below all of that, a quiet line
counts what is left: how many cubes still stand, and how many **moves** - clearable groups - are
hidden among them. When it says a few moves remain and you cannot see one, orbit: they are on the
far side.

## Where the rules live

The playable rules are plain classes with no dependency on the scene, tested without a running
game in `tests/Stride.CommunityToolkit.Tests`:

| Piece | File | What it owns |
|---|---|---|
| The board | `Gameplay/CubeGrid.cs` | Who is where, and how columns collapse |
| Matching | `Gameplay/MatchFinder.cs` | Flood-fill groups, the minimum-size rule, game over |
| Scoring | `Gameplay/ScoreRules.cs`, `Gameplay/ScoreKeeper.cs` | Points, tiers, the combo streak |
| Levels | `Gameplay/LevelRules.cs` | How the board grows, and where each board's centre is |
| Progress | `Gameplay/GameProgress.cs` | The persistence seam: fresh each launch now, JSON-ready |
| Tunables | `Shared/GameSettings.cs`, `Shared/ColourPalettes.cs` | Size caps, pace, scoring constants, palettes |

Everything else - input, physics, sound, popups, the 3D letters - is presentation around that
core, and the project's structure is walked through in
[components-and-scripts.md](../../../docs/manual/components-and-scripts.md).
