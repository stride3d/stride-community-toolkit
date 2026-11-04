using E05_3D_Constraints_Rope;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Overlay;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using System.Globalization;

// Bepu has no rope type. A rope is a chain of small bodies you build yourself, and the interesting
// part is not stringing them together - it is stopping the result from thrashing itself apart.
//
// The enemy is the MASS RATIO. Both ropes here carry the same heavy weight on the same light links.
// The force holding that weight up has to travel link by link to the fixed anchor, and the solver
// only gets so many passes per step. Hang a weight many times heavier than the links on a long thin
// chain and it will not finish in time: the rope stretches, snaps back, and oscillates.
//
// Left rope is the naive build. Right rope is identical apart from two changes taken from Bepu's own
// RopeStabilityDemo, and holds its shape:
//
//   Zero lever arm    - link constraints anchored at segment centres instead of at their ends, so a
//                       segment's own spin cannot feed back into the chain.
//   Skip constraints  - each segment also tied to several further down, giving impulses shortcuts
//                       instead of forcing them to crawl one link at a time.
//
// Watch when the left rope misbehaves: slack, it behaves; pulled taut, it wobbles. A distance limit
// does nothing until it reaches its limit, so a slack rope asks nothing of the solver. Once the chain
// is tight, every link has to relay the supporting force to the anchor - the job the naive build
// cannot finish in time.
//
// Press Z to switch the right-hand rope to the naive build and watch it join in.

// High enough that even the naive rope at full stretch keeps its weight off the floor. A weight
// resting on the ground is held up by the ground, so both ropes go slack and the length readout
// stops measuring anything.
const float AnchorHeight = 9f;
const float NaiveRopeX = -3.5f;
const float StableRopeX = 3.5f;

// Longer ropes read better and are harder to stabilise, since an impulse has further to travel to
// reach the anchor. Raise this and the anchor together, or the naive rope's weight will reach the
// floor at full stretch.
const int LinkCount = 20;
const float LinkRadius = 0.125f;
const float LinkSpacing = 0.075f;
const float LinkMass = 1f;

// Ten times the mass of a single link. Bepu's own demo uses 100:1, but at that ratio the naive rope
// here does not merely sag: it comes apart, stretching past twice its length onto the floor, which
// demonstrates nothing.
//
// The workable ratio also depends on the link count: 20 links tolerate about half the load that 10
// do, since every extra link is another step the force has to travel. Raise either and the left-hand
// rope will eventually reach the floor.
const float WeightRadius = 0.8f;
const float WeightMass = 10f;

// How far ahead each segment is also tied. 1 means neighbours only.
const int SkipSpan = 4;

// Towers for the weights to swing into, so the ropes visibly do work rather than just hang.
const float TowerZ = 3f;
const float TowerBoxSize = 0.6f;
const int TowerRows = 15;
const int TowerColumns = 2;

// Sideways speed given to a weight by the swing key, in m/s. The impulse is derived from it and the
// weight's mass, so changing WeightMass does not change how hard the swing is.
const float SwingSpeed = 4f;

var swingImpulse = new Vector3(0, 0, WeightMass * SwingSpeed);

Rope? naiveRope = null;
Rope? stableRope = null;
var stabilised = true;

using var game = new Game();

var sceneSetup = new SceneSetup3D
{
    Camera = CameraSetup.Perspective(position: new Vector3(13, 12, 12), rotation: new Vector3(40, -24, 0)).WithController(),
    Profiler = true
}.UseBepu().UseSkybox();

game.Run(sceneSetup, start: Start, update: Update);

