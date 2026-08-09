using Example15_Constraint_Rope;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;

// Bepu has no rope type. A rope is a chain of small bodies you build yourself, and the interesting
// part is not stringing them together - it is stopping the result from thrashing itself apart.
//
// The enemy is the MASS RATIO. Both ropes here carry the same heavy weight on the same light links.
// The force holding that weight up has to travel link by link all the way to the fixed anchor, and a
// solver only gets so many passes per frame to work that out. Ask it to hold a hundred times its
// own mass through a long thin chain and it will not finish in time: the rope stretches, snaps back,
// and oscillates.
//
// Left rope is the naive build. Right rope is identical apart from two changes taken from Bepu's own
// RopeStabilityDemo, and holds its shape:
//
//   Zero lever arm    - link constraints anchored at segment centres instead of at their ends, so a
//                       segment's own spin cannot feed back into the chain.
//   Skip constraints  - each segment also tied to several further down, giving impulses shortcuts
//                       instead of forcing them to crawl one link at a time.
//
// Watch when the left rope misbehaves, because it is the clearest evidence of the cause: while the
// chain hangs slack it looks perfectly well behaved, and it only goes wobbly once it is pulled taut.
// A distance limit does nothing until it reaches its limit, so a slack rope asks nothing of the
// solver. The instant the weight draws the chain tight, every link has to relay the supporting force
// to the anchor - and that is the job the naive build cannot finish in the time available.
//
// Press Z to switch the right-hand rope to the naive build and watch it join in.

// High enough that even the naive rope at full stretch keeps its weight off the floor. This matters
// more than it looks: a weight resting on the ground is being held up by the ground, so both ropes go
// slack, the stretch that the whole example is about disappears, and the length readout stops
// measuring anything.
const float AnchorHeight = 14.5f;
const float NaiveRopeX = -3.5f;
const float StableRopeX = 3.5f;

// Longer ropes read better and are harder to stabilise, since an impulse has further to travel to
// reach the anchor. Raise this and the anchor together, or the naive rope's weight will reach the
// floor at full stretch.
const int LinkCount = 20;
const float LinkRadius = 0.125f;
const float LinkSpacing = 0.075f;
const float LinkMass = 1f;

// Ten times the mass of a single link. Bepu's own demo uses 100:1, but it also runs its solver at
// eight velocity iterations; at Stride's default solver settings the naive rope stops merely sagging
// and simply comes apart, stretching past twice its length and landing on the floor, which
// demonstrates nothing except that the configuration was hopeless.
//
// The workable ratio also depends on how many links the force has to travel through: 20 links tolerate
// about half the load that 10 do, because every extra link is another step the solver has to push the
// supporting force along. Raise either and the left-hand rope will eventually reach the floor.
const float WeightRadius = 0.8f;
const float WeightMass = 10f;

// How far ahead each segment is also tied. 1 means neighbours only.
const int SkipSpan = 4;

// Towers for the weights to swing into, so the ropes visibly do work rather than just hang.
const float TowerZ = 3f;
const float TowerBoxSize = 0.6f;
const int TowerRows = 15;
const int TowerColumns = 2;

// Sideways speed given to a weight by the swing key, in metres per second. An impulse is mass times
// velocity, so it is derived from the weight rather than written as a raw number - otherwise every
// change to WeightMass silently changes how hard the swing is.
const float SwingSpeed = 4f;

var swingImpulse = new Vector3(0, 0, WeightMass * SwingSpeed);

DebugTextPrinter? instructions = null;

Rope? naiveRope = null;
Rope? stableRope = null;
var stabilised = true;

using var game = new Game();

game.Run(start: Start, update: Update);

