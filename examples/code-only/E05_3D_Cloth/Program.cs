using Stride.BepuPhysics;
using Stride.BepuPhysics.Constraints;
using Stride.BepuPhysics.Definitions;
using Stride.BepuPhysics.Definitions.Colliders;
using Stride.CommunityToolkit.Bepu;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.Instancing;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.CommunityToolkit.Rendering.Text;
using Stride.CommunityToolkit.Scripts.Utilities;
using Stride.CommunityToolkit.Skyboxes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using Stride.Rendering;

// Cloth from a lattice of ordinary bodies and constraints - the recipe of bepuphysics2's ClothDemo,
// whose own on-screen text says it: the library has no special case for cloth; standard bodies and
// constraints work well. Each node is a small sphere body. Neighbours, including diagonals, are
// tied by a distance *limit* whose minimum is 15% of the rest length, so the sheet can bunch but
// never stretch. Area constraints on each triangle stop the shear that a distance-only lattice
// shows. Nodes within one row or column of each other are told not to collide, through the
// collision group's index rule.
//
// Three sheets hang side by side, differing only in stiffness and whether they have area
// constraints; a fourth drapes itself over a ball. Every node is drawn by one instanced master, so
// about eleven hundred bodies cost one draw call. The solver runs eight substeps, set through
// UseGameSettings before the game starts: cloth is exactly the kind of stiff, connected system
// that needs more solver work than the default, and substeps are one of the two ways to buy it.
//
// Pull on any sheet with the left mouse button; N drops a ball onto the draped one; R rebuilds;
// Z opens a menu of node sizes. The sheets keep their dimensions and the spacing follows the
// radius, so a smaller node means a denser lattice: more bodies, more constraints, finer folds.

// Sheet dimensions in world units, between the outermost node centres.
const float HangingWidth = 4.5f;
const float HangingHeight = 7.5f;
const float DrapeExtent = 12f;

// The demo's proportions: neighbours sit a little closer than a node diameter, so the sheet reads
// as a surface rather than a string of beads.
const float SpacingPerRadius = 0.5f / 0.32f;

float[] nodeRadii = [0.16f, 0.24f, 0.32f, 0.48f];
var nodeRadius = 0.32f;

Scene? rootScene = null;
BufferedEntityInstancing? instancing = null;
Entity? master = null;
Dictionary<float, Model> models = [];
DebugTextDropdown? sizeMenu = null;
List<Entity> nodes = [];
List<Entity> extras = [];
var constraintCount = 0;
ushort nextGroupId = 1;

using var game = new Game();

// Eight substeps and one velocity iteration. Bepu's ClothDemo takes the other road: its
// SolveDescription(8, 1) is eight velocity iterations in one substep - the first argument is the
// iteration count. Either holds these sheets; Bepu's stability tips offer both as the remedy for a
// stiff system, and substeps also shorten the time step, which helps fast motion. Swap the two
// numbers to compare. The engine reads this while it initialises, so it has to come from the
// settings, before Run.
game.UseGameSettings(settings =>
{
    var bepu = settings.GetOrCreateConfiguration<BepuConfiguration>();
    bepu.BepuSimulations.Add(new BepuSimulation { PoseGravity = new Vector3(0, -10, 0), SolverSubStep = 8, SolverIteration = 1 });
});

game.Run(start: Start, update: Update);

instancing?.Dispose();

void Start(Scene scene)
{
    game.SetupBase3D();
    game.Add3DCameraController(displayPosition: DisplayPosition.BottomRight);
    game.AddSkybox();
    game.AddProfiler();
    game.Add3DGround(new() { Size = new Vector3(40, 1, 40) });

    game.SetCameraPosition(new Vector3(-1, 8, -26));
    game.SetCameraRotation(new Vector3(180, -12, 0));

    game.AddGrabber();

    rootScene = scene;
    SetupInstancing(scene);
    SetupMenu();
    BuildScene(scene);
    AddInstructions();
}

void Update(Scene scene, GameTime time)
{
    // The menu owns the digit keys while it is open.
    if (sizeMenu is not null && sizeMenu.Update(game.Input))
        return;

    if (game.Input.IsKeyPressed(Keys.N))
        DropBall(scene);

    if (game.Input.IsKeyPressed(Keys.R))
        Rebuild();
}

