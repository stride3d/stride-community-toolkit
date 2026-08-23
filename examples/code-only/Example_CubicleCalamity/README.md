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

## Future improvements

Ideas agreed worth doing, roughly by payoff per effort. Game-specific work is tracked here, not in
the repository's `notes/TODO.md` - only toolkit-level features live there.

### Quick wins

- **Perfect-clear bonus** - the classic SameGame rule this game is missing: clear *every* cube and
  a large bonus lands with "PERFECT" raining down in gold instead of "GAME OVER". Gives the
  save-one-colour strategy a true summit. A few lines in `CheckForGameOver` (`Grid.Count == 0`);
  the letters already exist.
- **Level intro drop** - "LEVEL 2" falls as slow-fall 3D letters when a level starts. Reuses
  `FallingLetters.SpawnWord` verbatim.
- **Best score + "NEW RECORD"** - add `BestScore` to `GameProgress`, show "Best:" on the HUD, drop
  NEW RECORD letters when beaten. The natural moment to wire in the ready `JsonProgressStore`,
  since a best score is the thing genuinely worth persisting.
- **Camera auto-framing** - the 5x5x5 board looks small from the distance tuned for the full one;
  pull the camera in proportional to `level.Rows` on level start.

### Gameplay

- **Level goals** - today a level cannot be failed: every dead board offers Next Level, so levels
  are only sizes. A target score (or a max-stranded threshold) per level turns the ladder into a
  run with stakes: reach it and "LEVEL CLEAR" unlocks N; miss it and the run ends for real. Pure
  `Gameplay/` logic, testable like the rest. Pairs naturally with the perfect-clear bonus - those
  two together complete the game loop.
- **Special cubes** - a rare bomb cube (clears a sphere around it), a stone cube (colourless,
  unclearable, must be undermined so it falls away), a rainbow cube (matches anything). Spawn rates
  per level become the difficulty curve. Also the best teaching extension in the game: identity via
  component data, exactly the components-and-scripts manual's lesson, and it forces `MatchFinder`
  to grow cleanly.

### Later

- **Juice pass** - cleared cubes burst into small physics debris, combo sounds rise in pitch with
  the streak (the audio hooks exist), a camera punch on CALAMITY.
- **Undo (U key)** - snapshot the grid before each clear. Good command-pattern teaching material,
  but needs a scoring-fairness decision first (does undo cost points? allowed after game over?).
- **Game modes** - timed or limited-moves, offered through the same dropdown pattern as the
  palettes.

### Housekeeping

- **`CubeGrid.RemoveAndCollapse` returns drop distances nothing consumes** since the physics-driven
  collapse replaced the teleport; only the tests read it, and they are what pin the collapse rule.
  Either keep it as a tested contract or make it `void` and assert grid state instead.
- **The orientation markers are unplaced** - `OrientationGizmo` sits where it did before the
  platform was centred on the origin, and the colliderless `ReferenceCube` has no stated purpose.
  The real fix - a screen-corner axis widget the way editor viewports do it - is a toolkit feature
  and is tracked in `notes/TODO.md`; what this game owes is a decision about the two markers in the
  meantime.