void Start(Scene scene)
{
    game.SetupBase3DScene();
    game.AddSkybox();
    game.AddProfiler();

    InitializeDebugTextPrinter();

    // Anchored at the segment ends, neighbours only: the straightforward build, and the unstable one.
    naiveRope = RopeBuilder.Build(game, scene,
        new Vector3(NaiveRopeX, AnchorHeight, 0),
        new RopeSettings(LinkCount, LinkRadius, LinkSpacing, LinkMass, LeverArm: LinkRadius, SkipSpan: 1, WeightRadius, WeightMass),
        Color.OrangeRed, Color.Firebrick);

    // Same rope, same weight. It is built with the naive lever arm and then stabilised, so the same
    // settings describe both states and S can move it between them while it hangs.
    stableRope = RopeBuilder.Build(game, scene,
        new Vector3(StableRopeX, AnchorHeight, 0),
        new RopeSettings(LinkCount, LinkRadius, LinkSpacing, LinkMass, LeverArm: LinkRadius, SkipSpan, WeightRadius, WeightMass),
        Color.LimeGreen, Color.DarkGreen);

    stableRope.SetStabilised(true);

    CreateTower(scene, NaiveRopeX);
    CreateTower(scene, StableRopeX);
}

/// <summary>
/// A stack of loose boxes standing in the path of a swinging weight.
/// </summary>
/// <remarks>
/// Tall enough to reach the weight, which hangs well above the floor. Nothing holds these together,
/// so how far the stack scatters is a rough read on how much energy the rope delivered.
/// </remarks>
void CreateTower(Scene scene, float x)
{
    for (var row = 0; row < TowerRows; row++)
    {
        for (var column = 0; column < TowerColumns; column++)
        {
            var box = game.Create3DPrimitive(PrimitiveModelType.Cube, new Bepu3DPhysicsOptions
            {
                EntityName = "Tower Box",
                Material = game.CreateMaterial(row % 2 == 0 ? Color.Tan : Color.SandyBrown),
                Size = new Vector3(TowerBoxSize),
            });

            box.Transform.Position = new Vector3(
                x + (column - (TowerColumns - 1) / 2f) * TowerBoxSize,
                TowerBoxSize / 2 + row * TowerBoxSize,
                TowerZ);

            box.Scene = scene;
        }
    }
}

void Update(Scene scene, GameTime time)
{
    if (game.Input.IsKeyPressed(Keys.Z) && stableRope is not null)
    {
        // Turns the right-hand rope into the left-hand one and back. Both fixes move together on
        // purpose: at this mass ratio the zero lever arm does nearly all the work, and toggling the
        // skip constraints on their own is almost invisible. They start paying for themselves at the
        // far heavier ratios Bepu's own demo uses.
        stabilised = !stabilised;
        stableRope.SetStabilised(stabilised);
    }

    if (game.Input.IsKeyPressed(Keys.P))
    {
        SwingWeights();
    }

    DisplayInstructions();
}

/// <summary>
/// Shoves both weights sideways with the same impulse, so any difference in how the two ropes cope
/// comes from how they are built rather than from the disturbance.
/// </summary>
/// <remarks>
/// Applied as an impulse from the update loop rather than as a velocity at build time: a velocity
/// assigned before the body reaches the simulation is silently discarded.
/// </remarks>
void SwingWeights()
{
    Push(naiveRope);
    Push(stableRope);

    void Push(Rope? rope)
    {
        if (rope is null) return;

        rope.Weight.ApplyLinearImpulse(swingImpulse);
        rope.Weight.Awake = true;
    }
}

void DisplayInstructions()
{
    if (instructions is null) return;

    // Anchor-to-weight distance is the giveaway. Both ropes are built to the same nominal length, so
    // a number that climbs and wanders is a rope being pulled apart faster than the solver can fix.
    instructions.Print([
        new("A rope is a chain of bodies. Keeping a heavy weight on a light chain stable is the hard part."),
        new($"Both ropes are identical: {LinkCount} links, one weight {WeightMass / LinkMass:0}x heavier than a link."),
        new($"Naive  (left):  length {Length(naiveRope)}   lever arm at segment ends, neighbours only", Color.OrangeRed),
        new($"Stable (right): length {Length(stableRope)}   {(stabilised ? $"zero lever arm, {SkipSpan - 1}x skip constraints" : "STABILISATION OFF - now built like the left one")}", Color.LimeGreen),
        new($"Z - Stabilise right rope: {(stabilised ? "ON" : "OFF")}", Color.Yellow),
        new("P - Swing both weights", Color.Yellow),
    ]);
}