void Start(Scene scene)
{
    // Pick up and throw any body with the left mouse button (GrabberScript, see E05_3D_Grabber).
    game.AddGrabber();
    game.AddStudioLighting();

    AddInstructions();

    // Anchored at the segment ends, neighbours only: the straightforward build, and the unstable one.
    naiveRope = RopeBuilder.Build(game, scene,
        new Vector3(NaiveRopeX, AnchorHeight, 0),
        new RopeSettings(LinkCount, LinkRadius, LinkSpacing, LinkMass, LeverArm: LinkRadius, SkipSpan: 1, WeightRadius, WeightMass),
        Color.OrangeRed, Color.Firebrick);

    // Same rope, same weight. It is built with the naive lever arm and then stabilised, so the same
    // settings describe both states and Z can move it between them while it hangs.
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
            var box = game.CreatePrimitive(Primitive3D.Cube(TowerBoxSize), new BepuPhysicsOptions
            {
                EntityName = "Tower Box",
                Material = game.CreateMaterial(row % 2 == 0 ? Color.Tan : Color.SandyBrown),
                Position = new Vector3(x + (column - (TowerColumns - 1) / 2f) * TowerBoxSize, TowerBoxSize / 2 + row * TowerBoxSize, TowerZ),
            });

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

    if (game.Input.IsKeyPressed(Keys.P)) SwingWeights();
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

IReadOnlyList<TextElement> BuildOverlayLines()
{
    // Anchor-to-weight distance is the giveaway. Both ropes are built to the same nominal length, so
    // a number that climbs and wanders is a rope being pulled apart faster than the solver can fix.
    return [
        new("Z", $"Stabilise the right rope: {(stabilised ? "on" : "off")}", Color.Yellow),
        new("P", "Swing both weights", Color.Yellow),
        new("Left mouse", "Pick up a link or a weight and throw it", Color.Yellow),
        new(""),
        new($"Naive (left): length {Length(naiveRope)}", Color.OrangeRed),
        new("Lever arm at segment ends, neighbours only", Color.OrangeRed),
        new($"Stable (right): length {Length(stableRope)}", Color.LimeGreen),
        new(stabilised ? $"Zero lever arm, {SkipSpan - 1}x skip constraints" : "Stabilisation off: built like the left one", Color.LimeGreen),
        new($"Both ropes: {LinkCount} links, a weight {WeightMass / LinkMass:0}x a link", Color.LightGray),
        new("A rope is a chain of bodies. Keeping a heavy", Color.LightGray),
        new("weight on a light chain stable is the hard part.", Color.LightGray),
    ];
}

static string Length(Rope? rope) => rope is null ? "-" : rope.Length.ToString("0.00", CultureInfo.InvariantCulture);

void AddInstructions()
{
    // BuildOverlayLines runs every frame the overlay is drawn, which keeps the rope lengths live
    TextOverlay.GetOrCreate(game).AddSection("Game", BuildOverlayLines);
}

/*
---example-metadata
slug: constraint-rope
title:
  en: Rope - building a stable chain of constraints
  cs: Lano - stabilní řetěz vazeb
level: Advanced
category: Physics
complexity: 5
order: 80
description:
  en: |-
    Bepu has no rope type, so a rope is a chain of small bodies tied together at runtime. Keeping a
    heavy weight on a light chain from thrashing itself apart is the real problem: the force holding
    the weight travels link by link to the anchor, and the solver gets only so many passes per frame.
    Two ropes carry the same weight: the naive one ties each segment to its neighbour at the segment
    ends, the stable one anchors at the centres and adds skip constraints that let impulses take
    shortcuts along the chain. Z switches the stabilisation off while it hangs. Follows Bepu's own
    RopeStabilityDemo rather than the obvious ball-socket construction, which is the one that misbehaves.
  cs: |-
    Bepu nemá typ pro lano, takže lano je řetěz malých těles svázaných za běhu. Skutečný problém je
    udržet těžké závaží na lehkém řetězu, aby se neroztrhalo: síla, která závaží drží, putuje článek
    po článku až k úchytu a řešič má jen omezený počet průchodů za snímek. Dvě lana nesou stejné
    závaží: naivní spojuje každý článek se sousedem na koncích článků, stabilní je ukotvené ve
    středech a přidává přeskakující vazby, které impulzům zkrátí cestu podél řetězu. Klávesa Z
    stabilizaci za běhu vypne. Vychází z RopeStabilityDemo přímo od autora Bepu, nikoli z nasnadě
    ležící konstrukce s kulovými klouby, která je právě tou nefungující.
concepts:
  - Building a rope as a runtime chain of bodies and constraints
  - Linking segments with DistanceLimitConstraintComponent rather than ball sockets
  - Why a rope needs a minimum distance well below its maximum
  - Why an unstable rope looks fine while slack and only misbehaves under load
  - How the mass ratio between weight and links drives instability

  - Removing angular feedback with a zero lever arm
  - Letting impulses take shortcuts with skip constraints
  - Moving a constraint's anchor points and allowed distance together at runtime
  - Why a constraint shorter than the distance it spans distorts a rope before anything moves
  - Deriving an impulse from mass so tuning one does not silently change the other
  - Setting collider mass explicitly instead of using the generated collider
  - Size for a sphere primitive is its radius, not its diameter
  - Making the topmost segment kinematic to hold the chain up
  - Applying an impulse from the update loop rather than a velocity at build time
  - Picking up and throwing bodies with GrabberScript
  - "Using helpers: SceneSetup3D, UseSkybox"
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
related:
  - E05_3D_Constraints_Motors
  - E05_3D_Constraints
  - E05_3D_Constraints_Simple
enabled: true
created: 2026-08-09
---
*/