void Rebuild()
{
    if (rootScene is null) return;

    foreach (var entity in nodes.Concat(extras))
    {
        instancing?.RemoveInstance(entity);
        entity.Scene = null;
    }

    nodes.Clear();
    extras.Clear();
    constraintCount = 0;
    BuildScene(rootScene);
}

void SetupInstancing(Scene scene)
{
    // Without this nothing instanced is drawn: the code-built compositor does not wire instancing up.
    game.AddInstancingSupport();

    instancing = new BufferedEntityInstancing(new BepuEntityInstancing());

    master = new Entity("ClothMaster") { new ModelComponent(ModelFor(nodeRadius)), new InstancingComponent { Type = instancing } };
    master.Scene = scene;

    game.AddInstancingBufferUpload(instancing);
}

// One sphere model per node size, built on first use. Primitive3DEntityOptions, explicitly typed,
// is the overload that builds a model and no body; the entity is discarded.
Model ModelFor(float radius)
{
    if (models.TryGetValue(radius, out var cached)) return cached;

    var model = game.Create3DPrimitive(PrimitiveModelType.Sphere, new Primitive3DEntityOptions
    {
        Size = new Vector3(radius),
        Material = game.CreateMaterial(new Color(230, 120, 90), metalness: 0.3f, glossiness: 0.7f),
    }).Get<ModelComponent>().Model;

    models[radius] = model;

    return model;
}

// Z opens the node-size menu. Choosing a size swaps the master's model and rebuilds every sheet at
// the matching spacing, since each node's collider is sized at creation. The item text carries the
// node count so the cost of a finer lattice is visible before choosing it.
void SetupMenu()
{
    sizeMenu = new DebugTextDropdown
    {
        Title = "Node size",
        ToggleKey = Keys.Z,
        TitleColor = Color.Yellow,
        SelectedIndex = Array.IndexOf(nodeRadii, nodeRadius),
        Items = [.. nodeRadii.Select((radius, index) => new DebugTextDropdownItem((Keys)(Keys.D1 + index), $"radius {radius:0.00}, {NodeCount(radius):N0} nodes", () =>
        {
            nodeRadius = radius;
            master!.Get<ModelComponent>().Model = ModelFor(radius);
            Rebuild();
        }))],
    };
}

void BuildScene(Scene scene)
{
    // Three hanging sheets, pinned at the top corners. Left to right: stiff distance limits alone;
    // stiff with area constraints; soft with area constraints.
    // The camera looks along +Z, so +X is screen-left: the first sheet takes the largest x.
    HangSheet(scene, new Vector3(-1, 10, 2), distanceHertz: 20, areaHertz: null);
    HangSheet(scene, new Vector3(-7, 10, 2), distanceHertz: 20, areaHertz: 30);
    HangSheet(scene, new Vector3(-13, 10, 2), distanceHertz: 5, areaHertz: 30);

    // A ball for the fourth sheet to drape over.
    var ball = game.Create3DPrimitive(PrimitiveModelType.Sphere, new()
    {
        Size = new Vector3(1.5f),
        Material = game.CreateMaterial(new Color(120, 160, 220)),
        Component = new StaticComponent { Collider = new CompoundCollider { Colliders = { new SphereCollider { Radius = 1.5f } } } },
        Position = new Vector3(9, 2, -4),
    });
    ball.Scene = scene;
    extras.Add(ball);

    var cube = game.Create3DPrimitive(PrimitiveModelType.Cube, new()
    {
        Size = new Vector3(1.0f),
        Material = game.CreateMaterial(new Color(120, 160, 220)),
        Component = new StaticComponent { Collider = new CompoundCollider { Colliders = { new BoxCollider { Size = new Vector3(1.0f) } } } },
        Position = new Vector3(13, 0.5f, -7),
    });
    cube.Scene = scene;
    extras.Add(cube);

    DrapeSheet(scene, new Vector3(9, 6, -4), distanceHertz: 10, areaHertz: 30);
}