static string Length(Rope? rope) => rope is null ? "-" : rope.Length.ToString("0.00");

void InitializeDebugTextPrinter()
{
    var screenSize = new Int2(game.GraphicsDevice.Presenter.BackBuffer.Width, game.GraphicsDevice.Presenter.BackBuffer.Height);

    instructions = new DebugTextPrinter()
    {
        DebugTextSystem = game.DebugTextSystem,
        TextSize = new(340, 20 * 6),
        ScreenSize = screenSize,
    };

    instructions.Initialize(DisplayPosition.BottomLeft);
}

/*
---example-metadata
title:
  en: Rope - building a stable chain of constraints
  cs: Lano - stabilní řetěz vazeb
level: Intermediate
category: Physics
complexity: 7
description:
  en: |
    Bepu has no rope type, so a rope is a chain of small bodies tied together at runtime. Stringing
    them together is the easy part; keeping a heavy weight on a light chain from thrashing itself
    apart is the real problem, because the force holding that weight up has to travel link by link
    to the fixed anchor and the solver only gets so many passes per frame. Two ropes hang side by
    side carrying the same weight: the naive one anchors its constraints at the segment ends and ties
    each segment only to its neighbour, while the stable one anchors at the segment centres and adds
    skip constraints that let impulses take shortcuts along the chain. The skip constraints can be
    switched off while it hangs, which shows immediately what they were holding together. Follows
    Bepu's own RopeStabilityDemo rather than the more obvious ball-socket construction, which is
    precisely the one that misbehaves.
  cs: |
    Bepu nemá typ pro lano, takže lano je řetěz malých těles svázaných za běhu. Spojit je dohromady je
    ta snadná část; skutečný problém je udržet těžké závaží na lehkém řetězu, aby se neroztrhalo -
    síla, která závaží drží, musí putovat článek po článku až k pevnému úchytu a řešič má na to jen
    omezený počet průchodů za snímek. Vedle sebe visí dvě lana se stejným závažím: naivní má vazby
    ukotvené na koncích článků a každý článek spojený jen se sousedem, kdežto stabilní je ukotvené ve
    středech článků a navíc má přeskakující vazby, které impulzům umožní zkratku podél řetězu.
    Přeskakující vazby lze za běhu vypnout, což okamžitě ukáže, co držely pohromadě. Vychází z
    ukázky RopeStabilityDemo přímo od autora Bepu, nikoli z nasnadě ležící konstrukce s kulovými
    klouby, která je právě tou nefungující.
concepts:
  - Building a rope as a runtime chain of bodies and constraints
  - Linking segments with DistanceLimitConstraintComponent rather than ball sockets
  - Why a rope needs a minimum distance well below its maximum
  - Why an unstable rope looks fine while slack and only misbehaves under load
  - How the mass ratio between weight and links drives instability
  - Why solver iteration count decides how extreme a ratio survives
  - Removing angular feedback with a zero lever arm
  - Letting impulses take shortcuts with skip constraints
  - Moving a constraint's anchor points and allowed distance together at runtime
  - Why a constraint shorter than the distance it spans distorts a rope before anything moves
  - Deriving an impulse from mass so tuning one does not silently change the other
  - Setting collider mass explicitly instead of using the generated collider
  - Size for a sphere primitive is its radius, not its diameter
  - Making the topmost segment kinematic to hold the chain up
  - Applying an impulse from the update loop rather than a velocity at build time
  - "Using helpers: SetupBase3DScene, AddSkybox, AddProfiler"
related:
  - Example15_Constraint_Motors
  - Example15_Constraint
  - Example15_Constraint_Simple
tags:
  - 3D
  - Physics
  - Bepu
  - Constraint
  - Rope
  - Distance Limit
  - Rigid Body
  - Kinematic Body
  - Stability
  - Intermediate
order: 15
enabled: true
created: 2026-08-09
---
*/