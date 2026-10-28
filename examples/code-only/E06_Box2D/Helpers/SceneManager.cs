using Box2D.NET;
using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Shapes;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;

namespace E06_Box2D.Helpers;

/// <summary>
/// Runs the demo: input, the UI lines and the static yard. Spawning is <see cref="ShapeSpawner"/>'s.
/// </summary>
public class SceneManager
{
    private readonly Game _game;
    private readonly Scene _scene;
    private readonly Box2DSimulation _simulation;
    private readonly CameraComponent _camera;
    private readonly ShapeFactory _shapeFactory;
    private readonly UiHelper _uiHelper;
    private readonly ShapeSpawner _shapeSpawner;

    private string _lastAction = "Initialized";
    private DateTime _lastActionTime = DateTime.Now;

    public int ShapeCount
    {
        get
        {
            var count = 0;

            foreach (var entity in _scene.Entities)
            {
                if (entity.Name.EndsWith(GameConfig.ShapeName)) count++;
            }

            return count;
        }
    }

    public SceneManager(Game game, Scene scene, Box2DSimulation simulation)
    {
        _game = game;
        _scene = scene;
        _simulation = simulation;
        _camera = game.GetCameraEntity().Get<CameraComponent>();
        _shapeFactory = new ShapeFactory(scene);
        _uiHelper = new UiHelper(game);
        _shapeSpawner = new ShapeSpawner(scene, simulation, _shapeFactory);
    }

    /// <summary>
    /// Builds the yard and the first shapes.
    /// </summary>
    public void Initialize()
    {
        LogAction("Demo initialized");

        CreateInitialScene();
    }

    private void CreateInitialScene()
    {
        AddGroundAndWalls();

        // One shape with no gravity, so it hangs where it was put
        var shape = ShapeFactory.Rectangle;
        var entity = _shapeFactory.CreateEntity(shape, position: new Vector2(0, 2));
        var bodyId = _simulation.CreateDynamicBody(entity, entity.Transform.Position);

        b2Body_SetGravityScale(bodyId, 0);

        ShapeFixtureBuilder.AttachShape(shape.Type, bodyId, ShapeSpawner.DefaultShapeDef());

        AddInitialShapes();
    }

    private void AddGroundAndWalls()
    {
        // Junkyard-style yard: one static body carrying rows of slightly overlapping squares,
        // each square also an entity drawn by the Box2D debug-draw component
        var groundId = _simulation.CreateStaticBody(Vector3.Zero);
        var shapeDef = ShapeSpawner.DefaultShapeDef();

        for (var i = 0; i <= 50; i++)
        {
            var x = -25f + i;
            var box = b2MakeOffsetBox(0.55f, 0.5f, new B2Vec2(x, -3f), b2Rot_identity);
            b2CreatePolygonShape(groundId, in shapeDef, in box);
            AddStaticSquare(new Vector2(x, -3f), new Vector2(1.1f, 1f));
        }

        for (var i = 0; i < 10; i++)
        {
            var y = -2f + i;

            foreach (var x in (float[])[-25f, 25f])
            {
                var box = b2MakeOffsetBox(0.5f, 0.55f, new B2Vec2(x, y), b2Rot_identity);
                b2CreatePolygonShape(groundId, in shapeDef, in box);
                AddStaticSquare(new Vector2(x, y), new Vector2(1f, 1.1f));
            }
        }
    }

    private void AddStaticSquare(Vector2 position, Vector2 size)
    {
        var entity = new Entity(GameConfig.WallName)
        {
            new ShapeComponent { Vertices = [.. ShapeComponent.Box(size)], Color = GameConfig.GroundColor }
        };
        entity.Transform.Position = new Vector3(position, 0);
        entity.Scene = _scene;
    }

    /// <summary>
    /// Reads the input and refreshes the UI lines.
    /// </summary>
    /// <param name="gameTime">Current game time</param>
    public void Update(GameTime gameTime)
    {
        ProcessInput();
        UpdateUI();
    }

