using Box2D.NET;
using Stride.CommunityToolkit.Box2D;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
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
/// Manages the overall demo experience including input handling, shape creation, and user interactions.
/// This class orchestrates the various components of the Box2D physics demonstration.
/// </summary>
public class SceneManager
{
    private readonly Game _game;
    private readonly Scene _scene;
    private readonly Box2DSimulation _simulation;
    private readonly CameraComponent _camera;
    private readonly ShapeFactory _shapeFactory;
    private readonly UiHelper _uiHelper;
    private readonly InputManager _inputManager;
    private readonly ShapeSpawner _shapeSpawner;

    private string _lastAction = "Initialized";
    private DateTime _lastActionTime = DateTime.Now;

    public int ShapeCount => _scene.Entities.Count(e => e.Name.EndsWith(GameConfig.ShapeName));

    public SceneManager(Game game, Scene scene, Box2DSimulation simulation)
    {
        _game = game;
        _scene = scene;

        var camera = scene.GetCamera() ?? throw new InvalidOperationException("Camera not found in scene");

        _simulation = simulation;
        _camera = camera;
        _shapeFactory = new ShapeFactory(scene);
        _uiHelper = new UiHelper(game);
        _inputManager = new InputManager(game, _camera);
        _shapeSpawner = new ShapeSpawner(scene, simulation, _shapeFactory);
    }

    /// <summary>
    /// Initializes the demo manager and sets up initial state
    /// </summary>
    public void Initialize()
    {
        LogAction("Demo initialized");

        // Register for physics events if needed
        // _simulation.RegisterContactEventHandler(this);

        // Could add initial demo shapes here
        // AddInitialShapes();

        // Create the initial scene setup
        CreateInitialScene();
    }

    void CreateInitialScene()
    {
        // Add ground for physics objects to collide with
        //WorldGeometryBuilder.AddGround(_simulation.GetWorldId());
        AddGroundAndWalls();

        // Create a single shape with zero gravity for demonstration
        var shape = _shapeFactory.GetShapeModel(Primitive2DModelType.Rectangle);

        if (shape != null)
        {
            var entity = _shapeFactory.CreateEntity(shape, position: new Vector2(0, 2));
            var bodyId = _simulation.CreateDynamicBody(entity, entity.Transform.Position);

            // Set zero gravity for this body to demonstrate weightless behavior
            b2Body_SetGravityScale(bodyId, 0);

            ShapeFixtureBuilder.AttachShape(shape.Type, shape.Size, bodyId, ShapeSpawner.DefaultShapeDef());
        }

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
            AddStaticSquare(new Vector2(x, -3f), 0.55f, 0.5f);
        }

        for (var i = 0; i < 10; i++)
        {
            var y = -2f + i;

            foreach (var x in (float[])[-25f, 25f])
            {
                var box = b2MakeOffsetBox(0.5f, 0.55f, new B2Vec2(x, y), b2Rot_identity);
                b2CreatePolygonShape(groundId, in shapeDef, in box);
                AddStaticSquare(new Vector2(x, y), 0.5f, 0.55f);
            }
        }
    }

    private void AddStaticSquare(Vector2 position, float halfWidth, float halfHeight)
    {
        var entity = new Entity(GameConfig.WallName)
        {
            new ShapeComponent
            {
                Vertices =
                [
                    new(-halfWidth, -halfHeight),
                    new(halfWidth, -halfHeight),
                    new(halfWidth, halfHeight),
                    new(-halfWidth, halfHeight),
                ],
                Color = GameConfig.GroundColor,
            }
        };
        entity.Transform.Position = new Vector3(position.X, position.Y, 0);
        entity.Scene = _scene;
    }

    /// <summary>
    /// Updates the demo manager each frame
    /// </summary>
    /// <param name="gameTime">Current game time</param>
    public void Update(GameTime gameTime)
    {
        ProcessInput();
        UpdateUI();

        // Could add periodic demo updates here
        // UpdateDemoLogic(gameTime);
    }

    /// <summary>
    /// Adds initial demonstration shapes to the scene
    /// </summary>
    public void AddInitialShapes()
    {
        // Add some demo shapes with different properties
        _shapeSpawner.Add(Primitive2DModelType.Rectangle, 10, GameConfig.ShapeColor);
        LogAction($"Added {10} initial demo shapes");
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
            _shapeSpawner.Add(Primitive2DModelType.Square, GameConfig.DefaultSpawnCount);
            LogAction("Added squares");
        }
        else if (input.IsKeyPressed(Keys.R))
        {
            _shapeSpawner.Add(Primitive2DModelType.Rectangle, GameConfig.DefaultSpawnCount);
            LogAction("Added rectangles");
        }
        else if (input.IsKeyPressed(Keys.C))
        {
            _shapeSpawner.Add(Primitive2DModelType.Circle, GameConfig.DefaultSpawnCount);
            LogAction("Added circles");
        }
        else if (input.IsKeyPressed(Keys.T))
        {
            _shapeSpawner.Add(Primitive2DModelType.Triangle, GameConfig.DefaultSpawnCount);
            LogAction("Added triangles");
        }
        else if (input.IsKeyPressed(Keys.V))
        {
            _shapeSpawner.Add(Primitive2DModelType.Capsule, GameConfig.DefaultSpawnCount);
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

        // Could add more advanced controls
        // Debug/utility commands could go here
    }

    private void ProcessMouseInput()
    {
        if (!_game.Input.IsMouseButtonPressed(MouseButton.Left)) return;

        var mousePosition = _game.Input.MousePosition;
        var worldPoint = _inputManager.GetWorldPointFromMouse(mousePosition);

        if (worldPoint == null)
        {
            LogAction("Mouse click outside world bounds");
            return;
        }

        // A click on a body is the grabber's (Grabber2DScript on the camera picks it up); a click on
        // empty space spawns something there.
        if (_simulation.OverlapPoint(worldPoint.Value, GameConfig.MouseQuerySize).HasValue) return;

        if (_shapeSpawner.AddAtPosition(worldPoint.Value) is { } spawned)
        {
            LogAction($"Created {spawned} at mouse position");
        }
    }

    private void TogglePhysics()
    {
        _simulation.Enabled = !_simulation.Enabled;
        var status = _simulation.Enabled ? "enabled" : "disabled";
        LogAction($"Physics {status}");
    }

    private void UpdateUI()
    {
        _uiHelper.RenderNavigation(ShapeCount, _shapeSpawner.TotalCreated, _simulation);

        // Show last action
        if ((DateTime.Now - _lastActionTime).TotalSeconds < 3)
        {
            _uiHelper.RenderStatusMessage($"Last action: {_lastAction}", Color.LightGreen);
        }

        // Could add more UI elements here
        // Performance metrics, physics debug info, etc.
    }

    private void LogAction(string action)
    {
        _lastAction = action;
        _lastActionTime = DateTime.Now;

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {action}");
    }
}