// Node spacing and the node counts that fit the sheet dimensions, all from the current radius.
float Spacing() => nodeRadius * SpacingPerRadius;

int NodesAcross(float extent, float spacing) => (int)MathF.Round(extent / spacing) + 1;

int NodeCount(float radius)
{
    var spacing = radius * SpacingPerRadius;
    var drape = NodesAcross(DrapeExtent, spacing);

    return 3 * NodesAcross(HangingWidth, spacing) * NodesAcross(HangingHeight, spacing) + drape * drape;
}

void HangSheet(Scene scene, Vector3 topLeft, float distanceHertz, float? areaHertz)
{
    var spacing = Spacing();
    var rows = NodesAcross(HangingHeight, spacing);
    var columns = NodesAcross(HangingWidth, spacing);
    var grid = new Entity[rows, columns];
    var groupId = nextGroupId++;

    for (var row = 0; row < rows; row++)
    {
        for (var column = 0; column < columns; column++)
        {
            var pinned = row == 0 && (column == 0 || column == columns - 1);

            grid[row, column] = Node(scene, topLeft + new Vector3(column * spacing, -row * spacing, 0), groupId, row, column, pinned);
        }
    }

    Lace(grid, distanceHertz, areaHertz);
}

void DrapeSheet(Scene scene, Vector3 centre, float distanceHertz, float? areaHertz)
{
    var spacing = Spacing();
    var size = NodesAcross(DrapeExtent, spacing);
    var grid = new Entity[size, size];
    var groupId = nextGroupId++;
    var corner = centre - new Vector3((size - 1) * spacing / 2, 0, (size - 1) * spacing / 2);

    for (var row = 0; row < size; row++)
    {
        for (var column = 0; column < size; column++)
            grid[row, column] = Node(scene, corner + new Vector3(column * spacing, 0, row * spacing), groupId, row, column, pinned: false);
    }

    Lace(grid, distanceHertz, areaHertz);
}

// One node: a sphere body with no model - the master draws it. The collision group says which
// sheet it belongs to and where in it; nodes within one row and one column of each other are
// neighbours and must not collide, or the lattice fights itself.
Entity Node(Scene scene, Vector3 position, ushort groupId, int row, int column, bool pinned)
{
    var entity = new Entity("Node")
    {
        new BodyComponent
        {
            Collider = new CompoundCollider { Colliders = { new SphereCollider { Radius = nodeRadius, Mass = 1 } } },
            Kinematic = pinned,
            CollisionGroup = new CollisionGroup { Id = groupId, IndexA = (ushort)row, IndexB = (ushort)column },
        },
    };
    entity.Transform.Position = position;
    entity.Scene = scene;

    instancing?.AddInstance(entity);
    nodes.Add(entity);

    return entity;
}

// The lattice: distance limits along rows, columns and both diagonals; area constraints on the two
// triangles of every quad when asked for. All measured from the nodes' starting positions.
void Lace(Entity[,] grid, float distanceHertz, float? areaHertz)
{
    var rows = grid.GetLength(0);
    var columns = grid.GetLength(1);

    for (var row = 0; row < rows; row++)
    {
        for (var column = 0; column < columns; column++)
        {
            if (column + 1 < columns) Limit(grid[row, column], grid[row, column + 1], distanceHertz);
            if (row + 1 < rows) Limit(grid[row, column], grid[row + 1, column], distanceHertz);

            if (row + 1 < rows && column + 1 < columns)
            {
                Limit(grid[row, column], grid[row + 1, column + 1], distanceHertz);
                Limit(grid[row, column + 1], grid[row + 1, column], distanceHertz);

                if (areaHertz is { } hertz)
                {
                    Area(grid[row, column], grid[row + 1, column], grid[row, column + 1], hertz);
                    Area(grid[row + 1, column], grid[row, column + 1], grid[row + 1, column + 1], hertz);
                }
            }
        }
    }
}