    /// <summary>
    /// The shapes on the yard at the start.
    /// </summary>
    public void AddInitialShapes()
    {
        _shapeSpawner.Add(ShapeFactory.Rectangle, GameConfig.DefaultSpawnCount, GameConfig.ShapeColor);
        LogAction($"Added {GameConfig.DefaultSpawnCount} initial demo shapes");
    }

    private void ProcessInput()
    {
        ProcessKeyboardInput();
        ProcessMouseInput();
    }

    private void ProcessKeyboardInput()
    {
        var input = _game.Input;

        // Shape creation commands
        if (input.IsKeyPressed(Keys.M))
        {
            _shapeSpawner.Add(ShapeFactory.Square, GameConfig.DefaultSpawnCount);
            LogAction("Added squares");
        }
        else if (input.IsKeyPressed(Keys.R))
        {
            _shapeSpawner.Add(ShapeFactory.Rectangle, GameConfig.DefaultSpawnCount);
            LogAction("Added rectangles");
        }
        else if (input.IsKeyPressed(Keys.C))
        {
            _shapeSpawner.Add(ShapeFactory.Circle, GameConfig.DefaultSpawnCount);
            LogAction("Added circles");
        }
        else if (input.IsKeyPressed(Keys.T))
        {
            _shapeSpawner.Add(ShapeFactory.Triangle, GameConfig.DefaultSpawnCount);
            LogAction("Added triangles");
        }
        else if (input.IsKeyPressed(Keys.V))
        {
            _shapeSpawner.Add(ShapeFactory.Capsule, GameConfig.DefaultSpawnCount);
            LogAction("Added capsules");
        }
        else if (input.IsKeyPressed(Keys.P))
        {
            _shapeSpawner.AddRandom(GameConfig.MassSpawnCount);
            LogAction($"Added {GameConfig.MassSpawnCount} random shapes");
        }
        else if (input.IsKeyPressed(Keys.J))
        {
            _shapeSpawner.AddWithJoints(GameConfig.DefaultSpawnCount);
            LogAction("Added shapes with joints");
        }
        else if (input.IsKeyPressed(Keys.G))
        {
            AddInitialShapes();
            LogAction("Added demo shapes");
        }

        // Control commands
        else if (input.IsKeyPressed(Keys.X))
        {
            _shapeSpawner.Clear();
            LogAction("Cleared all shapes");
        }
        else if (input.IsKeyPressed(Keys.Space))
        {
            TogglePhysics();
        }
    }

    private void ProcessMouseInput()
    {
        if (!_game.Input.IsMouseButtonPressed(MouseButton.Left)) return;

        var mousePosition = _game.Input.MousePosition;
        var worldPoint = _camera.ScreenToWorldPointOnXYPlane(mousePosition);

        if (worldPoint is null)
        {
            LogAction("Mouse click outside world bounds");
            return;
        }

        // A click on a body is the grabber's (Grabber2DScript on the camera picks it up); a click on
        // empty space spawns something there.
        if (_simulation.OverlapPoint(worldPoint.Value, GameConfig.MouseQuerySize).HasValue) return;

        var spawned = _shapeSpawner.AddAtPosition(worldPoint.Value);

        LogAction($"Created {spawned.Type.Name} at mouse position");
    }

    private void TogglePhysics()
    {
        _simulation.Enabled = !_simulation.Enabled;
        LogAction($"Physics {(_simulation.Enabled ? "enabled" : "disabled")}");
    }

    private void UpdateUI()
    {
        _uiHelper.RenderNavigation(ShapeCount, _shapeSpawner.TotalCreated, _simulation);

        if ((DateTime.Now - _lastActionTime).TotalSeconds < 3)
        {
            _uiHelper.RenderStatusMessage($"Last action: {_lastAction}", Color.LightGreen);
        }
    }

    private void LogAction(string action)
    {
        _lastAction = action;
        _lastActionTime = DateTime.Now;

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {action}");
    }
}