using Box2D.NET;
using Example18_Box2DPhysics.Physics;
using Stride.CommunityToolkit.Engine;
using Stride.CommunityToolkit.Rendering.ProceduralModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Input;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Joints;

namespace Example18_Box2DPhysics.Helpers;

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
    private readonly B2WorldId _worldId;

    private int _totalShapesCreated = 0;
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
        _worldId = simulation.GetWorldId();

        _shapeFactory = new ShapeFactory(game, scene);
        _uiHelper = new UiHelper(game);
        _inputManager = new InputManager(game, _camera);
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
        WorldGeometryBuilder.AddGround(_simulation.GetWorldId());

        // Create a simple demonstration with a few shapes
        var shapeFactory = new ShapeFactory(_game, _scene);

        // Create a single shape with zero gravity for demonstration
        var shape = shapeFactory.GetShapeModel(Primitive2DModelType.Rectangle2D);

        if (shape != null)
        {
            var entity = shapeFactory.CreateEntity(shape, position: new Vector2(0, 2));
            var bodyId = _simulation.CreateDynamicBody(entity, entity.Transform.Position);

            // Set zero gravity for this body to demonstrate control
            b2Body_SetGravityScale(bodyId, 0);

            ShapeFixtureBuilder.AttachShape(shape, bodyId);
        }

        // Add some initial shapes for interaction
        AddInitialShapes();
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
        AddShapes(Primitive2DModelType.Rectangle2D, 10, Color.Black);
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
            AddShapes(Primitive2DModelType.Square2D, GameConfig.DefaultSpawnCount);
            LogAction("Added squares");
        }
        else if (input.IsKeyPressed(Keys.R))
        {
            AddShapes(Primitive2DModelType.Rectangle2D, GameConfig.DefaultSpawnCount);
            LogAction("Added rectangles");
        }
        else if (input.IsKeyPressed(Keys.C))
        {
            AddShapes(Primitive2DModelType.Circle2D, GameConfig.DefaultSpawnCount);
            LogAction("Added circles");
        }
        else if (input.IsKeyPressed(Keys.T))
        {
            AddShapes(Primitive2DModelType.Triangle2D, GameConfig.DefaultSpawnCount);
            LogAction("Added triangles");
        }
        else if (input.IsKeyPressed(Keys.V))
        {
            AddShapes(Primitive2DModelType.Capsule, GameConfig.DefaultSpawnCount);
            LogAction("Added capsules");
        }
        else if (input.IsKeyPressed(Keys.P))
        {
            AddRandomShapes(GameConfig.MassSpawnCount);
            LogAction($"Added {GameConfig.MassSpawnCount} random shapes");
        }
        else if (input.IsKeyPressed(Keys.J))
        {
            AddShapesWithJoints(GameConfig.DefaultSpawnCount);
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
            ClearAllShapes();
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

        // Try to find a physics body at the mouse position
        var hitBodyId = _simulation.OverlapPoint(worldPoint.Value, GameConfig.MouseQuerySize);

        if (hitBodyId.HasValue)
        {
            ApplyMouseImpulse(hitBodyId.Value, worldPoint.Value);
        }
        else
        {
            // Could create a new shape at mouse position
            CreateShapeAtPosition(worldPoint.Value);
        }
    }

    private void AddShapes(Primitive2DModelType type, int count, Color? color = null)
    {
        for (int i = 0; i < count; i++)
        {
            var shapeModel = _shapeFactory.GetShapeModel(type);
            if (shapeModel == null) continue;

            var entity = _shapeFactory.CreateEntity(shapeModel, color);
            var bodyId = _simulation.CreateDynamicBody(entity, entity.Transform.Position);

            ShapeFixtureBuilder.AttachShape(shapeModel, bodyId);

            _totalShapesCreated++;
        }
    }

    private void AddRandomShapes(int count)
    {
        for (int i = 0; i < count; i++)
        {
            var shapeModel = _shapeFactory.GetShapeModel(); // Random shape
            if (shapeModel == null) continue;

            var entity = _shapeFactory.CreateEntity(shapeModel);
            var bodyId = _simulation.CreateDynamicBody(entity, entity.Transform.Position);

            ShapeFixtureBuilder.AttachShape(shapeModel, bodyId);
            _totalShapesCreated++;
        }
    }

    private void AddShapesWithJoints(int count)
    {
        for (int i = 0; i < count; i++)
        {
            CreateConnectedShapePair();
        }
    }

    private void CreateConnectedShapePair()
    {
        // Create two shapes
        var shapeModel1 = _shapeFactory.GetShapeModel();
        var shapeModel2 = _shapeFactory.GetShapeModel();

        if (shapeModel1 == null || shapeModel2 == null) return;

        var entity1 = _shapeFactory.CreateEntity(shapeModel1, GameConfig.ConstraintColor);
        var entity2 = _shapeFactory.CreateEntity(shapeModel2, GameConfig.ConstraintColor);

        // Position second shape relative to first
        entity2.Transform.Position = new Vector3(
            entity1.Transform.Position.X + GameConfig.DefaultJointLength,
            entity1.Transform.Position.Y,
            entity1.Transform.Position.Z);

        // Create physics bodies
        var bodyIdA = _simulation.CreateDynamicBody(entity1, entity1.Transform.Position);
        var bodyIdB = _simulation.CreateDynamicBody(entity2, entity2.Transform.Position);

        ShapeFixtureBuilder.AttachShape(shapeModel1, bodyIdA);
        ShapeFixtureBuilder.AttachShape(shapeModel2, bodyIdB);

        // Create distance joint
        CreateDistanceJoint(bodyIdA, bodyIdB);

        _totalShapesCreated += 2;
    }

    private void CreateDistanceJoint(B2BodyId bodyA, B2BodyId bodyB)
    {
        var jointDef = b2DefaultDistanceJointDef();
        jointDef.hertz = GameConfig.JointHertz;
        jointDef.dampingRatio = GameConfig.JointDampingRatio;
        jointDef.length = GameConfig.DefaultJointLength;
        jointDef.maxLength = GameConfig.DefaultJointLength;
        jointDef.minLength = GameConfig.DefaultJointLength;
        jointDef.enableSpring = true;
        jointDef.enableLimit = true;

        jointDef.@base.bodyIdA = bodyA;
        jointDef.@base.bodyIdB = bodyB;
        jointDef.@base.localFrameA.p = new B2Vec2(0, 0);
        jointDef.@base.localFrameB.p = new B2Vec2(0, 0);

        b2CreateDistanceJoint(_worldId, ref jointDef);
    }

    private void CreateShapeAtPosition(Vector2 position)
    {
        var shapeModel = _shapeFactory.GetShapeModel();
        if (shapeModel == null) return;

        var entity = _shapeFactory.CreateEntity(shapeModel, GameConfig.SelectedShapeColor, position);
        var bodyId = _simulation.CreateDynamicBody(entity, entity.Transform.Position);

        ShapeFixtureBuilder.AttachShape(shapeModel, bodyId);
        _totalShapesCreated++;

        LogAction($"Created {shapeModel.Type} at mouse position");
    }

    private void ClearAllShapes()
    {
        var shapesToRemove = _scene.Entities
            .Where(e => e.Name.EndsWith(GameConfig.ShapeName))
            .ToList();

        foreach (var entity in shapesToRemove)
        {
            _simulation.RemoveBody(entity);
            entity.Remove();
        }

        _totalShapesCreated = 0;
    }

    private void ApplyMouseImpulse(B2BodyId bodyId, Vector2 worldPoint)
    {
        var entity = _simulation.GetEntity(bodyId);
        if (entity == null) return;

        // Apply upward impulse with some randomness
        var impulseDirection = new Vector2(
            Random.Shared.NextSingle() * 2f - 1f, // -1 to 1
            GameConfig.ImpulseStrength
        );

        BodyForces.ApplyImpulse(bodyId, impulseDirection);

        LogAction($"Applied impulse to {entity.Name}");
    }

    private void TogglePhysics()
    {
        _simulation.Enabled = !_simulation.Enabled;
        var status = _simulation.Enabled ? "enabled" : "disabled";
        LogAction($"Physics {status}");
    }

    private void UpdateUI()
    {
        _uiHelper.RenderNavigation(ShapeCount, _simulation);

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