void Limit(Entity a, Entity b, float hertz)
{
    var bodyA = a.Get<BodyComponent>();
    var bodyB = b.Get<BodyComponent>();

    // Two pinned nodes have nothing to constrain.
    if (bodyA.Kinematic && bodyB.Kinematic) return;

    var distance = Vector3.Distance(a.Transform.Position, b.Transform.Position);

    a.Add(new CenterDistanceLimitConstraintComponent
    {
        A = bodyA,
        B = bodyB,
        MinimumDistance = distance * 0.15f,        // may bunch, may not stretch
        MaximumDistance = distance,
        SpringFrequency = hertz,
        SpringDampingRatio = 1,
    });

    constraintCount++;
}

void Area(Entity a, Entity b, Entity c, float hertz)
{
    var pa = a.Transform.Position;

    a.Add(new AreaConstraintComponent
    {
        A = a.Get<BodyComponent>(),
        B = b.Get<BodyComponent>(),
        C = c.Get<BodyComponent>(),
        TargetScaledArea = Vector3.Cross(b.Transform.Position - pa, c.Transform.Position - pa).Length(),
        SpringFrequency = hertz,
        SpringDampingRatio = 1,
    });

    constraintCount++;
}

void DropBall(Scene scene)
{
    var ball = game.Create3DPrimitive(PrimitiveModelType.Sphere, new()
    {
        Size = new Vector3(0.7f),
        Material = game.CreateMaterial(new Color(240, 200, 80)),
        Component = new BodyComponent { Collider = new CompoundCollider { Colliders = { new SphereCollider { Radius = 0.7f, Mass = 4 } } } },
        Position = new Vector3(9, 12, -4),
    });
    ball.Scene = scene;
    extras.Add(ball);
}

void AddInstructions()
{
    var overlay = DebugOverlay.GetOrCreate(game);
    overlay.SectionGap = 0;

    overlay.AddSection("Cloth", () =>
    [
        .. sizeMenu?.GetLines() ?? [],
        new("Left mouse", "Pull on a sheet", Color.Yellow),
        new("N", "Drop a ball on the draped sheet", Color.Yellow),
        new("R", "Rebuild", Color.Yellow),
        new(""),
        new($"{nodes.Count:N0} nodes, {constraintCount:N0} constraints", Color.LightGreen),
        new("One draw call, 8 solver substeps", Color.LightGreen),
        new("Hanging, left to right: stiff distance limits only,"),
        new("stiff + area constraints, soft + area constraints"),
    ]);
}

/*
---example-metadata
slug: cloth
title:
  en: Cloth
level: Intermediate
category: Physics
complexity: 5
order: 97
description:
  en: |-
    Cloth from ordinary bodies and constraints, the way bepuphysics2's own demo does it: a lattice
    of sphere nodes tied by distance limits that may bunch but never stretch, area constraints on
    every triangle against shear, and collision groups keeping neighbours from fighting. Three
    sheets hang side by side to compare stiffness with and without area constraints, a fourth
    drapes over a ball, about eleven hundred nodes are one instanced draw call, and the solver runs eight
    substeps set through UseGameSettings, where Bepu's demo runs eight velocity iterations instead. Pull on anything with the grabber, and pick the node
    size from a menu: the sheets keep their dimensions, so smaller nodes mean a denser lattice
    with finer folds and a higher body count.
concepts:
  - Cloth as a lattice of bodies with CenterDistanceLimit and Area constraints - no special case needed
  - "A distance limit with a low minimum: the sheet can bunch but not stretch"
  - Area constraints against shear, and what a sheet looks like without them
  - Keeping neighbouring nodes from colliding with CollisionGroup's index rule
  - Solver substeps or velocity iterations for a stiff connected system, set through UseGameSettings
  - Drawing hundreds of bodies as one instanced master with BepuEntityInstancing
  - "Node size as lattice density: spacing follows the radius, the sheets keep their size, a DebugTextDropdown rebuilds"
  - "Using helpers: SetupBase3D, Add3DGround, GrabberScript, DebugOverlay, DebugTextDropdown"
tags:
  - 3D
  - Bepu
  - Physics
  - Cloth
  - Constraint
  - Instancing
related:
  - E05_3D_Constraints_Rope
  - E05_3D_Grabber
  - E10_2D_StressPile
screenshotFrame: 150
enabled: true
created: 2026-09-06
---
*/